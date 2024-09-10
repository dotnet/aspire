// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Lifetime modes for container resources
/// </summary>
public enum ContainerLifetimeType
{
    /// <summary>
    /// The default lifetime behavior should apply. This will create the resource when the AppHost starts and dispose of it when the AppHost shuts down.
    /// </summary>
    Default,
    /// <summary>
    /// The resource is persistent and will not be disposed of when the AppHost shuts down.
    /// </summary>
    Persistent,
}

/// <summary>
/// Annotation that controls the lifetime of a container resource (default behavior that matches the lifetime of the AppHost or a persistent lifetime across AppHost restarts)
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
public sealed class ContainerLifetimeAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the lifetime type for the container resource.
    /// </summary>
    public required ContainerLifetimeType LifetimeType { get; set; }
}