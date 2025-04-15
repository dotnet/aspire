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
    /// the deployment target, if the deployment target is an image-based environment.
    /// </summary>
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public IContainerRegistry? ContainerRegistryInfo { get; set; }
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
