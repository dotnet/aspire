// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Represents a .NET MAUI project resource within the Aspire hosting environment.
/// </summary>
/// <remarks>
/// This class is used to define and manage the configuration of a .NET MAUI project,
/// including platform-specific build and launch behaviors. MAUI projects are automatically
/// excluded from deployment manifests since they represent client applications that cannot
/// be deployed as cloud services.
/// </remarks>
public class MauiProjectResource(string name) : ProjectResource(name)
{
    /// <summary>
    /// Gets or sets the target platform for the MAUI application.
    /// </summary>
    /// <remarks>
    /// This property can be used to specify which platform the MAUI app should be built/launched for
    /// during development (e.g., "android", "ios", "maccatalyst", "windows").
    /// If not specified, the default behavior will depend on the development environment.
    /// </remarks>
    public string? TargetPlatform { get; set; }

    /// <summary>
    /// Gets or sets whether the MAUI application should auto-start during development.
    /// </summary>
    /// <remarks>
    /// MAUI applications typically don't auto-start by default since they represent client applications
    /// that developers usually want to launch manually or through the IDE. Set this to true to
    /// automatically start the MAUI app when the AppHost starts.
    /// </remarks>
    public bool AutoStart { get; set; }
}