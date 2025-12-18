// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that enables stdin support for a container resource.
/// </summary>
/// <remarks>
/// When this annotation is present on a container resource, the container will be started
/// with the -i flag, which keeps stdin open and allows the container process to read from stdin.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Enabled = {Enabled}")]
public sealed class ContainerStdinAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets whether stdin support is enabled for the container resource.
    /// </summary>
    public required bool Enabled { get; set; }
}
