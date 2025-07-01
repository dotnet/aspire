// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
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
            var newerVersion = GetNewerVersion(currentVersion, availablePackages);

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
        try
        {
            var versionString = VersionHelper.GetDefaultTemplateVersion();
            // Remove any build metadata (e.g., +sha.12345) for comparison
            var cleanVersionString = versionString.Split('+')[0];
            return SemVersion.Parse(cleanVersionString, SemVersionStyles.Strict);
        }
        catch
        {
            return null;
        }
    }

    private static SemVersion? GetNewerVersion(SemVersion currentVersion, IEnumerable<NuGetPackage> availablePackages)
    {
        SemVersion? newestStable = null;
        SemVersion? newestPrerelease = null;

        foreach (var package in availablePackages)
        {
            if (SemVersion.TryParse(package.Version, SemVersionStyles.Strict, out var version))
            {
                if (version.IsPrerelease)
                {
                    newestPrerelease = newestPrerelease is null || SemVersion.PrecedenceComparer.Compare(version, newestPrerelease) > 0 ? version : newestPrerelease;
                }
                else
                {
                    newestStable = newestStable is null || SemVersion.PrecedenceComparer.Compare(version, newestStable) > 0 ? version : newestStable;
                }
            }
        }

        // Apply notification rules
        if (currentVersion.IsPrerelease)
        {
            // Rule 1: If using a prerelease version where the version is lower than the latest stable version, prompt to upgrade
            if (newestStable is not null && SemVersion.PrecedenceComparer.Compare(currentVersion, newestStable) < 0)
            {
                return newestStable;
            }

            // Rule 2: If using a prerelease version and there is a newer prerelease version, prompt to upgrade
            if (newestPrerelease is not null && SemVersion.PrecedenceComparer.Compare(currentVersion, newestPrerelease) < 0)
            {
                return newestPrerelease;
            }
        }
        else
        {
            // Rule 3: If using a stable version and there is a newer stable version, prompt to upgrade
            if (newestStable is not null && SemVersion.PrecedenceComparer.Compare(currentVersion, newestStable) < 0)
            {
                return newestStable;
            }
        }

        return null;
    }
}