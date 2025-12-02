// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding container registry resources to the distributed application.
/// </summary>
public static class ContainerRegistryResourceBuilderExtensions
{
    /// <summary>
    /// Adds a container registry resource to the application model with parameterized values.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the container registry resource.</param>
    /// <param name="endpoint">An <see cref="IResourceBuilder{ParameterResource}"/> containing the registry endpoint URL or hostname.</param>
    /// <param name="repository">An optional <see cref="IResourceBuilder{ParameterResource}"/> containing the repository path within the registry.</param>
    /// <returns>An <see cref="IResourceBuilder{ContainerRegistryResource}"/> for the container registry resource.</returns>
    /// <remarks>
    /// Use this method when the registry endpoint and repository values need to be provided dynamically
    /// via configuration or user input.
    /// </remarks>
    /// <example>
    /// Add a container registry with parameterized values:
    /// <code>
    /// var endpointParameter = builder.AddParameter("registry-endpoint");
    /// var repositoryParameter = builder.AddParameter("registry-repo");
    /// var registry = builder.AddContainerRegistry("my-registry", endpointParameter, repositoryParameter);
    /// </code>
    /// </example>
    public static IResourceBuilder<ContainerRegistryResource> AddContainerRegistry(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource> endpoint,
        IResourceBuilder<ParameterResource>? repository = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(endpoint);

        var endpointExpression = ReferenceExpression.Create($"{endpoint.Resource}");
        var repositoryExpression = repository is not null
            ? ReferenceExpression.Create($"{repository.Resource}")
            : null;

        var resource = new ContainerRegistryResource(name, endpointExpression, repositoryExpression);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Adds a container registry resource to the application model with literal string values.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the container registry resource.</param>
    /// <param name="endpoint">The registry endpoint URL or hostname (e.g., "docker.io", "ghcr.io").</param>
    /// <param name="repository">The optional repository path within the registry (e.g., "myusername" for Docker Hub, "owner/repo" for GHCR).</param>
    /// <returns>An <see cref="IResourceBuilder{ContainerRegistryResource}"/> for the container registry resource.</returns>
    /// <remarks>
    /// Use this method when the registry endpoint and repository values are known at design time
    /// and do not need to be parameterized.
    /// </remarks>
    /// <example>
    /// Add a Docker Hub container registry:
    /// <code>
    /// var registry = builder.AddContainerRegistry("docker-hub", "docker.io", "myusername");
    /// </code>
    /// </example>
    /// <example>
    /// Add a GitHub Container Registry:
    /// <code>
    /// var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "owner/repo");
    /// </code>
    /// </example>
    public static IResourceBuilder<ContainerRegistryResource> AddContainerRegistry(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string endpoint,
        string? repository = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        var endpointExpression = ReferenceExpression.Create($"{endpoint}");
        var repositoryExpression = repository is not null
            ? ReferenceExpression.Create($"{repository}")
            : null;

        var resource = new ContainerRegistryResource(name, endpointExpression, repositoryExpression);
        return builder.AddResource(resource);
    }
}
