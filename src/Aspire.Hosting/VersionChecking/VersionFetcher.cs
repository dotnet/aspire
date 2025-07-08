// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Hosting.VersionChecking;

internal sealed class VersionFetcher : IVersionFetcher
{
    private const string PackageId = "Aspire.Hosting.AppHost";

    private readonly ILogger<VersionFetcher> _logger;

    public VersionFetcher(ILogger<VersionFetcher> logger)
    {
        _logger = logger;
    }

    public async Task<SemVersion?> TryFetchLatestVersionAsync(string appHostDirectory, CancellationToken cancellationToken)
    {
        var outputJson = new StringBuilder();
        var spec = new ProcessSpec("dotnet")
        {
            Arguments = $"package search {PackageId} --format json",
            ThrowOnNonZeroReturnCode = false,
            InheritEnv = true,
            OnOutputData = output =>
            {
                outputJson.Append(output);
                _logger.LogDebug("dotnet (stdout): {Output}", output);
            },
            OnErrorData = error =>
            {
                _logger.LogDebug("dotnet (stderr): {Error}", error);
            },
            WorkingDirectory = appHostDirectory
        };

        _logger.LogDebug("Running dotnet CLI to check for latest version of {PackageId} with arguments: {ArgumentList}", PackageId, spec.Arguments);
        var (pendingProcessResult, processDisposable) = ProcessUtil.Run(spec);

        await using (processDisposable)
        {
            var processResult = await pendingProcessResult
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (processResult.ExitCode != 0)
            {
                _logger.LogDebug("The dotnet CLI call to check for latest version failed with exit code {ExitCode}.", processResult.ExitCode);
                return null;
            }
        }

        return GetLatestVersion(outputJson.ToString());
    }

    internal static SemVersion? GetLatestVersion(string outputJson)
    {
        var packages = ParseOutput(outputJson);
        var versions = new List<SemVersion>();
        foreach (var package in packages)
        {
            // Filter packages to only consider "Aspire.Hosting.AppHost".
            // Although the CLI command 'dotnet package search Aspire.Hosting.AppHost --format json' 
            // should already limit results according to NuGet search syntax 
            // (https://learn.microsoft.com/en-us/nuget/consume-packages/finding-and-choosing-packages#search-syntax),
            // we add this extra check for robustness in case the CLI output includes unexpected packages.
            if (package.Id == PackageId &&
                SemVersion.TryParse(package.LatestVersion, out var version) &&
                !version.IsPrerelease)
            {
                versions.Add(version);
            }
        }

        return versions.OrderDescending(SemVersion.PrecedenceComparer).FirstOrDefault();
    }

    private static List<Package> ParseOutput(string outputJson)
    {
        var packages = new List<Package>();

        using var document = JsonDocument.Parse(outputJson);
        var root = document.RootElement;

        if (root.TryGetProperty("searchResult", out var searchResults))
        {
            foreach (var result in searchResults.EnumerateArray())
            {
                if (result.TryGetProperty("packages", out var packagesArray))
                {
                    foreach (var pkg in packagesArray.EnumerateArray())
                    {
                        var id = pkg.GetProperty("id").GetString();
                        var latestVersion = pkg.GetProperty("latestVersion").GetString();
                        if (id != null && latestVersion != null)
                        {
                            packages.Add(new Package(id, latestVersion));
                        }
                    }
                }
            }
        }

        return packages;
    }
}
