// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Specifies the target platform for a MAUI application.
/// </summary>
public enum MauiTargetPlatform
{
    /// <summary>
    /// Target the Windows platform.
    /// </summary>
    Windows,

    /// <summary>
    /// Target the Android platform.
    /// </summary>
    Android,

    /// <summary>
    /// Target the iOS platform.
    /// </summary>
    iOS,

    /// <summary>
    /// Target the macOS platform (Mac Catalyst).
    /// </summary>
    MacCatalyst
}

/// <summary>
/// An annotation that specifies the target platform for a MAUI application resource.
/// </summary>
/// <param name="platform">The target platform for the MAUI application.</param>
public class MauiPlatformAnnotation(MauiTargetPlatform platform) : IResourceAnnotation
{
    /// <summary>
    /// Gets the target platform for the MAUI application.
    /// </summary>
    public MauiTargetPlatform Platform { get; } = platform;
}