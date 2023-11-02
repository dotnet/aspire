// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a volume mount annotation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Source = {Source}, Target = {Target}")]
public sealed class VolumeMountAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Instantiates a volume mount annotation that specifies the source and target paths for a volume mount.
    /// </summary>
    /// <param name="source">The source path of the volume mount.</param>
    /// <param name="target">The target path of the volume mount.</param>
    /// <param name="type">The type of the volume mount.</param>
    /// <param name="isReadOnly">A value indicating whether the volume mount is read-only.</param>
    public VolumeMountAnnotation(string source, string target, VolumeMountType type = default, bool isReadOnly = false)
    {
        Source = source;
        Target = target;
        Type = type;
        IsReadOnly = isReadOnly;
    }

    /// <summary>
    /// Gets or sets the source of the volume mount.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the target of the volume mount.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the type of the volume mount.
    /// </summary>
    public VolumeMountType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the volume mount is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }
}

public enum VolumeMountType
{
    Bind,
    Named
}
