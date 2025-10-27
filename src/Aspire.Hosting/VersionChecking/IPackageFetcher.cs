// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;

namespace Aspire.Hosting.VersionChecking;

internal interface IPackageFetcher
{
    Task<List<NuGetPackage>> TryFetchPackagesAsync(string appHostDirectory, CancellationToken cancellationToken);
}
