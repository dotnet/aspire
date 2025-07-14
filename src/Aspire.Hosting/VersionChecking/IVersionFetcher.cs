// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;

namespace Aspire.Hosting.VersionChecking;

internal interface IVersionFetcher
{
    Task<List<NuGetPackage>> TryFetchVersionsAsync(string appHostDirectory, CancellationToken cancellationToken);
}
