// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Semver;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Packaging;

internal class PackageChannel(string name, PackageChannelQuality quality, PackageMapping[]? mappings, INuGetPackageCache nuGetPackageCache)
{
    public string Name { get; } = name;
    public PackageChannelQuality Quality { get; } = quality;
    public PackageMapping[]? Mappings { get; } = mappings;
    public PackageChannelType Type { get; } = mappings is null ? PackageChannelType.Implicit : PackageChannelType.Explicit;

    public async Task<IEnumerable<NuGetPackage>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var tasks = new List<Task<IEnumerable<NuGetPackage>>>();

        using var tempNuGetConfig = Type is PackageChannelType.Explicit ? await TemporaryNuGetConfig.CreateAsync(Mappings!) : null;

        if (Quality is PackageChannelQuality.Stable || Quality is PackageChannelQuality.Both)
        {
            tasks.Add(nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, false, tempNuGetConfig?.ConfigFile, cancellationToken));
        }

        if (Quality is PackageChannelQuality.Prerelease || Quality is PackageChannelQuality.Both)
        {
            tasks.Add(nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, true, tempNuGetConfig?.ConfigFile, cancellationToken));
        }

        var packageResults = await Task.WhenAll(tasks);

        var packages = packageResults
            .SelectMany(p => p)
            .DistinctBy(p => $"{p.Id}-{p.Version}");

        // When doing a `dotnet package search` the the results may include stable packages even when searching for
        // prerelease packages. This filters out this noise.
        var filteredPackages = packages.Where(p => new { SemVer = SemVersion.Parse(p.Version), Quality = Quality } switch
        {
            { Quality: PackageChannelQuality.Both } => true,
            { Quality: PackageChannelQuality.Stable, SemVer: { IsPrerelease: false } } => true,
            { Quality: PackageChannelQuality.Prerelease, SemVer: { IsPrerelease: true } } => true,
            _ => false
        });

        return filteredPackages;
    }

    public static PackageChannel CreateExplicitChannel(string name, PackageChannelQuality quality, PackageMapping[]? mappings, INuGetPackageCache nuGetPackageCache)
    {
        return new PackageChannel(name, quality, mappings, nuGetPackageCache);
    }

    public static PackageChannel CreateImplicitChannel(INuGetPackageCache nuGetPackageCache)
    {
        // The reason that PackageChannelQuality.Both is because there are situations like
        // in community toolkit where there is a newer beta version available for a package
        // in the case of implicit feeds we want to be able to show that, along side the stable
        // version. Not really an issue for template selection though (unless we start allowing)
        // for broader templating options.
        return new PackageChannel("default", PackageChannelQuality.Both, null, nuGetPackageCache);
    }
}