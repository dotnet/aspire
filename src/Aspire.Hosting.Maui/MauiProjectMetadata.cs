// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui;

/// <summary>
/// Project metadata for MAUI projects.
/// </summary>
/// <param name="projectPath">The path to the MAUI project file.</param>
internal sealed class MauiProjectMetadata(string projectPath) : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath { get; } = projectPath;
}
