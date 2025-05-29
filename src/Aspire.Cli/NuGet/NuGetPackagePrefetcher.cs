// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Aspire.Cli.NuGet;

internal sealed class NuGetPackagePrefetcher(INuGetPackageCache nuGetPackageCache) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        await nuGetPackageCache.GetTemplatePackagesAsync(
            workingDirectory: currentDirectory,
            prerelease: true,
            source: null,
            cancellationToken: stoppingToken
            );
    }
}