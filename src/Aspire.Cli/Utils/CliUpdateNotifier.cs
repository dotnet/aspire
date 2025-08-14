// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Shared;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Utils;

internal interface ICliUpdateNotifier
{
    Task NotifyIfUpdateAvailableAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken);
}

internal class CliUpdateNotifier(
    ILogger<CliUpdateNotifier> logger,
    INuGetPackageCache nuGetPackageCache,
    IInteractionService interactionService) : ICliUpdateNotifier
{

    public async Task NotifyIfUpdateAvailableAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            if (currentVersion is null)
            {
                logger.LogDebug("Unable to determine current CLI version for update check.");
                return;
            }

            // Ultimately the package nuget cache invokes dotnet CLI runner
            // which launches the dotnet package search command. It can take some
            // time for the wait on this process to be cancelled and for it to unwind
            // so this change makes it so that we can detect cancellation on this
            // side and exit gracefully if we didn't already have a cached result.
            var tcs = new TaskCompletionSource();
            cancellationToken.Register(() => tcs.TrySetResult());

            var pendingAvailablePackages = nuGetPackageCache.GetCliPackagesAsync(
                    workingDirectory: workingDirectory,
                    prerelease: true,
                    nugetConfigFile: null,
                    cancellationToken: cancellationToken);

            await Task.WhenAny(tcs.Task, pendingAvailablePackages);

            if (!cancellationToken.IsCancellationRequested)
            {
                var availablePackages = await pendingAvailablePackages;
                var newerVersion = PackageUpdateHelpers.GetNewerVersion(currentVersion, availablePackages);

                if (newerVersion is not null)
                {
                    interactionService.DisplayVersionUpdateNotification(newerVersion.ToString());
                }
            }
                    
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Non-fatal error while checking for CLI updates.");
        }
    }

    protected virtual SemVersion? GetCurrentVersion()
    {
        return PackageUpdateHelpers.GetCurrentPackageVersion();
    }
}
