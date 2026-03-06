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
        public const string DotNetWatchHotReloadWebSocketEndpoint = HotReload.AgentEnvironmentVariables.DotNetWatchHotReloadWebSocketEndpoint;
        public const string DotNetWatchAgentWebSocketPort = "DOTNET_WATCH_AGENT_WEBSOCKET_PORT";
        public const string DotNetWatchAgentWebSocketSecurePort = "DOTNET_WATCH_AGENT_WEBSOCKET_SECURE_PORT";
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
    public static TimeSpan? ProcessCleanupTimeout => ReadTimeSpanMilliseconds("DOTNET_WATCH_PROCESS_CLEANUP_TIMEOUT_MS");

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

    public static string? BrowserWebSocketHostName => Environment.GetEnvironmentVariable("DOTNET_WATCH_AUTO_RELOAD_WS_HOSTNAME");

    /// <summary>
    /// Port used for browser WebSocket communication. Defaults to 0 (auto-assign) if not specified.
    /// </summary>
    public static int BrowserWebSocketPort => ReadInt("DOTNET_WATCH_AUTO_RELOAD_WS_PORT") ?? 0;

    /// <summary>
    /// Secure (HTTPS/WSS) port used for browser WebSocket communication. Defaults to 0 (auto-assign) if not specified.
    /// Only used if TLS is supported and enabled.
    /// </summary>
    public static int BrowserWebSocketSecurePort => ReadInt("DOTNET_WATCH_AUTO_RELOAD_WSS_PORT") ?? 0;

    public static string? BrowserPath => Environment.GetEnvironmentVariable("DOTNET_WATCH_BROWSER_PATH");

    /// <summary>
    /// Port for WebSocket hot reload communication. Used for projects with the HotReloadWebSockets capability.
    /// Mobile workloads (Android, iOS) add this capability. Defaults to 0 (auto-assign) if not specified.
    /// </summary>
    public static int AgentWebSocketPort => ReadInt(Names.DotNetWatchAgentWebSocketPort) ?? 0;

    /// <summary>
    /// Secure (HTTPS/WSS) port for WebSocket hot reload communication.
    /// If not specified, HTTPS is not enabled for the agent WebSocket server.
    /// </summary>
    public static int? AgentWebSocketSecurePort => ReadInt(Names.DotNetWatchAgentWebSocketSecurePort);

    private static bool ReadBool(string variableName)
        => ParseBool(Environment.GetEnvironmentVariable(variableName));

    internal static TimeSpan? ReadTimeSpanMilliseconds(string variableName)
        => Environment.GetEnvironmentVariable(variableName) is var value && long.TryParse(value, out var intValue) && intValue >= 0 ? TimeSpan.FromMilliseconds(intValue) : null;

    internal static TimeSpan? ReadTimeSpanSeconds(string variableName)
        => Environment.GetEnvironmentVariable(variableName) is var value && long.TryParse(value, out var intValue) && intValue >= 0 ? TimeSpan.FromSeconds(intValue) : null;

    private static int? ReadInt(string variableName)
        => Environment.GetEnvironmentVariable(variableName) is var value && int.TryParse(value, out var intValue) ? intValue : null;

    private static bool ParseBool(string? value)
        => value == "1" || bool.TryParse(value, out var boolValue) && boolValue;
}
