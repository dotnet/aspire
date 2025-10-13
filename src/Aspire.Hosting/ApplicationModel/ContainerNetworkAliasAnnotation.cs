// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a custom network alias for a container resource.
/// </summary>
/// <remarks>
/// Network aliases enable DNS resolution of the container on the network by custom names.
/// Multiple aliases can be specified for a single container by adding multiple annotations.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Alias = {Alias}")]
public sealed class ContainerNetworkAliasAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the network alias for the container.
    /// </summary>
    public required string Alias { get; set; }
}
