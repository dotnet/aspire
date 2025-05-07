#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that indicates a resource is using a specific container registry.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ContainerRegistryReferenceAnnotation"/> class.
/// </remarks>
/// <param name="registry">The container registry resource.</param>
public class ContainerRegistryReferenceAnnotation(IContainerRegistry registry) : IResourceAnnotation
{
    /// <summary>
    /// Gets the container registry resource.
    /// </summary>
    public IContainerRegistry Registry { get; } = registry;
}
