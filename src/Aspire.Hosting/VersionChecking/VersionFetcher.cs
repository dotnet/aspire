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
    private readonly ILogger<VersionFetcher> _logger;

    public VersionFetcher(ILogger<VersionFetcher> logger)
    {
        _logger = logger;
    }

    public async Task<SemVersion?> TryFetchLatestVersionAsync(CancellationToken cancellationToken)
    {
        var outputJson = new StringBuilder();
        var spec = new ProcessSpec("dotnet")
        {
            Arguments = "package search Aspire.Hosting.AppHost --format json",
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
        };

        _logger.LogDebug("Running dotnet CLI to check for latest version with arguments: {ArgumentList}", spec.Arguments);
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

        var packages = ParseOutput(outputJson.ToString());
        var versions = new List<SemVersion>();
        foreach (var package in packages)
        {
            if (SemVersion.TryParse(package.LatestVersion, out var version) && !version.IsPrerelease)
            {
                versions.Add(version);
            }
        }

        return versions.Order(SemVersion.PrecedenceComparer).FirstOrDefault();
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
