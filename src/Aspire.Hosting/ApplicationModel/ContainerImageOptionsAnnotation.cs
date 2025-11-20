// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for container image options that can be applied to resources.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class ContainerImageOptionsAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the output path for the container archive.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets the container image format.
    /// </summary>
    public ContainerImageFormat? ImageFormat { get; init; }

    /// <summary>
    /// Gets the target platform for the container.
    /// </summary>
    public ContainerTargetPlatform? TargetPlatform { get; init; }

    /// <summary>
    /// Gets the image name for the container.
    /// </summary>
    public string? ImageName { get; init; }

    /// <summary>
    /// Gets the image tag to apply during build. Can be a single tag or multiple tags separated by semicolons.
    /// </summary>
    public string? ImageTag { get; init; }
}
