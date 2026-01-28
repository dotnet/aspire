// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
        string MuxerPath,
        TimeSpan? ProcessCleanupTimeout,
        bool IsPollingEnabled = false,
        bool SuppressHandlingStaticWebAssets = false,
        bool SuppressMSBuildIncrementalism = false,
        bool SuppressLaunchBrowser = false,
        bool SuppressBrowserRefresh = false,
        bool SuppressEmojis = false,
        bool RestartOnRudeEdit = false,
        LogLevel? CliLogLevel = null,
        string? AutoReloadWebSocketHostName = null,
        int? AutoReloadWebSocketPort = null,
        string? BrowserPath = null,
        TestFlags TestFlags = TestFlags.None,
        string TestOutput = "")
    {
        public static EnvironmentOptions FromEnvironment(string muxerPath) => new
        (
            WorkingDirectory: Directory.GetCurrentDirectory(),
            MuxerPath: ValidateMuxerPath(muxerPath),
            ProcessCleanupTimeout: EnvironmentVariables.ProcessCleanupTimeout,
            IsPollingEnabled: EnvironmentVariables.IsPollingEnabled,
            SuppressHandlingStaticWebAssets: EnvironmentVariables.SuppressHandlingStaticWebAssets,
            SuppressMSBuildIncrementalism: EnvironmentVariables.SuppressMSBuildIncrementalism,
            SuppressLaunchBrowser: EnvironmentVariables.SuppressLaunchBrowser,
            SuppressBrowserRefresh: EnvironmentVariables.SuppressBrowserRefresh,
            SuppressEmojis: EnvironmentVariables.SuppressEmojis,
            RestartOnRudeEdit: EnvironmentVariables.RestartOnRudeEdit,
            CliLogLevel: EnvironmentVariables.CliLogLevel,
            AutoReloadWebSocketHostName: EnvironmentVariables.AutoReloadWSHostName,
            AutoReloadWebSocketPort: EnvironmentVariables.AutoReloadWSPort,
            BrowserPath: EnvironmentVariables.BrowserPath,
            TestFlags: EnvironmentVariables.TestFlags,
            TestOutput: EnvironmentVariables.TestOutputDir
        );

        public TimeSpan GetProcessCleanupTimeout(bool isHotReloadEnabled)
            // Allow sufficient time for the process to exit gracefully and release resources (e.g., network ports).
            => ProcessCleanupTimeout ?? TimeSpan.FromSeconds(5);

        private int _uniqueLogId;

        public bool RunningAsTest { get => (TestFlags & TestFlags.RunningAsTest) != TestFlags.None; }

        private static string ValidateMuxerPath(string path)
        {
            Debug.Assert(Path.GetFileNameWithoutExtension(path) == "dotnet");
            return path;
        }

        public string? GetBinLogPath(string projectPath, string operationName, GlobalOptions options)
            => options.BinaryLogPath != null
               ? $"{Path.Combine(WorkingDirectory, options.BinaryLogPath)[..^".binlog".Length]}-dotnet-watch.{operationName}.{Path.GetFileName(projectPath)}.{Interlocked.Increment(ref _uniqueLogId)}.binlog"
               : null;
    }
}
