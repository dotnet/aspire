// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Options for building container images.
/// </summary>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ContainerImageOptions
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
    /// Gets the image tag to apply during build. Can be a single tag or multiple tags separated by semicolons.
    /// </summary>
    public string? ImageTag { get; init; }
}
