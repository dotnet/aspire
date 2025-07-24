// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.NuGet;

internal sealed class NuGetPackagePrefetcher(ILogger<NuGetPackagePrefetcher> logger, INuGetPackageCache nuGetPackageCache, DirectoryInfo currentDirectory, IFeatures features) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Because of this: https://github.com/dotnet/aspire/issues/6956
        _ = Task.Run(async () =>
        {
            try
            {
                // Prefetch template packages
                await nuGetPackageCache.GetTemplatePackagesAsync(
                    workingDirectory: currentDirectory,
                    prerelease: true,
                    source: null,
                    cancellationToken: stoppingToken
                    );
            }
            catch (System.Exception ex)
            {
                logger.LogDebug(ex, "Non-fatal error while prefetching template packages. This is not critical to the operation of the CLI.");
                // This prefetching is best effort. If it fails we log (above) and then the
                // background service will exit gracefully. Code paths that depend on this
                // data will handle the absence of pre-fetched packages gracefully.
            }
        }, stoppingToken);

        if (features.IsFeatureEnabled(KnownFeatures.UpdateNotificationsEnabled, true))
        {
            // Also prefetch CLI packages for update notifications
                _ = Task.Run(async () =>
            {
                try
                {
                    await nuGetPackageCache.GetCliPackagesAsync(
                        workingDirectory: currentDirectory,
                        prerelease: true,
                        source: null,
                        cancellationToken: stoppingToken
                        );
                }
                catch (System.Exception ex)
                {
                    logger.LogDebug(ex, "Non-fatal error while prefetching CLI packages. This is not critical to the operation of the CLI.");
                }
            }, stoppingToken);
        }

        return Task.CompletedTask;
    }
}