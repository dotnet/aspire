// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;
using System.Globalization;
using Aspire.Cli.Telemetry;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.NuGet;

internal interface INuGetPackageCache
{
    Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken);
    Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken);
    Task<IEnumerable<NuGetPackage>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken);
}

internal sealed class NuGetPackageCache(ILogger<NuGetPackageCache> logger, IDotNetCliRunner cliRunner, IMemoryCache memoryCache, AspireCliTelemetry telemetry) : INuGetPackageCache
{

    private const int SearchPageSize = 1000;

    public async Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        var key = $"TemplatePackages-{workingDirectory.FullName}-{prerelease}-{source}";

        var packages = await memoryCache.GetOrCreateAsync(key, async (entry) =>
        {
            return await GetPackagesAsync(workingDirectory, "Aspire.ProjectTemplates", prerelease, source, cancellationToken);
        }) ?? throw new NuGetPackageCacheException(ErrorStrings.FailedToRetrieveCachedTemplatePackages);

        return packages;
    }

    public async Task<IEnumerable<NuGetPackage>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return await GetPackagesAsync(workingDirectory, "Aspire.Hosting", prerelease, source, cancellationToken);
    }

    public async Task<IEnumerable<NuGetPackage>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        var key = $"CliPackages-{workingDirectory.FullName}-{prerelease}-{source}";

        var packages = await memoryCache.GetOrCreateAsync(key, async (entry) =>
        {
            // Set cache expiration to 1 hour for CLI updates
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await GetPackagesAsync(workingDirectory, "Aspire.Cli", prerelease, source, cancellationToken);
        }) ?? [];

        return packages;
    }

    internal async Task<IEnumerable<NuGetPackage>> GetPackagesAsync(DirectoryInfo workingDirectory, string query, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

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
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken
                );

            if (result.ExitCode != 0)
            {
                throw new NuGetPackageCacheException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.FailedToSearchForPackages, result.ExitCode));
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
                   packageName.Equals("Aspire.ProjectTemplates", StringComparison.Ordinal) ||
                   packageName.Equals("Aspire.Cli", StringComparison.Ordinal);

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
