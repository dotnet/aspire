// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that stores container build metadata obtained from MSBuild properties.
/// This includes the ContainerRepository and ContainerImageTag that were resolved from the project file.
/// </summary>
/// <remarks>
/// This annotation is automatically populated when building a ProjectResource by querying the project's
/// MSBuild properties using the ComputeContainerConfig target. The values stored here take precedence
/// when determining the actual container image name for the resource.
/// </remarks>
public sealed class ContainerBuildMetadataAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the container repository name (ContainerRepository MSBuild property).
    /// </summary>
    /// <value>
    /// The repository name for the container image, typically in the format "repository-name" or "registry/repository-name".
    /// </value>
    public string? ContainerRepository { get; set; }

    /// <summary>
    /// Gets or sets the container image tag (ContainerImageTag MSBuild property).
    /// </summary>
    /// <value>
    /// The tag for the container image. If not set, defaults to "latest".
    /// </value>
    public string? ContainerImageTag { get; set; }
}
