// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal abstract class WebApplicationAppModel(DotNetWatchContext context) : HotReloadAppModel
{
    // This needs to be in sync with the version BrowserRefreshMiddleware is compiled against.
    private static readonly Version s_minimumSupportedVersion = Versions.Version6_0;
    private const string MiddlewareTargetFramework = "net6.0";

    public DotNetWatchContext Context => context;

    public abstract bool RequiresBrowserRefresh { get; }

    /// <summary>
    /// Project that's used for launching the application.
    /// </summary>
    public abstract ProjectGraphNode LaunchingProject { get; }

    protected abstract HotReloadClients CreateClients(ILogger clientLogger, ILogger agentLogger, BrowserRefreshServer? browserRefreshServer);

    public async sealed override ValueTask<HotReloadClients?> TryCreateClientsAsync(ILogger clientLogger, ILogger agentLogger, CancellationToken cancellationToken)
    {
        var browserRefreshServer = await context.BrowserRefreshServerFactory.GetOrCreateBrowserRefreshServerAsync(LaunchingProject, this, cancellationToken);
        if (RequiresBrowserRefresh && browserRefreshServer == null)
        {
            // Error has been reported
            return null;
        }

        return CreateClients(clientLogger, agentLogger, browserRefreshServer);
    }

    protected WebAssemblyHotReloadClient CreateWebAssemblyClient(ILogger clientLogger, ILogger agentLogger, BrowserRefreshServer browserRefreshServer, ProjectGraphNode clientProject)
    {
        var capabilities = clientProject.GetWebAssemblyCapabilities().ToImmutableArray();
        var targetFramework = clientProject.GetTargetFrameworkVersion() ?? throw new InvalidOperationException($"Project doesn't define {PropertyNames.TargetFrameworkMoniker}");

        return new WebAssemblyHotReloadClient(clientLogger, agentLogger, browserRefreshServer, capabilities, targetFramework, context.EnvironmentOptions.TestFlags.HasFlag(TestFlags.MockBrowser));
    }

    private static string GetMiddlewareAssemblyPath()
        => GetInjectedAssemblyPath(MiddlewareTargetFramework, "Microsoft.AspNetCore.Watch.BrowserRefresh");

    public BrowserRefreshServer? TryCreateRefreshServer(ProjectGraphNode projectNode)
    {
        var logger = context.LoggerFactory.CreateLogger(BrowserRefreshServer.ServerLogComponentName, projectNode.GetDisplayName());

        if (IsServerSupported(projectNode, logger))
        {
            return new BrowserRefreshServer(
                logger,
                context.LoggerFactory,
                middlewareAssemblyPath: GetMiddlewareAssemblyPath(),
                dotnetPath: context.EnvironmentOptions.MuxerPath,
                autoReloadWebSocketHostName: context.EnvironmentOptions.AutoReloadWebSocketHostName,
                autoReloadWebSocketPort: context.EnvironmentOptions.AutoReloadWebSocketPort,
                suppressTimeouts: context.EnvironmentOptions.TestFlags != TestFlags.None);
        }

        return null;
    }

    public bool IsServerSupported(ProjectGraphNode projectNode, ILogger logger)
    {
        if (context.EnvironmentOptions.SuppressBrowserRefresh)
        {
            logger.Log(MessageDescriptor.SkippingConfiguringBrowserRefresh_SuppressedViaEnvironmentVariable.WithLevelWhen(LogLevel.Error, RequiresBrowserRefresh), EnvironmentVariables.Names.SuppressBrowserRefresh);
            return false;
        }

        if (!projectNode.IsNetCoreApp(minVersion: s_minimumSupportedVersion))
        {
            logger.Log(MessageDescriptor.SkippingConfiguringBrowserRefresh_TargetFrameworkNotSupported.WithLevelWhen(LogLevel.Error, RequiresBrowserRefresh));
            return false;
        }

        logger.Log(MessageDescriptor.ConfiguredToUseBrowserRefresh);
        return true;
    }
}
