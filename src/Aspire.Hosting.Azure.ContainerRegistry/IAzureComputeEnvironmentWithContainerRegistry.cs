// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure compute environment resource that uses a container registry.
/// </summary>
public interface IAzureComputeEnvironmentWithContainerRegistry : IAzureComputeEnvironmentResource
{
    /// <summary>
    /// Gets the Azure Container Registry resource used by this compute environment.
    /// </summary>
    /// <returns>The <see cref="AzureContainerRegistryResource"/> used by this environment.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no container registry is configured for this environment, or when 
    /// the configured container registry is not an Azure Container Registry.
    /// </exception>
    AzureContainerRegistryResource GetContainerRegistryResource();
}
