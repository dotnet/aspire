// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.NuGet;

internal sealed class NuGetPackagePrefetcher(ILogger<NuGetPackagePrefetcher> logger, CliExecutionContext executionContext, IFeatures features, IPackagingService packagingService, ICliUpdateNotifier cliUpdateNotifier) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Because of this: https://github.com/dotnet/aspire/issues/6956
        _ = Task.Run(async () =>
         {
             try
             {
                 var channels = await packagingService.GetChannelsAsync();

                 await Parallel.ForEachAsync(channels, stoppingToken, async (channel, ct) =>
                 {
                     // Discard the results here, we just want them in the cache.
                     _ = await channel.GetTemplatePackagesAsync(executionContext.WorkingDirectory, ct);
                 });
             }
             catch (System.Exception ex)
             {
                 logger.LogDebug(ex, "Non-fatal error while prefetching template packages. This is not critical to the operation of the CLI.");
                 // This prefetching is best effort. If it fails we log (above) and then the
                 // background service will exit gracefully. Code paths that depend on this
                 // data will handle the absence of pre-fetched packages gracefully.
             }
         }, stoppingToken);

        // Also prefetch CLI packages for update notifications
        _ = Task.Run(async () =>
        {
            if (features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, true))
            {
                try
                {
                    await cliUpdateNotifier.CheckForCliUpdatesAsync(
                        workingDirectory: executionContext.WorkingDirectory,
                        cancellationToken: stoppingToken
                        );
                }
                catch (System.Exception ex)
                {
                    logger.LogDebug(ex, "Non-fatal error while prefetching CLI packages. This is not critical to the operation of the CLI.");
                }
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }
}