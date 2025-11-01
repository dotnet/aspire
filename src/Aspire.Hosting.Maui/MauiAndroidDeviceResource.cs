// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// A resource that represents an Android physical device for running a .NET MAUI application.
/// </summary>
/// <param name="name">The name of the Android device resource.</param>
/// <param name="parent">The parent MAUI project resource.</param>
public sealed class MauiAndroidDeviceResource(string name, MauiProjectResource parent)
    : ProjectResource(name), IMauiPlatformResource
{
    /// <summary>
    /// Gets the parent MAUI project resource.
    /// </summary>
    public MauiProjectResource Parent { get; } = parent;
}
