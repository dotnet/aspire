// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.PackageManagement;

internal interface IPackageExecutableResolver
{
    Task<PackageExecutableResolutionResult> ResolveAsync(PackageExecutableResource resource, CancellationToken cancellationToken);
}