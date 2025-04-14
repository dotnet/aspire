// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli;

internal interface INuGetPackageCache
{
    Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken);
    Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken);
}

internal sealed class NuGetPackageCache(ILogger<NuGetPackageCache> logger, IDotNetCliRunner cliRunner) : INuGetPackageCache
{
    private readonly ActivitySource _activitySource = new(nameof(NuGetPackageCache));

    private const int SearchPageSize = 100;

    public async Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return await GetPackagesAsync(workingDirectory, "Aspire.ProjectTemplates", prerelease, source, cancellationToken);
    }

    public async Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return await GetPackagesAsync(workingDirectory, "Aspire.Hosting", prerelease, source, cancellationToken);
    }

    internal async Task<IEnumerable<NuGetPackage>> GetPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        logger.LogDebug("Getting integrations from NuGet");

        var collectedPackages = new List<NuGetPackage>();
        var skip = 0;

        bool continueFetching;
        do
        {
            // This search should pick up Aspire.Hosting.* and CommunityToolkit.Aspire.Hosting.*
            var result = await cliRunner.SearchPackagesAsync(
                workingDirectory,
                query,
                prerelease,
                SearchPageSize,
                skip,
                source,
                cancellationToken
                );

            if (result.ExitCode != 0)
            {
                throw new NuGetPackageCacheException(
                    $"Failed to search for packages. Exit code: {result}");
            }
            else
            {
                if (result.Packages?.Length > 0)
                {
                    collectedPackages.AddRange(result.Packages);
                }

                if (result.Packages?.Length < SearchPageSize)
                {
                    continueFetching = false;
                }
                else
                {
                    continueFetching = true;
                    skip += SearchPageSize;
                }
            }
        } while (continueFetching);

        // For now we only return community toolkit packages.
        return collectedPackages.Where(p => IsOfficialOrCommunityToolkitPackage(p.Id));

        static bool IsOfficialOrCommunityToolkitPackage(string packageName)
        {
            var isHostingOrCommunityToolkitNamespaced = packageName.StartsWith("Aspire.Hosting.", StringComparison.Ordinal) ||
                   packageName.StartsWith("CommunityToolkit.Aspire.Hosting.", StringComparison.Ordinal) ||
                   packageName.Equals("Aspire.ProjectTemplates", StringComparison.Ordinal);
            
            var isExcluded = packageName.StartsWith("Aspire.Hosting.AppHost") ||
                             packageName.StartsWith("Aspire.Hosting.Sdk") ||
                             packageName.StartsWith("Aspire.Hosting.Orchestration") ||
                             packageName.StartsWith("Aspire.Hosting.Testing") ||
                             packageName.StartsWith("Aspire.Hosting.Msi");

            return isHostingOrCommunityToolkitNamespaced && !isExcluded;
        }
    }
}

internal sealed class NuGetPackageCacheException(string message) : Exception(message)
{
}
