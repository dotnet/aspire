// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;
using Semver;

namespace Aspire.Cli.Updater;

internal interface IUpdaterService
{
    Task<(bool UpdateAvailable, string? AdvertisedVersion)> CheckForCliUpdateAsync(CancellationToken cancellationToken);
}

internal sealed class UpdaterService(DotNetCliRunner runner) : IUpdaterService
{
    public async Task<(bool UpdateAvailable, string? AdvertisedVersion)> CheckForCliUpdateAsync(CancellationToken cancellationToken)
    {
        var informationalVersion = VersionHelper.GetInformationalVersion();
        var parsedInformationalVersion = SemVersion.Parse(informationalVersion);
        var includePrerelease = parsedInformationalVersion.IsPrerelease;

        var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        var searchResult = await runner.SearchPackagesAsync(
            workingDirectory,
            "Aspire.Cli",
            includePrerelease,
            100,
            0,
            null,
            cancellationToken);

        var latestVersions = searchResult.Packages?
            .Where(p => p.Id == "Aspire.Cli") // Make sure we are only looking at the Aspire.Cli package.
            .Select(p => SemVersion.Parse(p.Version)) // Project to a semver version object so we can do comparisons.
            .ToList();

        latestVersions?.Add(SemVersion.Parse("9.4.0"));
        latestVersions?.Add(SemVersion.Parse("9.5.0-preview"));
        latestVersions?.Add(SemVersion.Parse("9.3.0"));

        latestVersions?.Sort(SemVersion.CompareSortOrder);

        if (latestVersions is not null && latestVersions.Count > 0)
        {
            var latestVersion = latestVersions.Last();

            // Check if the latest version is greater than the current version.
            if (latestVersion.ComparePrecedenceTo(parsedInformationalVersion) > 0)
            {
                return (true, latestVersion.ToString());
            }
            else
            {
                return (false, null);
            }
        }
        else
        {
            return (false, null);
        }
    }
}