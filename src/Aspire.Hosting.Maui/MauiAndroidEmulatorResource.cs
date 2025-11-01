// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// A resource that represents an Android emulator for running a .NET MAUI application.
/// </summary>
/// <param name="name">The name of the Android emulator resource.</param>
/// <param name="parent">The parent MAUI project resource.</param>
public sealed class MauiAndroidEmulatorResource(string name, MauiProjectResource parent)
    : ProjectResource(name), IMauiPlatformResource
{
    /// <summary>
    /// Gets the parent MAUI project resource.
    /// </summary>
    public MauiProjectResource Parent { get; } = parent;
}
