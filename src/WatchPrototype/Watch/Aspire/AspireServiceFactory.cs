// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using Aspire.Tools.Service;
using Microsoft.Build.Graph;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal class AspireServiceFactory : IRuntimeProcessLauncherFactory
{
    internal sealed class SessionManager : IAspireServerEvents, IRuntimeProcessLauncher
    {
        private readonly struct Session(string dcpId, string sessionId, RunningProject runningProject, Task outputReader)
        {
            public string DcpId { get; } = dcpId;
            public string Id { get; } = sessionId;
            public RunningProject RunningProject { get; } = runningProject;
            public Task OutputReader { get; } = outputReader;
        }

        private static readonly UnboundedChannelOptions s_outputChannelOptions = new()
        {
            SingleReader = true,
            SingleWriter = true
        };

        private readonly ProjectLauncher _projectLauncher;
        private readonly AspireServerService _service;
        private readonly ProjectOptions _hostProjectOptions;
        private readonly ILogger _logger;

        /// <summary>
        /// Lock to access:
        /// <see cref="_sessions"/>
        /// <see cref="_sessionIdDispenser"/>
        /// </summary>
        private readonly object _guard = new();

        private readonly Dictionary<string, Session> _sessions = [];
        private int _sessionIdDispenser;

        private volatile bool _isDisposed;

        public SessionManager(ProjectLauncher projectLauncher, ProjectOptions hostProjectOptions)
        {
            _projectLauncher = projectLauncher;
            _hostProjectOptions = hostProjectOptions;
            _logger = projectLauncher.LoggerFactory.CreateLogger(AspireLogComponentName);

            _service = new AspireServerService(
                this,
                displayName: ".NET Watch Aspire Server",
                m => _logger.LogDebug(m));
        }

        public async ValueTask DisposeAsync()
        {
#if DEBUG
            lock (_guard)
            {
                Debug.Assert(_sessions.Count == 0);
            }
#endif
            _isDisposed = true;

            await _service.DisposeAsync();
        }

        public async ValueTask TerminateLaunchedProcessesAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            ImmutableArray<Session> sessions;
            lock (_guard)
            {
                // caller guarantees the session is active
                sessions = [.. _sessions.Values];
                _sessions.Clear();
            }

            await Task.WhenAll(sessions.Select(TerminateSessionAsync)).WaitAsync(cancellationToken);
        }

        public IEnumerable<(string name, string value)> GetEnvironmentVariables()
            => _service.GetServerConnectionEnvironment().Select(kvp => (kvp.Key, kvp.Value));

        /// <summary>
        /// Implements https://github.com/dotnet/aspire/blob/445d2fc8a6a0b7ce3d8cc42def4d37b02709043b/docs/specs/IDE-execution.md#create-session-request.
        /// </summary>
        async ValueTask<string> IAspireServerEvents.StartProjectAsync(string dcpId, ProjectLaunchRequest projectLaunchInfo, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            var projectOptions = GetProjectOptions(projectLaunchInfo);
            var sessionId = Interlocked.Increment(ref _sessionIdDispenser).ToString(CultureInfo.InvariantCulture);
            await StartProjectAsync(dcpId, sessionId, projectOptions, isRestart: false, cancellationToken);
            return sessionId;
        }

        public async ValueTask<RunningProject> StartProjectAsync(string dcpId, string sessionId, ProjectOptions projectOptions, bool isRestart, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _logger.LogDebug("Starting project: {Path}", projectOptions.ProjectPath);

            var processTerminationSource = new CancellationTokenSource();
            var outputChannel = Channel.CreateUnbounded<OutputLine>(s_outputChannelOptions);

            RunningProject? runningProject = null;

            runningProject = await _projectLauncher.TryLaunchProcessAsync(
                projectOptions,
                processTerminationSource,
                onOutput: line =>
                {
                    var writeResult = outputChannel.Writer.TryWrite(line);
                    Debug.Assert(writeResult);
                },
                onExit: async (processId, exitCode) =>
                {
                    // Project can be null if the process exists while it's being initialized.
                    if (runningProject?.IsRestarting == false)
                    {
                        try
                        {
                            await _service.NotifySessionEndedAsync(dcpId, sessionId, processId, exitCode, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // canceled on shutdown, ignore
                        }
                    }
                },
                restartOperation: cancellationToken =>
                    StartProjectAsync(dcpId, sessionId, projectOptions, isRestart: true, cancellationToken),
                cancellationToken);

            if (runningProject == null)
            {
                // detailed error already reported:
                throw new ApplicationException($"Failed to launch project '{projectOptions.ProjectPath}'.");
            }

            await _service.NotifySessionStartedAsync(dcpId, sessionId, runningProject.ProcessId, cancellationToken);

            // cancel reading output when the process terminates:
            var outputReader = StartChannelReader(runningProject.ProcessExitedCancellationToken);

            lock (_guard)
            {
                // When process is restarted we reuse the session id.
                // The session already exists, it needs to be updated with new info.
                Debug.Assert(_sessions.ContainsKey(sessionId) == isRestart);

                _sessions[sessionId] = new Session(dcpId, sessionId, runningProject, outputReader);
            }

            _logger.LogDebug("Session started: #{SessionId}", sessionId);
            return runningProject;

            async Task StartChannelReader(CancellationToken cancellationToken)
            {
                try
                {
                    await foreach (var line in outputChannel.Reader.ReadAllAsync(cancellationToken))
                    {
                        await _service.NotifyLogMessageAsync(dcpId, sessionId, isStdErr: line.IsError, data: line.Content, cancellationToken);
                    }
                }
                catch (Exception e)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError("Unexpected error reading output of session '{SessionId}': {Exception}", sessionId, e);
                    }
                }
            }
        }

        /// <summary>
        /// Implements https://github.com/dotnet/aspire/blob/445d2fc8a6a0b7ce3d8cc42def4d37b02709043b/docs/specs/IDE-execution.md#stop-session-request.
        /// </summary>
        async ValueTask<bool> IAspireServerEvents.StopSessionAsync(string dcpId, string sessionId, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            Session session;
            lock (_guard)
            {
                if (!_sessions.TryGetValue(sessionId, out session))
                {
                    return false;
                }

                _sessions.Remove(sessionId);
            }

            await TerminateSessionAsync(session);
            return true;
        }

        private async Task TerminateSessionAsync(Session session)
        {
            _logger.LogDebug("Stop session #{SessionId}", session.Id);

            await session.RunningProject.TerminateAsync();

            // process termination should cancel output reader task:
            await session.OutputReader;
        }

        private ProjectOptions GetProjectOptions(ProjectLaunchRequest projectLaunchInfo)
        {
            var hostLaunchProfile = _hostProjectOptions.NoLaunchProfile ? null : _hostProjectOptions.LaunchProfileName;

            return new()
            {
                IsRootProject = false,
                ProjectPath = projectLaunchInfo.ProjectPath,
                WorkingDirectory = Path.GetDirectoryName(projectLaunchInfo.ProjectPath) ?? throw new InvalidOperationException(),
                BuildArguments = _hostProjectOptions.BuildArguments,
                Command = "run",
                CommandArguments = GetRunCommandArguments(projectLaunchInfo, hostLaunchProfile),
                LaunchEnvironmentVariables = projectLaunchInfo.Environment?.Select(e => (e.Key, e.Value))?.ToArray() ?? [],
                LaunchProfileName = projectLaunchInfo.LaunchProfile,
                NoLaunchProfile = projectLaunchInfo.DisableLaunchProfile,
                TargetFramework = _hostProjectOptions.TargetFramework,
            };
        }

        // internal for testing
        internal static IReadOnlyList<string> GetRunCommandArguments(ProjectLaunchRequest projectLaunchInfo, string? hostLaunchProfile)
        {
            var arguments = new List<string>
            {
                "--project",
                projectLaunchInfo.ProjectPath,
            };

            // Implements https://github.com/dotnet/aspire/blob/main/docs/specs/IDE-execution.md#launch-profile-processing-project-launch-configuration

            if (projectLaunchInfo.DisableLaunchProfile)
            {
                arguments.Add("--no-launch-profile");
            }
            else if (!string.IsNullOrEmpty(projectLaunchInfo.LaunchProfile))
            {
                arguments.Add("--launch-profile");
                arguments.Add(projectLaunchInfo.LaunchProfile);
            }
            else if (hostLaunchProfile != null)
            {
                arguments.Add("--launch-profile");
                arguments.Add(hostLaunchProfile);
            }

            if (projectLaunchInfo.Arguments != null)
            {
                if (projectLaunchInfo.Arguments.Any())
                {
                    arguments.AddRange(projectLaunchInfo.Arguments);
                }
                else
                {
                    // indicate that no arguments should be used even if launch profile specifies some:
                    arguments.Add("--no-launch-profile-arguments");
                }
            }

            return arguments;
        }
    }

    public static readonly AspireServiceFactory Instance = new();

    public const string AspireLogComponentName = "Aspire";
    public const string AppHostProjectCapability = ProjectCapability.Aspire;

    public IRuntimeProcessLauncher? TryCreate(ProjectGraphNode projectNode, ProjectLauncher projectLauncher, ProjectOptions hostProjectOptions)
        => projectNode.GetCapabilities().Contains(AppHostProjectCapability)
            ? new SessionManager(projectLauncher, hostProjectOptions)
            : null;
}
