// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.ProjectTools;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Project can be reprented by project file or by entry point file (for single-file apps).
/// </summary>
internal readonly struct ProjectRepresentation(string projectGraphPath, string? projectPath, string? entryPointFilePath)
{
    /// <summary>
    /// Path used in Project Graph (may be virtual).
    /// </summary>
    public readonly string ProjectGraphPath = projectGraphPath;

    /// <summary>
    /// Path to an physical (non-virtual) project, if available.
    /// </summary>
    public readonly string? PhysicalPath = projectPath;

    /// <summary>
    /// Path to an entry point file, if available.
    /// </summary>
    public readonly string? EntryPointFilePath = entryPointFilePath;

    public ProjectRepresentation(string? projectPath, string? entryPointFilePath)
        : this(projectPath ?? VirtualProjectBuilder.GetVirtualProjectPath(entryPointFilePath!), projectPath, entryPointFilePath)
    {
    }

    public string ProjectOrEntryPointFilePath
        => PhysicalPath ?? EntryPointFilePath!;

    public string GetContainingDirectory()
        => Path.GetDirectoryName(ProjectOrEntryPointFilePath)!;

    public static ProjectRepresentation FromProjectOrEntryPointFilePath(string projectOrEntryPointFilePath)
        => string.Equals(Path.GetExtension(projectOrEntryPointFilePath), ".csproj", StringComparison.OrdinalIgnoreCase)
            ? new(projectPath: null, entryPointFilePath: projectOrEntryPointFilePath)
            : new(projectPath: projectOrEntryPointFilePath, entryPointFilePath: null);

    public ProjectRepresentation WithProjectGraphPath(string projectGraphPath)
        => new(projectGraphPath, PhysicalPath, EntryPointFilePath);
}
