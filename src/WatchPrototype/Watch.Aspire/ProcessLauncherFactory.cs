// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class ProcessLauncherFactory(string namedPipe, CancellationToken shutdownCancellationToken) : IRuntimeProcessLauncherFactory
{
    public IRuntimeProcessLauncher Create(ProjectLauncher projectLauncher, string? launchProfile, string? targetFramework, IReadOnlyList<string> buildArguments)
        => new Launcher(namedPipe, projectLauncher, launchProfile, targetFramework, buildArguments, shutdownCancellationToken);

    private sealed class Launcher : IRuntimeProcessLauncher
    {
        private const byte Version = 1;

        private readonly ProjectLauncher _projectLauncher;
        private readonly string? _launchProfile;
        private readonly string? _targetFramework;
        private readonly IReadOnlyList<string> _buildArguments;
        private readonly Task _listenerTask;
        private readonly CancellationToken _shutdownCancellationToken;

        private ImmutableHashSet<Task> _pendingRequestCompletions = [];

        public Launcher(
            string namedPipe,
            ProjectLauncher projectLauncher,
            string? launchProfile,
            string? targetFramework,
            IReadOnlyList<string> buildArguments,
            CancellationToken shutdownCancellationToken)
        {
            _projectLauncher = projectLauncher;
            _launchProfile = launchProfile;
            _targetFramework = targetFramework;
            _buildArguments = buildArguments;

            _listenerTask = StartListeningAsync(namedPipe, _shutdownCancellationToken);
            _shutdownCancellationToken = shutdownCancellationToken;
        }

        public async ValueTask DisposeAsync()
        {
            Logger.LogDebug("Waiting for server to shutdown ...");

            await _listenerTask;
            await Task.WhenAll(_pendingRequestCompletions);
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

            try
            {
                await StartProjectAsync(GetProjectOptions(request), pipe, isRestart: false, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // nop
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to start '{Path}': {Exception}", request.EntryPoint, e.Message);
            }
            finally
            {
                await pipe.DisposeAsync();
            }

            ImmutableInterlocked.Update(ref _pendingRequestCompletions, set => set.Remove(completionSource.Task));
            completionSource.SetResult();
        }

        private async ValueTask<RunningProject> StartProjectAsync(ProjectOptions projectOptions, NamedPipeServerStream pipe, bool isRestart, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Starting: '{Path}'", projectOptions.Representation.ProjectOrEntryPointFilePath);

            // Use a SemaphoreSlim to serialize writes to the pipe from multiple threads
            var pipeLock = new SemaphoreSlim(1, 1);

            return await _projectLauncher.TryLaunchProcessAsync(
                projectOptions,
                processTerminationSource: new CancellationTokenSource(),
                onOutput: line =>
                {
                    // Write output to the pipe: [type byte] + [string content]
                    var typeByte = line.IsError ? AspireResourceLauncher.OutputTypeStderr : AspireResourceLauncher.OutputTypeStdout;
                    pipeLock.Wait(cancellationToken);
                    try
                    {
                        pipe.WriteAsync(typeByte, cancellationToken).AsTask().GetAwaiter().GetResult();
                        pipe.WriteAsync(line.Content, cancellationToken).AsTask().GetAwaiter().GetResult();
                    }
                    catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                    {
                        // Pipe disconnected, resource command exited
                    }
                    finally
                    {
                        pipeLock.Release();
                    }
                },
                onExit: async (processId, exitCode) =>
                {
                    // Write exit notification to the pipe: [type=0] + [exit code as string]
                    try
                    {
                        await pipeLock.WaitAsync(cancellationToken);
                        try
                        {
                            await pipe.WriteAsync(AspireResourceLauncher.OutputTypeExit, cancellationToken);
                            await pipe.WriteAsync((exitCode ?? -1).ToString(), cancellationToken);
                        }
                        finally
                        {
                            pipeLock.Release();
                        }
                    }
                    catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
                    {
                        // Pipe disconnected or cancellation
                    }
                },
                restartOperation: cancellationToken => StartProjectAsync(projectOptions, pipe, isRestart: true, cancellationToken),
                cancellationToken)
                ?? throw new InvalidOperationException();
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
