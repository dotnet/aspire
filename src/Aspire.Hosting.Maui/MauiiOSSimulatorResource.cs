// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// A resource that represents an iOS simulator for running a .NET MAUI application.
/// </summary>
/// <param name="name">The name of the iOS simulator resource.</param>
/// <param name="parent">The parent MAUI project resource.</param>
public sealed class MauiiOSSimulatorResource(string name, MauiProjectResource parent)
    : ProjectResource(name), IMauiPlatformResource
{
    /// <summary>
    /// Gets the parent MAUI project resource.
    /// </summary>
    public MauiProjectResource Parent { get; } = parent;
}
