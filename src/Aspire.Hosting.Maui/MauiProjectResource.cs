// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Logical placeholder resource representing a multi-target .NET MAUI project. Not started directly.
/// Platform specific child <see cref="ProjectResource"/> instances are created for each selected target.
/// </summary>
public sealed class MauiProjectResource(string name, string projectPath) : Resource(name)
{
    /// <summary>
    /// Gets the path to the underlying multi-target .NET MAUI project (.csproj) file.
    /// </summary>
    public string ProjectPath { get; } = projectPath;
}
