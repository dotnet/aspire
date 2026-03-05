// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using Aspire.Tools.Service;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal class AspireServiceFactory(ProjectOptions hostProjectOptions) : IRuntimeProcessLauncherFactory
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
        private readonly ProjectOptions _hostProjectOptions;
        private readonly AspireServerService _service;
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

        // The number of sessions whose initialization is in progress.
        private int _pendingSessionInitializationCount;

        // Blocks disposal until no session initialization is in progress.
        private readonly SemaphoreSlim _postDisposalSessionInitializationCompleted = new(initialCount: 0, maxCount: 1);

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
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _logger.LogDebug("Disposing service factory ...");

            // stop accepting requests - triggers cancellation token for in-flight operations:
            await _service.DisposeAsync();

            // should not receive any more requests at this point:
            _isDisposed = true;

            // wait for all in-flight process initialization to complete:
            await _postDisposalSessionInitializationCompleted.WaitAsync(CancellationToken.None);

            // terminate all active sessions:
            ImmutableArray<Session> sessions;
            lock (_guard)
            {
                // caller guarantees the session is active
                sessions = [.. _sessions.Values];
                _sessions.Clear();
            }

            await Task.WhenAll(sessions.Select(TerminateSessionAsync)).WaitAsync(CancellationToken.None);

            _postDisposalSessionInitializationCompleted.Dispose();

            _logger.LogDebug("Service factory disposed");
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

        public async ValueTask StartProjectAsync(string dcpId, string sessionId, ProjectOptions projectOptions, bool isRestart, CancellationToken cancellationToken)
        {
            // Neither request from DCP nor restart should happen once the disposal has started.
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            _logger.LogDebug("[#{SessionId}] Starting: '{Path}'", sessionId, projectOptions.Representation.ProjectOrEntryPointFilePath);

            RunningProject? runningProject = null;
            var outputChannel = Channel.CreateUnbounded<OutputLine>(s_outputChannelOptions);

            Interlocked.Increment(ref _pendingSessionInitializationCount);

            try
            {
                runningProject = await _projectLauncher.TryLaunchProcessAsync(
                    projectOptions,
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
                    throw new ApplicationException($"Failed to launch '{projectOptions.Representation.ProjectOrEntryPointFilePath}'.");
                }

                await _service.NotifySessionStartedAsync(dcpId, sessionId, runningProject.Process.Id, cancellationToken);

                // cancel reading output when the process terminates:
                var outputReader = StartChannelReader(runningProject.Process.ExitedCancellationToken);

                lock (_guard)
                {
                    // When process is restarted we reuse the session id.
                    // The session already exists, it needs to be updated with new info.
                    Debug.Assert(_sessions.ContainsKey(sessionId) == isRestart);

                    _sessions[sessionId] = new Session(dcpId, sessionId, runningProject, outputReader);
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref _pendingSessionInitializationCount) == 0 && _isDisposed)
                {
                    _postDisposalSessionInitializationCompleted.Release();
                }
            }

            _logger.LogDebug("[#{SessionId}] Session started", sessionId);

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
            _logger.LogDebug("[#{SessionId}] Stop session", session.Id);

            await session.RunningProject.Process.TerminateAsync();

            // process termination should cancel output reader task:
            await session.OutputReader;
        }

        private ProjectOptions GetProjectOptions(ProjectLaunchRequest projectLaunchInfo)
            => new()
            {
                IsMainProject = false,
                Representation = ProjectRepresentation.FromProjectOrEntryPointFilePath(projectLaunchInfo.ProjectPath),
                WorkingDirectory = Path.GetDirectoryName(projectLaunchInfo.ProjectPath) ?? throw new InvalidOperationException(),
                Command = "run",
                CommandArguments = GetRunCommandArguments(projectLaunchInfo, _hostProjectOptions.LaunchProfileName.Value),
                LaunchEnvironmentVariables = projectLaunchInfo.Environment?.Select(e => (e.Key, e.Value))?.ToArray() ?? [],
                LaunchProfileName = projectLaunchInfo.DisableLaunchProfile ? default : projectLaunchInfo.LaunchProfile,
            };

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

    public const string AspireLogComponentName = "Aspire";
    public const string AppHostProjectCapability = ProjectCapability.Aspire;

    public IRuntimeProcessLauncher Create(ProjectLauncher projectLauncher)
        => new SessionManager(projectLauncher, hostProjectOptions);
}
