// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Publishing;
/// <summary>
/// Options for building and publishing a container image.  
/// </summary>
public class BuildImageOptions
{
    /// <summary>
    /// The registry to push to.
    /// </summary>
    public string? ContainerRegistry { get; set; }

    /// <summary>
    /// The tag to associate with the new image.
    /// </summary>
    public string? ContainerImageTag { get; set; }

    /// <summary>
    /// If true, the tooling will skip the publishing step.
    /// </summary>
    public bool SkipPublishing { get; set; }
}
