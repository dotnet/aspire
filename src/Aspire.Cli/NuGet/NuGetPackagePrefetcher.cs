// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.NuGet;

internal sealed class NuGetPackagePrefetcher(ILogger<NuGetPackagePrefetcher> logger, INuGetPackageCache nuGetPackageCache, DirectoryInfo currentDirectory) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
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

        return Task.CompletedTask;
    }
}