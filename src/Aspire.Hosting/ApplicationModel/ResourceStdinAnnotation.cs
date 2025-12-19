// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that enables stdin support for a resource.
/// </summary>
/// <remarks>
/// When this annotation is present on an executable or project resource, DCP will keep stdin open
/// and accept stdin writes via the log subresource (source=stdin).
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Enabled = {Enabled}")]
public sealed class ResourceStdinAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets whether stdin support is enabled for the resource.
    /// </summary>
    public required bool Enabled { get; set; }
}
