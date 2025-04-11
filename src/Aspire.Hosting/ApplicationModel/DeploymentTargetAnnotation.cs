// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a deployment target.
/// </summary>
public sealed class DeploymentTargetAnnotation(IResource target) : IResourceAnnotation
{
    /// <summary>
    /// The deployment target.
    /// </summary>
    public IResource DeploymentTarget { get; } = target;

    /// <summary>
    /// Gets or sets the container registry information associated with
    /// the deployment target, if the deployment target an image-based environment.
    /// </summary>
    /// <remarks>
    /// This property is typed as <see cref="object"/> since the
    /// IContainerRegistry interface is defined in the layer above
    /// in the Aspire.Hosting.Azure.AppContainers assemblies.
    /// </remarks>
    public object? ContainerRegistryInfo { get; set; }
}
