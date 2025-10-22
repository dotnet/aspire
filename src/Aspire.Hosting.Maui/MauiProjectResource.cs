// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Represents a .NET MAUI project resource in the distributed application model.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="projectPath">The path to the .NET MAUI project file.</param>
/// <remarks>
/// This resource serves as a parent for platform-specific MAUI resources (Windows, Android, iOS, macOS).
/// Use extension methods like <c>AddWindowsDevice</c> to add platform-specific instances.
/// <para>
/// MAUI projects are built on-demand when the platform-specific resource is started, avoiding long
/// AppHost startup times while still allowing incremental builds during development.
/// </para>
/// </remarks>
public class MauiProjectResource(string name, string projectPath) : Resource(name)
{
    /// <summary>
    /// Gets the path to the .NET MAUI project file.
    /// </summary>
    public string ProjectPath { get; } = projectPath ?? throw new ArgumentNullException(nameof(projectPath));

    /// <summary>
    /// Gets the collection of Windows platform resources associated with this MAUI project.
    /// </summary>
    internal List<MauiWindowsPlatformResource> WindowsDevices { get; } = [];
}
