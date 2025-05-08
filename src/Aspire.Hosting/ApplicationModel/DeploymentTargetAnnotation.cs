// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

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
    [Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostic/{0}")]
    public IContainerRegistry? ContainerRegistry { get; set; }

    /// <summary>
    /// Gets or sets the compute environment resource associated with the deployment target.
    /// </summary>
    [Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostic/{0}")]
    public IComputeEnvironmentResource? ComputeEnvironment { get; set; }
}
