// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a custom container name.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class ContainerNameAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the name for the container.
    /// </summary>
    public required string Name { get; set; }
}
