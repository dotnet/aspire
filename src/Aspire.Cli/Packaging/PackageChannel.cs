// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Packaging;

internal class PackageChannel(INuGetPackageCache nuGetPackageCache)
{
    public Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        return nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, prerelease, source, cancellationToken);
    }
}