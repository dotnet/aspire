// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch
{
    [Flags]
    internal enum TestFlags
    {
        None = 0,
        RunningAsTest = 1 << 0,
        MockBrowser = 1 << 1,

        /// <summary>
        /// Instead of using <see cref="Console.ReadKey()"/> to watch for Ctrl+C, Ctlr+R, and other keys, read from standard input.
        /// This allows tests to trigger key based events.
        /// </summary>
        ReadKeyFromStdin = 1 << 2,

        /// <summary>
        /// Redirects the output of the launched browser process to watch output.
        /// </summary>
        RedirectBrowserOutput = 1 << 3,
    }

    internal sealed record EnvironmentOptions(
        string WorkingDirectory,
        string? SdkDirectory,
        string LogMessagePrefix,
        TimeSpan? ProcessCleanupTimeout = null,
        bool IsPollingEnabled = false,
        bool SuppressHandlingStaticWebAssets = false,
        bool SuppressMSBuildIncrementalism = false,
        bool SuppressLaunchBrowser = false,
        bool SuppressBrowserRefresh = false,
        bool SuppressEmojis = false,
        bool RestartOnRudeEdit = false,
        LogLevel? CliLogLevel = null,
        string? BrowserPath = null,
        WebSocketConfig BrowserWebSocketConfig = default,
        WebSocketConfig AgentWebSocketConfig = default,
        TestFlags TestFlags = TestFlags.None,
        string TestOutput = "")
    {
        public static EnvironmentOptions FromEnvironment(string? sdkDirectory, string logMessagePrefix) => new
        (
            WorkingDirectory: Directory.GetCurrentDirectory(),
            SdkDirectory: sdkDirectory,
            LogMessagePrefix: logMessagePrefix,
            ProcessCleanupTimeout: EnvironmentVariables.ProcessCleanupTimeout,
            IsPollingEnabled: EnvironmentVariables.IsPollingEnabled,
            SuppressHandlingStaticWebAssets: EnvironmentVariables.SuppressHandlingStaticWebAssets,
            SuppressMSBuildIncrementalism: EnvironmentVariables.SuppressMSBuildIncrementalism,
            SuppressLaunchBrowser: EnvironmentVariables.SuppressLaunchBrowser,
            SuppressBrowserRefresh: EnvironmentVariables.SuppressBrowserRefresh,
            SuppressEmojis: EnvironmentVariables.SuppressEmojis,
            RestartOnRudeEdit: EnvironmentVariables.RestartOnRudeEdit,
            CliLogLevel: EnvironmentVariables.CliLogLevel,
            BrowserPath: EnvironmentVariables.BrowserPath,
            BrowserWebSocketConfig: new(EnvironmentVariables.BrowserWebSocketPort, EnvironmentVariables.BrowserWebSocketSecurePort, EnvironmentVariables.BrowserWebSocketHostName),
            AgentWebSocketConfig: new(EnvironmentVariables.AgentWebSocketPort, EnvironmentVariables.AgentWebSocketSecurePort, hostName: null),
            TestFlags: EnvironmentVariables.TestFlags,
            TestOutput: EnvironmentVariables.TestOutputDir
        );

        public TimeSpan GetProcessCleanupTimeout()
            // Allow sufficient time for the process to exit gracefully and release resources (e.g., network ports).
            => ProcessCleanupTimeout ?? TimeSpan.FromSeconds(5);

        private readonly string? _muxerPath = SdkDirectory != null
            ? Path.GetFullPath(Path.Combine(SdkDirectory, "..", "..", "dotnet" + PathUtilities.ExecutableExtension))
            : null;

        public string GetMuxerPath()
            => _muxerPath ?? throw new InvalidOperationException("SDK directory is required to determine muxer path.");

        private int _uniqueLogId;

        public bool RunningAsTest { get => (TestFlags & TestFlags.RunningAsTest) != TestFlags.None; }

        public string? GetBinLogPath(string projectPath, string operationName, GlobalOptions options)
            => options.BinaryLogPath != null
               ? $"{Path.Combine(WorkingDirectory, options.BinaryLogPath)[..^".binlog".Length]}-dotnet-watch.{operationName}.{Path.GetFileName(projectPath)}.{Interlocked.Increment(ref _uniqueLogId)}.binlog"
               : null;
    }
}
