// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Semver;

namespace Aspire.Hosting.VersionChecking;

internal interface IVersionFetcher
{
    Task<SemVersion?> TryFetchLatestVersionAsync(CancellationToken cancellationToken);
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
