// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Tools.Service;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class ProcessLauncherFactory(
    string serverPipeName,
    string? controlPipeName,
    WatchStatusWriter? statusWriter,
    Optional<string?> launchProfile,
    CancellationToken shutdownCancellationToken) : IRuntimeProcessLauncherFactory
{
    public IRuntimeProcessLauncher Create(ProjectLauncher projectLauncher)
    {
        // Connect to control pipe if provided
        var controlReader = controlPipeName != null
            ? new WatchControlReader(controlPipeName, projectLauncher.CompilationHandler, projectLauncher.Logger)
            : null;

        return new Launcher(serverPipeName, controlReader, projectLauncher, statusWriter, launchProfile, shutdownCancellationToken);
    }

    private sealed class Launcher : IRuntimeProcessLauncher
    {
        private const byte Version = 1;

        private readonly Optional<string?> _launchProfileName;
        private readonly Task _listenerTask;
        private readonly WatchStatusWriter? _statusWriter;
        private readonly WatchControlReader? _controlReader;
        private readonly ProjectLauncher _projectLauncher;

        private CancellationTokenSource? _disposalCancellationSource;
        private ImmutableHashSet<Task> _pendingRequestCompletions = [];

        public Launcher(
            string serverPipeName,
            WatchControlReader? controlReader,
            ProjectLauncher projectLauncher,
            WatchStatusWriter? statusWriter,
            Optional<string?> launchProfile,
            CancellationToken shutdownCancellationToken)
        {
            _projectLauncher = projectLauncher;
            _statusWriter = statusWriter;
            _launchProfileName = launchProfile;
            _controlReader = controlReader;
            _disposalCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownCancellationToken);
            _listenerTask = StartListeningAsync(serverPipeName, _disposalCancellationSource.Token);
        }

        public bool IsDisposed
            => _disposalCancellationSource == null;

        private ILogger Logger
            => _projectLauncher.Logger;

        public async ValueTask DisposeAsync()
        {
            var disposalCancellationSource = Interlocked.Exchange(ref _disposalCancellationSource, null);
            ObjectDisposedException.ThrowIf(disposalCancellationSource == null, this);

            Logger.LogDebug("Disposing process launcher.");
            await disposalCancellationSource.CancelAsync();

            if (_controlReader != null)
            {
                await _controlReader.DisposeAsync();
            }

            await _listenerTask;
            await Task.WhenAll(_pendingRequestCompletions);

            disposalCancellationSource.Dispose();
        }

        private async Task StartListeningAsync(string pipeName, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    NamedPipeServerStream? pipe = null;
                    try
                    {
                        pipe = new NamedPipeServerStream(
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
                            continue;
                        }

                        var json = await pipe.ReadStringAsync(cancellationToken);

                        var request = JsonSerializer.Deserialize<LaunchResourceRequest>(json) ?? throw new JsonException("Unexpected null");

                        Logger.LogDebug("Request received.");
                        await pipe.WriteAsync((byte)1, cancellationToken);

                        _ = HandleRequestAsync(request, pipe, cancellationToken);

                        // Don't dispose the pipe - it's now owned by HandleRequestAsync
                        // which will keep it alive for output proxying
                        pipe = null;
                    }
                    finally
                    {
                        if (pipe != null)
                        {
                            await pipe.DisposeAsync();
                        }
                    }
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
            using var pipeDisconnectedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var connectionToken = pipeDisconnectedSource.Token;

            try
            {
                var projectOptions = GetProjectOptions(request);

                await StartProjectAsync(projectOptions, pipe, currentProject, isRestart: currentProject.Value is not null, connectionToken);
                Debug.Assert(currentProject.Value != null);

                var projectLogger = currentProject.Value.ClientLogger;
                projectLogger.LogDebug("Waiting for resource to disconnect or relaunch.");

                await WaitForPipeDisconnectAsync(pipe, connectionToken);

                projectLogger.LogDebug("Resource pipe disconnected.");
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
                await pipeDisconnectedSource.CancelAsync();

                // Terminate the project process when the resource command disconnects.
                // This handles DCP Stop — the resource command is killed, pipe breaks,
                // and we clean up the project process the watch server launched.
                if (currentProject.Value is { } project)
                {
                    Logger.LogDebug("Pipe disconnected for '{Path}', terminating project process.", request.EntryPoint);
                    await project.Process.TerminateAsync();
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
        }

        private async ValueTask StartProjectAsync(ProjectOptions projectOptions, NamedPipeServerStream pipe, StrongBox<RunningProject?> currentProject, bool isRestart, CancellationToken cancellationToken)
        {
            // Buffer output through a channel to avoid blocking the synchronous onOutput callback.
            // The channel is drained asynchronously by DrainOutputChannelAsync which writes to the pipe.
            var outputChannel = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });

            var outputChannelDrainTask = WriteProcessOutputToPipeAsync();

            currentProject.Value = await _projectLauncher.TryLaunchProcessAsync(
                projectOptions,
                onOutput: line => outputChannel.Writer.TryWrite(line),
                onExit: async (processId, exitCode) =>
                {
                    var isRestarting = currentProject.Value?.IsRestarting == true;
                    if (exitCode is not null and not 0 && !cancellationToken.IsCancellationRequested && !isRestarting)
                    {
                        // Emit a status event for non-zero exit codes so the dashboard shows the crash.
                        // Skip if cancellation is requested (DCP Stop/shutdown) or if the project
                        // is being deliberately restarted (rude edit restart).
                        _statusWriter?.WriteEvent(new WatchStatusEvent
                        {
                            Type = WatchStatusEvent.Types.ProcessExited,
                            Projects = [projectOptions.Representation.ProjectOrEntryPointFilePath],
                            ExitCode = exitCode,
                        });
                    }

                    // DON'T complete the output channel.
                    // dotnet-watch will auto-retry on crash and reuse the same onOutput callback,
                    // so new output from the retried process flows through the same channel/pipe.
                    // Completing the channel would starve the pipe and cause DCP to kill the
                    // resource command, triggering a disconnect → terminate → reconnect storm.
                },
                restartOperation: async cancellationToken =>
                {
                    // Complete the old channel so the old drain task finishes before
                    // StartProjectAsync creates a new channel + drain on the same pipe.
                    outputChannel.Writer.TryComplete();
                    await outputChannelDrainTask;

                    await StartProjectAsync(projectOptions, pipe, currentProject, isRestart: true, cancellationToken);
                },
                cancellationToken)
                ?? throw new InvalidOperationException();

            // Emit ProcessStarted so the dashboard knows the process is actually running.
            _statusWriter?.WriteEvent(new WatchStatusEvent
            {
                Type = WatchStatusEvent.Types.ProcessStarted,
                Projects = [projectOptions.Representation.ProjectOrEntryPointFilePath],
            });

            async Task WriteProcessOutputToPipeAsync()
            {
                try
                {
                    await foreach (var line in outputChannel.Reader.ReadAllAsync(cancellationToken))
                    {
                        await pipe.WriteAsync(line.IsError ? AspireResourceLauncher.OutputTypeStderr : AspireResourceLauncher.OutputTypeStdout, cancellationToken);
                        await pipe.WriteAsync(line.Content, cancellationToken);
                    }
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
                {
                    // Pipe disconnected or cancelled
                }
            }
        }

        private ProjectOptions GetProjectOptions(LaunchResourceRequest request)
        {
            var project = ProjectRepresentation.FromProjectOrEntryPointFilePath(request.EntryPoint);

            return new()
            {
                IsMainProject = false,
                Representation = project,
                WorkingDirectory = Path.GetDirectoryName(request.EntryPoint) ?? throw new InvalidOperationException(),
                Command = "run",
                CommandArguments = GetRunCommandArguments(request, _launchProfileName.Value),
                LaunchEnvironmentVariables = request.EnvironmentVariables?.Select(e => (e.Key, e.Value))?.ToArray() ?? [],
                LaunchProfileName = request.LaunchProfileName,
            };
        }

        // internal for testing
        internal static IReadOnlyList<string> GetRunCommandArguments(LaunchResourceRequest request, string? hostLaunchProfile)
        {
            var arguments = new List<string>();

            if (!request.LaunchProfileName.HasValue)
            {
                arguments.Add("--no-launch-profile");
            }
            else if (!string.IsNullOrEmpty(request.LaunchProfileName.Value))
            {
                arguments.Add("--launch-profile");
                arguments.Add(request.LaunchProfileName.Value);
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
