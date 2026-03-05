// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.DotNet.HotReload;

internal static class AgentEnvironmentVariables
{
    /// <summary>
    /// Intentionally different from the variable name used by the debugger.
    /// This is to avoid the debugger colliding with dotnet-watch pipe connection when debugging dotnet-watch (or tests).
    /// </summary>
    public const string DotNetWatchHotReloadNamedPipeName = "DOTNET_WATCH_HOTRELOAD_NAMEDPIPE_NAME";

    /// <summary>
    /// WebSocket endpoint for hot reload communication. Used for mobile platforms (Android, iOS)
    /// where named pipes don't work over the network.
    /// </summary>
    public const string DotNetWatchHotReloadWebSocketEndpoint = "DOTNET_WATCH_HOTRELOAD_WEBSOCKET_ENDPOINT";

    /// <summary>
    /// RSA public key (Base64-encoded X.509 SubjectPublicKeyInfo) for WebSocket connection authentication.
    /// The client encrypts a random secret with this key and sends it as the WebSocket subprotocol.
    /// </summary>
    public const string DotNetWatchHotReloadWebSocketKey = "DOTNET_WATCH_HOTRELOAD_WEBSOCKET_KEY";

    /// <summary>
    /// Enables logging from the client delta applier agent.
    /// </summary>
    public const string HotReloadDeltaClientLogMessages = "HOTRELOAD_DELTA_CLIENT_LOG_MESSAGES";

    /// <summary>
    /// dotnet runtime environment variable.
    /// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_startup_hooks
    /// </summary>
    public const string DotNetStartupHooks = "DOTNET_STARTUP_HOOKS";

    /// <summary>
    /// dotnet runtime environment variable.
    /// </summary>
    public const string DotNetModifiableAssemblies = "DOTNET_MODIFIABLE_ASSEMBLIES";
}
