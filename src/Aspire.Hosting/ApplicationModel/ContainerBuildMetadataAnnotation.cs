// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that stores container build metadata obtained from MSBuild properties.
/// This includes the ContainerRepository and ContainerImageTag that were resolved from the project file.
/// </summary>
public sealed class ContainerBuildMetadataAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the container repository name (ContainerRepository MSBuild property).
    /// </summary>
    public string? ContainerRepository { get; set; }

    /// <summary>
    /// Gets or sets the container image tag (ContainerImageTag MSBuild property).
    /// </summary>
    public string? ContainerImageTag { get; set; }
}
