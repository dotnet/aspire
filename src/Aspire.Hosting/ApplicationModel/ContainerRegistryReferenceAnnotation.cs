// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that indicates a resource is using a specific container registry.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ContainerRegistryReferenceAnnotation"/> class.
/// </remarks>
/// <param name="registry">The container registry resource.</param>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ContainerRegistryReferenceAnnotation(IContainerRegistry registry) : IResourceAnnotation
{
    /// <summary>
    /// Gets the container registry resource.
    /// </summary>
    public IContainerRegistry Registry { get; } = registry;
}
