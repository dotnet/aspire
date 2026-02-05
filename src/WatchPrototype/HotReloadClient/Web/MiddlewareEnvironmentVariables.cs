// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.DotNet.HotReload;

internal static class MiddlewareEnvironmentVariables
{
    /// <summary>
    /// dotnet runtime environment variable used to load middleware assembly into the web server process.
    /// https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_startup_hooks
    /// </summary>
    public const string DotNetStartupHooks = "DOTNET_STARTUP_HOOKS";

    /// <summary>
    /// dotnet runtime environment variable.
    /// </summary>
    public const string DotNetModifiableAssemblies = "DOTNET_MODIFIABLE_ASSEMBLIES";

    /// <summary>
    /// Simple names of assemblies that implement middleware components to be added to the web server.
    /// </summary>
    public const string AspNetCoreHostingStartupAssemblies = "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";
    public const char AspNetCoreHostingStartupAssembliesSeparator = ';';

    /// <summary>
    /// Comma-separated list of WebSocket end points to communicate with browser refresh client.
    /// </summary>
    public const string AspNetCoreAutoReloadWSEndPoint = "ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT";

    public const string AspNetCoreAutoReloadVirtualDirectory = "ASPNETCORE_AUTO_RELOAD_VDIR";

    /// <summary>
    /// Public key to use to communicate with browser refresh client.
    /// </summary>
    public const string AspNetCoreAutoReloadWSKey = "ASPNETCORE_AUTO_RELOAD_WS_KEY";

    /// <summary>
    /// Variable used to set the logging level of the middleware logger.
    /// </summary>
    public const string LoggingLevel = "Logging__LogLevel__Microsoft.AspNetCore.Watch";
}
