// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;

namespace Aspire.Cli.Packaging;

internal interface IPackagingService
{
    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default);
}

internal class PackagingService(INuGetPackageCache nuGetPackageCache) : IPackagingService
{
    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        var channels = new List<PackageChannel>()
        {
            new PackageChannel(nuGetPackageCache)
        };

        return Task.FromResult<IEnumerable<PackageChannel>>(channels);
    }
}
