// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal static class EnvironmentVariables
{
    public static class Names
    {
        public const string DotnetWatch = "DOTNET_WATCH";
        public const string DotnetWatchIteration = "DOTNET_WATCH_ITERATION";

        public const string DotnetLaunchProfile = "DOTNET_LAUNCH_PROFILE";
        public const string DotnetHostPath = "DOTNET_HOST_PATH";

        public const string DotNetWatchHotReloadNamedPipeName = HotReload.AgentEnvironmentVariables.DotNetWatchHotReloadNamedPipeName;
        public const string DotNetStartupHooks = HotReload.AgentEnvironmentVariables.DotNetStartupHooks;
        public const string DotNetModifiableAssemblies = HotReload.AgentEnvironmentVariables.DotNetModifiableAssemblies;
        public const string HotReloadDeltaClientLogMessages = HotReload.AgentEnvironmentVariables.HotReloadDeltaClientLogMessages;

        public const string SuppressBrowserRefresh = "DOTNET_WATCH_SUPPRESS_BROWSER_REFRESH";
    }

    public static LogLevel? CliLogLevel
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE");
            return string.Equals(value, "trace", StringComparison.OrdinalIgnoreCase)
                ? LogLevel.Trace
                : ParseBool(value)
                ? LogLevel.Debug
                : null;
        }
    }

    public static bool IsPollingEnabled => ReadBool("DOTNET_USE_POLLING_FILE_WATCHER");
    public static bool SuppressEmojis => ReadBool("DOTNET_WATCH_SUPPRESS_EMOJIS");
    public static bool RestartOnRudeEdit => ReadBool("DOTNET_WATCH_RESTART_ON_RUDE_EDIT");
    public static TimeSpan? ProcessCleanupTimeout => ReadTimeSpan("DOTNET_WATCH_PROCESS_CLEANUP_TIMEOUT_MS");

    public static string SdkRootDirectory =>
#if DEBUG
        Environment.GetEnvironmentVariable("DOTNET_WATCH_DEBUG_SDK_DIRECTORY") ?? "";
#else
        "";
#endif

    public static bool SuppressHandlingStaticWebAssets => ReadBool("DOTNET_WATCH_SUPPRESS_STATIC_FILE_HANDLING");
    public static bool SuppressMSBuildIncrementalism => ReadBool("DOTNET_WATCH_SUPPRESS_MSBUILD_INCREMENTALISM");
    public static bool SuppressLaunchBrowser => ReadBool("DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER");
    public static bool SuppressBrowserRefresh => ReadBool(Names.SuppressBrowserRefresh);

    public static TestFlags TestFlags => Environment.GetEnvironmentVariable("__DOTNET_WATCH_TEST_FLAGS") is { } value ? Enum.Parse<TestFlags>(value) : TestFlags.None;
    public static string TestOutputDir => Environment.GetEnvironmentVariable("__DOTNET_WATCH_TEST_OUTPUT_DIR") ?? "";

    public static string? AutoReloadWSHostName => Environment.GetEnvironmentVariable("DOTNET_WATCH_AUTO_RELOAD_WS_HOSTNAME");
    public static int? AutoReloadWSPort => ReadInt("DOTNET_WATCH_AUTO_RELOAD_WS_PORT");
    public static string? BrowserPath => Environment.GetEnvironmentVariable("DOTNET_WATCH_BROWSER_PATH");

    private static bool ReadBool(string variableName)
        => ParseBool(Environment.GetEnvironmentVariable(variableName));

    private static TimeSpan? ReadTimeSpan(string variableName)
        => Environment.GetEnvironmentVariable(variableName) is var value && long.TryParse(value, out var intValue) && intValue >= 0 ? TimeSpan.FromMilliseconds(intValue) : null;

    private static int? ReadInt(string variableName)
        => Environment.GetEnvironmentVariable(variableName) is var value && int.TryParse(value, out var intValue) ? intValue : null;

    private static bool ParseBool(string? value)
        => value == "1" || bool.TryParse(value, out var boolValue) && boolValue;
}
