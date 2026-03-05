// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal abstract class AspireWatcherLauncher(GlobalOptions globalOptions, EnvironmentOptions environmentOptions)
    : AspireLauncher(globalOptions, environmentOptions)
{
    protected async Task<int> LaunchWatcherAsync(
        ImmutableArray<ProjectRepresentation> rootProjects,
        ILoggerFactory loggerFactory,
        IRuntimeProcessLauncherFactory? processLauncherFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory != LoggerFactory
            ? loggerFactory.CreateLogger(DotNetWatchContext.DefaultLogComponentName)
            : Logger;

        using var context = new DotNetWatchContext()
        {
            ProcessOutputReporter = Reporter,
            LoggerFactory = loggerFactory,
            Logger = logger,
            BuildLogger = loggerFactory.CreateLogger(DotNetWatchContext.BuildLogComponentName),
            ProcessRunner = new ProcessRunner(EnvironmentOptions.GetProcessCleanupTimeout()),
            Options = GlobalOptions,
            EnvironmentOptions = EnvironmentOptions,
            MainProjectOptions = null,
            BuildArguments = [],
            TargetFramework = null,
            RootProjects = rootProjects,
            BrowserRefreshServerFactory = new BrowserRefreshServerFactory(),
            BrowserLauncher = new BrowserLauncher(logger, Reporter, EnvironmentOptions),
        };

        using var shutdownHandler = new ShutdownHandler(Console, context.Logger);
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdownHandler.CancellationToken);

        try
        {
            var watcher = new HotReloadDotNetWatcher(context, Console, processLauncherFactory);
            await watcher.WatchAsync(cancellationSource.Token);
        }
        catch (OperationCanceledException) when (shutdownHandler.CancellationToken.IsCancellationRequested)
        {
            // Ctrl+C forced an exit
        }

        return 0;
    }
}
