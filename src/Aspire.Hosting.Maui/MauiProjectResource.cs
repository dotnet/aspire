// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Logical placeholder resource representing a multi-target .NET MAUI project.
/// This resource will be invisible in the Aspire dashboard and does not represent
/// a deployable unit itself. Instead, platform specific resources will be created
/// for each enabled platform.
/// </summary>
public sealed class MauiProjectResource(string name, string projectPath) : Resource(name)
{
    /// <summary>
    /// Gets the path to the underlying multi-target .NET MAUI project (.csproj) file.
    /// </summary>
    public string ProjectPath { get; } = projectPath;
}
