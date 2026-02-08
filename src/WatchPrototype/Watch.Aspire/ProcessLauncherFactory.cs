// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class ProcessLauncherFactory(string namedPipe, Func<WatchStatusEvent, Task>? statusEventWriter, CancellationToken shutdownCancellationToken) : IRuntimeProcessLauncherFactory
{
    private Launcher? _currentLauncher;

    public IRuntimeProcessLauncher Create(ProjectLauncher projectLauncher, string? launchProfile, string? targetFramework, IReadOnlyList<string> buildArguments, Action? onLaunchedProcessCrashed = null)
    {
        // Reuse the existing launcher if the pipe is still alive (crash-restart iteration).
        // This keeps the DCP resource command connected across iteration restarts.
        if (_currentLauncher is { IsDisposed: false } launcher)
        {
            projectLauncher.Logger.LogDebug("Reusing existing launcher (pipe still alive)");
            launcher.UpdateProjectLauncher(projectLauncher);
            return launcher;
        }

        _currentLauncher = new Launcher(namedPipe, statusEventWriter, projectLauncher, launchProfile, targetFramework, buildArguments, onLaunchedProcessCrashed, shutdownCancellationToken);
        return _currentLauncher;
    }

    private sealed class Launcher : IRuntimeProcessLauncher
    {
        private const byte Version = 1;

        private volatile ProjectLauncher _projectLauncher;
        private readonly Func<WatchStatusEvent, Task>? _statusEventWriter;
        private readonly string? _launchProfile;
        private readonly string? _targetFramework;
        private readonly IReadOnlyList<string> _buildArguments;
        private readonly Action? _onLaunchedProcessCrashed;
        private readonly Task _listenerTask;
        private readonly CancellationTokenSource _launcherCts;

        private ImmutableHashSet<Task> _pendingRequestCompletions = [];
        private volatile bool _crashCallbackFired;

        public bool IsDisposed { get; private set; }

        public Launcher(
            string namedPipe,
            Func<WatchStatusEvent, Task>? statusEventWriter,
            ProjectLauncher projectLauncher,
            string? launchProfile,
            string? targetFramework,
            IReadOnlyList<string> buildArguments,
            Action? onLaunchedProcessCrashed,
            CancellationToken shutdownCancellationToken)
        {
            _projectLauncher = projectLauncher;
            _statusEventWriter = statusEventWriter;
            _launchProfile = launchProfile;
            _targetFramework = targetFramework;
            _buildArguments = buildArguments;
            _onLaunchedProcessCrashed = onLaunchedProcessCrashed;

            _launcherCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCancellationToken);
            _listenerTask = StartListeningAsync(namedPipe, _launcherCts.Token);
        }

        private TaskCompletionSource? _relaunchSignal;

        public void UpdateProjectLauncher(ProjectLauncher projectLauncher)
        {
            _projectLauncher = projectLauncher;
            // Reset crash flag so new iteration can detect crashes again
            _crashCallbackFired = false;
            // Signal existing HandleRequestAsync to relaunch the project
            _relaunchSignal?.TrySetResult();
        }

        public async ValueTask DisposeAsync()
        {
            Logger.LogDebug("DisposeAsync: cancelling listener");
            IsDisposed = true;
            await _launcherCts.CancelAsync();
            await _listenerTask;
            await Task.WhenAll(_pendingRequestCompletions);
            _launcherCts.Dispose();
            Logger.LogDebug("DisposeAsync: completed");
        }

        private ILogger Logger => _projectLauncher.Logger;

        private async Task StartListeningAsync(string pipeName, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Listening on '{PipeName}'", pipeName);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var pipe = new NamedPipeServerStream(
                        pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                    await pipe.WaitForConnectionAsync(cancellationToken);

                    Logger.LogDebug("Connected to '{PipeName}'", pipeName);

                    var version = await pipe.ReadByteAsync(cancellationToken);
                    if (version != Version)
                    {
                        Logger.LogDebug("Unsupported protocol version '{Version}'", version);
                        await pipe.WriteAsync((byte)0, cancellationToken);
                        await pipe.DisposeAsync();
                        continue;
                    }

                    var json = await pipe.ReadStringAsync(cancellationToken);

                    var request = JsonSerializer.Deserialize<LaunchResourceRequest>(json) ?? throw new JsonException("Unexpected null");

                    Logger.LogDebug("Request received.");
                    await pipe.WriteAsync((byte)1, cancellationToken);

                    // Don't dispose the pipe - it's now owned by HandleRequestAsync
                    // which will keep it alive for output proxying
                    _ = HandleRequestAsync(request, pipe, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // nop
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to launch resource: {Exception}", e.Message);
            }
        }

        private async Task HandleRequestAsync(LaunchResourceRequest request, NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource();
            ImmutableInterlocked.Update(ref _pendingRequestCompletions, set => set.Add(completionSource.Task));

            // Shared box to track the latest RunningProject across restarts.
            // restartOperation creates new RunningProjects — we always need the latest one.
            var currentProject = new StrongBox<RunningProject?>(null);

            // Create a per-connection token that cancels when the pipe disconnects OR on shutdown.
            // DCP Stop kills the resource command, which closes the pipe from the other end.
            // We detect that by reading from the pipe — when it breaks, we cancel.
            using var pipeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var connectionToken = pipeCts.Token;

            try
            {
                Logger.LogDebug("HandleRequest: starting '{EntryPoint}'", request.EntryPoint);
                var projectOptions = GetProjectOptions(request);

                while (true)
                {
                    // Set up a relaunch signal for crash recovery.
                    // UpdateProjectLauncher() completes this when a new iteration starts after a crash.
                    _relaunchSignal = new TaskCompletionSource();

                    currentProject.Value = await StartProjectAsync(projectOptions, pipe, currentProject, isRestart: currentProject.Value is not null, connectionToken);

                    Logger.LogDebug("Project started, waiting for pipe disconnect or relaunch.");

                    // Wait for either: pipe disconnects (DCP Stop) OR relaunch signal (crash recovery)
                    var pipeDisconnectTask = WaitForPipeDisconnectAsync(pipe, connectionToken);
                    var completedTask = await Task.WhenAny(pipeDisconnectTask, _relaunchSignal.Task);

                    if (completedTask == _relaunchSignal.Task)
                    {
                        Logger.LogDebug("Relaunch signal received for '{EntryPoint}'.", request.EntryPoint);
                        // New iteration started after crash — relaunch the project with the updated _projectLauncher
                        continue;
                    }
                    else
                    {
                        Logger.LogDebug("Pipe disconnected for '{EntryPoint}'.", request.EntryPoint);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown or DCP killed the resource command
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to start '{Path}': {Exception}", request.EntryPoint, e.Message);
            }
            finally
            {
                // Cancel the connection token so any in-flight restartOperation / drain tasks stop.
                await pipeCts.CancelAsync();

                // Terminate the project process when the resource command disconnects.
                // This handles DCP Stop — the resource command is killed, pipe breaks,
                // and we clean up the project process the watch server launched.
                if (currentProject.Value is { } project)
                {
                    Logger.LogDebug("Pipe disconnected for '{Path}', terminating project process.", request.EntryPoint);
                    await project.TerminateAsync();
                }

                await pipe.DisposeAsync();
                Logger.LogDebug("HandleRequest completed for '{Path}'.", request.EntryPoint);
            }

            ImmutableInterlocked.Update(ref _pendingRequestCompletions, set => set.Remove(completionSource.Task));
            completionSource.SetResult();
        }

        private static async Task WaitForPipeDisconnectAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[1];
                while (pipe.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await pipe.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                }
            }
            catch (IOException)
            {
                // Pipe disconnected
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

        private async ValueTask<RunningProject> StartProjectAsync(ProjectOptions projectOptions, NamedPipeServerStream pipe, StrongBox<RunningProject?> currentProject, bool isRestart, CancellationToken cancellationToken)
        {
            Logger.LogDebug("{Action}: '{Path}'", isRestart ? "Restarting" : "Starting", projectOptions.Representation.ProjectOrEntryPointFilePath);

            // Buffer output through a channel to avoid blocking the synchronous onOutput callback.
            // The channel is drained asynchronously by DrainOutputChannelAsync which writes to the pipe.
            var outputChannel = Channel.CreateUnbounded<(byte Type, string Content)>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });

            var drainTask = DrainOutputChannelAsync(outputChannel.Reader, pipe, cancellationToken);

            var runningProject = await _projectLauncher.TryLaunchProcessAsync(
                projectOptions,
                processTerminationSource: new CancellationTokenSource(),
                onOutput: line =>
                {
                    var typeByte = line.IsError ? AspireResourceLauncher.OutputTypeStderr : AspireResourceLauncher.OutputTypeStdout;
                    outputChannel.Writer.TryWrite((typeByte, line.Content));
                },
                onExit: async (processId, exitCode) =>
                {
                    var isRestarting = currentProject.Value?.IsRestarting == true;
                    Logger.LogDebug("Process {ProcessId} exited with code {ExitCode} for '{Path}'",
                        processId, exitCode, projectOptions.Representation.ProjectOrEntryPointFilePath);

                    // Emit a status event for non-zero exit codes so the dashboard shows the crash.
                    // Skip if cancellation is requested (DCP Stop/shutdown) or if the project
                    // is being deliberately restarted (rude edit restart).
                    if (exitCode is not null and not 0 && _statusEventWriter is not null && !cancellationToken.IsCancellationRequested && !isRestarting)
                    {
                        await _statusEventWriter(new WatchStatusEvent
                        {
                            Type = WatchStatusEvent.Types.ProcessExited,
                            Projects = [projectOptions.Representation.ProjectOrEntryPointFilePath],
                            ExitCode = exitCode,
                        });
                    }

                    // Signal the iteration to restart so dotnet-watch rebuilds on next file change.
                    // Only for actual crashes (non-zero exit, not cancelled, not a deliberate restart).
                    // Use once-only flag to prevent storms from dotnet-watch auto-retry.
                    if (exitCode is not null and not 0 && !cancellationToken.IsCancellationRequested && !isRestarting)
                    {
                        if (!_crashCallbackFired && _onLaunchedProcessCrashed is not null)
                        {
                            _crashCallbackFired = true;
                            Logger.LogDebug("Launched process crashed, cancelling iteration.");
                            _onLaunchedProcessCrashed();
                        }
                    }

                    // Signal the exit to the resource command but DON'T complete the channel.
                    // dotnet-watch will auto-retry on crash and reuse the same onOutput callback,
                    // so new output from the retried process flows through the same channel/pipe.
                    // Completing the channel would starve the pipe and cause DCP to kill the
                    // resource command, triggering a disconnect → terminate → reconnect storm.
                    outputChannel.Writer.TryWrite((AspireResourceLauncher.OutputTypeExit, (exitCode ?? -1).ToString()));
                },
                restartOperation: async ct =>
                {
                    Logger.LogDebug("Restart operation initiated.");
                    // Complete the old channel so the old drain task finishes before
                    // StartProjectAsync creates a new channel + drain on the same pipe.
                    outputChannel.Writer.TryComplete();
                    await drainTask;
                    var newProject = await StartProjectAsync(projectOptions, pipe, currentProject, isRestart: true, ct);
                    currentProject.Value = newProject;
                    Logger.LogDebug("Restart operation completed.");
                    return newProject;
                },
                cancellationToken)
                ?? throw new InvalidOperationException();

            // Emit ProcessStarted so the dashboard knows the process is actually running.
            if (_statusEventWriter is not null)
            {
                await _statusEventWriter(new WatchStatusEvent
                {
                    Type = WatchStatusEvent.Types.ProcessStarted,
                    Projects = [projectOptions.Representation.ProjectOrEntryPointFilePath],
                });
            }

            return runningProject;
        }

        private static async Task DrainOutputChannelAsync(ChannelReader<(byte Type, string Content)> reader, NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var (typeByte, content) in reader.ReadAllAsync(cancellationToken))
                {
                    await pipe.WriteAsync(typeByte, cancellationToken);
                    await pipe.WriteAsync(content, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
                // Pipe disconnected or cancelled
            }
        }

        private ProjectOptions GetProjectOptions(LaunchResourceRequest request)
        {
            var project = ProjectRepresentation.FromProjectOrEntryPointFilePath(request.EntryPoint);

            return new()
            {
                IsRootProject = false,
                Representation = project,
                WorkingDirectory = Path.GetDirectoryName(request.EntryPoint) ?? throw new InvalidOperationException(),
                BuildArguments = _buildArguments,
                Command = "run",
                CommandArguments = GetRunCommandArguments(request, _launchProfile),
                LaunchEnvironmentVariables = request.EnvironmentVariables?.Select(e => (e.Key, e.Value))?.ToArray() ?? [],
                LaunchProfileName = request.LaunchProfile,
                NoLaunchProfile = request.NoLaunchProfile,
                TargetFramework = _targetFramework,
            };
        }

        // internal for testing
        internal static IReadOnlyList<string> GetRunCommandArguments(LaunchResourceRequest request, string? hostLaunchProfile)
        {
            var arguments = new List<string>();

            // Implements https://github.com/dotnet/aspire/blob/main/docs/specs/IDE-execution.md#launch-profile-processing-project-launch-configuration

            if (request.NoLaunchProfile)
            {
                arguments.Add("--no-launch-profile");
            }
            else if (!string.IsNullOrEmpty(request.LaunchProfile))
            {
                arguments.Add("--launch-profile");
                arguments.Add(request.LaunchProfile);
            }
            else if (hostLaunchProfile != null)
            {
                arguments.Add("--launch-profile");
                arguments.Add(hostLaunchProfile);
            }

            if (request.ApplicationArguments != null)
            {
                if (request.ApplicationArguments.Any())
                {
                    arguments.AddRange(request.ApplicationArguments);
                }
                else
                {
                    // indicate that no arguments should be used even if launch profile specifies some:
                    arguments.Add("--no-launch-profile-arguments");
                }
            }

            return arguments;
        }

        public IEnumerable<(string name, string value)> GetEnvironmentVariables()
            => [];

        public ValueTask TerminateLaunchedProcessesAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask; 
    }
}
