// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Wraps an <see cref="DurableTaskSchedulerResource" /> in a type that exposes container extension methods.
/// </summary>
/// <param name="innerResource">The inner resource used to store annotations.</param>
public sealed class DurableTaskSchedulerEmulatorResource(DurableTaskSchedulerResource innerResource)
    : ContainerResource(innerResource.Name), IResource
{
    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => innerResource.Annotations;

    /// <inheritdoc />
    public override string Name => innerResource.Name;

    /// <summary>
    /// Gets or sets whether the emulator should use dynamic task hubs.
    /// </summary>
    /// <remarks>
    /// Using dynamic task hubs eliminates the requirement that they be pre-defined,
    /// which can be useful when the same emulator instance is used across sessions.
    /// </remarks>
    public bool UseDynamicTaskHubs { get; set; }
}
