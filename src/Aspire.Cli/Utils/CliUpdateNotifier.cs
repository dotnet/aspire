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
    Task NotifyIfUpdateAvailableAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken = default);
}

internal class CliUpdateNotifier(
    ILogger<CliUpdateNotifier> logger,
    INuGetPackageCache nuGetPackageCache,
    IInteractionService interactionService) : ICliUpdateNotifier
{

    public async Task NotifyIfUpdateAvailableAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            if (currentVersion is null)
            {
                logger.LogDebug("Unable to determine current CLI version for update check.");
                return;
            }

            var availablePackages = await nuGetPackageCache.GetCliPackagesAsync(workingDirectory, prerelease: true, source: null, cancellationToken);
            var newerVersion = PackageUpdateHelpers.GetNewerVersion(currentVersion, availablePackages);

            if (newerVersion is not null)
            {
                interactionService.DisplayVersionUpdateNotification(newerVersion.ToString());
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
