// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Packaging;

internal class PackageChannel(string name, PackageMapping[]? mappings, INuGetPackageCache nuGetPackageCache)
{
    public string Name { get; } = name;
    public PackageMapping[]? Mappings { get; } = mappings;
    public PackageChannelType Type { get; } = mappings is null ? PackageChannelType.Implicit : PackageChannelType.Explicit;

    public async Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        using var tempNuGetConfig = await TemporaryNuGetConfig.CreateAsync(Mappings ?? []);
        return await nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, prerelease, source, cancellationToken);
    }

    public static PackageChannel CreateExplicitChannel(string name, PackageMapping[]? mappings, INuGetPackageCache nuGetPackageCache)
    {
        return new PackageChannel(name, mappings, nuGetPackageCache);
    }

    public static PackageChannel CreateImplicitChannel(INuGetPackageCache nuGetPackageCache)
    {
        return new PackageChannel("default", null, nuGetPackageCache);
    }
}