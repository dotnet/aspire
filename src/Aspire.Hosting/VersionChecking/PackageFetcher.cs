// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Dcp.Process;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.VersionChecking;

internal sealed class PackageFetcher : IPackageFetcher
{
    public const string PackageId = "Aspire.Hosting.AppHost";

    // Limit the number of packages fetched per search to avoid overwhelming the output. This should never happen unless there is a bug in the API.
    // Package search returns the latest version per source and few packages will match "Aspire.Hosting.AppHost" search string.
    private const int SearchPageSize = 1000;

    private readonly ILogger<PackageFetcher> _logger;

    public PackageFetcher(ILogger<PackageFetcher> logger)
    {
        _logger = logger;
    }

    public async Task<List<NuGetPackage>> TryFetchPackagesAsync(string appHostDirectory, CancellationToken cancellationToken)
    {
        var outputJson = new StringBuilder();
        var spec = new ProcessSpec("dotnet")
        {
            Arguments = $"package search {PackageId} --format json --prerelease --take {SearchPageSize}",
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
                return [];
            }
        }

        // Filter packages to only consider "Aspire.Hosting.AppHost".
        // Although the CLI command 'dotnet package search Aspire.Hosting.AppHost --format json' 
        // should already limit results according to NuGet search syntax 
        // (https://learn.microsoft.com/en-us/nuget/consume-packages/finding-and-choosing-packages#search-syntax),
        // we add this extra check for robustness in case the CLI output includes unexpected packages.
        return PackageUpdateHelpers.ParsePackageSearchResults(outputJson.ToString(), PackageId);
    }
}
