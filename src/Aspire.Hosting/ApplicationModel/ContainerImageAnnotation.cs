// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a container image.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Image = {Image}, Tag = {Tag}")]
public sealed class ContainerImageAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the registry for the container image.
    /// </summary>
    public string? Registry { get; set; }

    /// <summary>
    /// Gets or sets the image for the container.
    /// </summary>
    public required string Image { get; set; }

    /// <summary>
    /// Gets or sets the tag for the container image.
    /// </summary>
    public required string Tag { get; set; }
}
