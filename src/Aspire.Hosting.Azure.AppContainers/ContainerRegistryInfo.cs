// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Provides container registry information for Azure Container Apps.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ContainerRegistryInfo"/> class.
/// </remarks>
/// <param name="name">The name of the container registry.</param>
/// <param name="endpoint">The endpoint URL of the container registry.</param>
/// <param name="managedIdentityId">The managed identity ID associated with the container registry.</param>
internal sealed class ContainerRegistryInfo(
    IManifestExpressionProvider name,
    IManifestExpressionProvider endpoint,
    IManifestExpressionProvider managedIdentityId) : IContainerRegistry
{
    /// <inheritdoc/>
    public IManifestExpressionProvider Name { get; } = name;

    /// <inheritdoc/>
    public IManifestExpressionProvider Endpoint { get; } = endpoint;

    /// <inheritdoc/>
    public IManifestExpressionProvider ManagedIdentityId { get; } = managedIdentityId;
}
