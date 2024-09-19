// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Lifetime modes for container resources
/// </summary>
public enum ContainerLifetime
{
    /// <summary>
    /// The default lifetime behavior should apply. This will create the resource when the AppHost starts and dispose of it when the AppHost shuts down.
    /// </summary>
    Default,
    /// <summary>
    /// Attempt to re-use a previously created resource (based on the container name) if one exists. Do not destroy the container on AppHost shutdown.
    /// </summary>
    /// <remarks>
    /// In the event that a container with the given name does not exist, a new container will always be created based on the
    /// current <see cref="ContainerResource"/> configuration.
    /// <para>When an existing container IS found, Aspire MAY re-use it based on the following criteria:</para>
    /// <list type="bullet">
    /// <item>If the container WAS NOT originally created by Aspire, the existing container will be re-used.</item>
    /// <item>If the container WAS originally created by Aspire:
    /// <list type="bullet">
    /// <item>And the <see cref="ContainerResource"/> configuration DOES match the existing container, the existing container will be re-used.</item>
    /// <item>And the <see cref="ContainerResource"/> configuration DOES NOT match the existing container, the existing container will be stopped
    /// and a new container created in order to apply the updated configuration.</item>
    /// </list></item>
    /// </list>
    /// </remarks>
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
    public required ContainerLifetime Lifetime { get; set; }
}