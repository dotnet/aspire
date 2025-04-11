// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Represents container registry information for Azure Container Apps.
/// </summary>
public interface IContainerRegistry
{
    /// <summary>
    /// Gets the name of the container registry.
    /// </summary>
    IManifestExpressionProvider Name { get; }

    /// <summary>
    /// Gets the endpoint URL of the container registry.
    /// </summary>
    IManifestExpressionProvider Endpoint { get; }

    /// <summary>
    /// Gets the managed identity ID associated with the container registry.
    /// </summary>
    IManifestExpressionProvider ManagedIdentityId { get; }
}