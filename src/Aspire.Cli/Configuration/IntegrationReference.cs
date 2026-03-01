// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Configuration;

/// <summary>
/// Represents a reference to an Aspire hosting integration, which can be either
/// a NuGet package (with a version) or a local project reference (with a path to a .csproj).
/// </summary>
/// <param name="Name">The package or assembly name (e.g., "Aspire.Hosting.Redis").</param>
/// <param name="Version">The NuGet package version, or null for project references.</param>
/// <param name="ProjectPath">The absolute path to the .csproj file, or null for NuGet packages.</param>
internal sealed record IntegrationReference(string Name, string? Version, string? ProjectPath)
{
    /// <summary>
    /// Returns true if this is a project reference (has a .csproj path).
    /// </summary>
    public bool IsProjectReference => ProjectPath is not null;

    /// <summary>
    /// Returns true if this is a NuGet package reference (has a version).
    /// </summary>
    public bool IsPackageReference => Version is not null;
}
