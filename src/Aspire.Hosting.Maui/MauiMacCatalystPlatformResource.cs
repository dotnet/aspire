// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Represents a Mac Catalyst platform instance of a .NET MAUI project.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The parent MAUI project resource.</param>
/// <remarks>
/// This resource represents a MAUI application running on the Mac Catalyst platform.
/// The actual build and deployment happens when the resource is started, allowing for
/// incremental builds during development without blocking AppHost startup.
/// <para>
/// Use <see cref="MauiMacCatalystExtensions.AddMacCatalystDevice(IResourceBuilder{MauiProjectResource}, string?)"/>
/// to add this resource to a MAUI project.
/// </para>
/// </remarks>
public class MauiMacCatalystPlatformResource(string name, MauiProjectResource parent)
    : ProjectResource(name), IResourceWithParent<MauiProjectResource>, IMauiPlatformResource
{
    /// <summary>
    /// Gets the parent MAUI project resource.
    /// </summary>
    public MauiProjectResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));
}
