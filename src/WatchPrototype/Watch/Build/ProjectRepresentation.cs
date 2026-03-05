// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.ProjectTools;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Project can be reprented by project file or by entry point file (for single-file apps).
/// </summary>
/// <param name="ProjectGraphPath">Path used in Project Graph (may be virtual).</param>
/// <param name="PhysicalPath">Path to an physical (non-virtual) project, if available.</param>
/// <param name="EntryPointFilePath">Path to an entry point file, if available.</param>
internal readonly record struct ProjectRepresentation(string ProjectGraphPath, string? PhysicalPath, string? EntryPointFilePath)
{
    public ProjectRepresentation(string? projectPath, string? entryPointFilePath)
        : this(projectPath ?? VirtualProjectBuilder.GetVirtualProjectPath(entryPointFilePath!), projectPath, entryPointFilePath)
    {
    }

    [MemberNotNullWhen(true, nameof(PhysicalPath))]
    [MemberNotNullWhen(false, nameof(EntryPointFilePath))]
    public bool IsProjectFile
        => PhysicalPath != null;

    public string ProjectOrEntryPointFilePath
        => IsProjectFile ? PhysicalPath : EntryPointFilePath;

    public string GetContainingDirectory()
        => Path.GetDirectoryName(ProjectOrEntryPointFilePath)!;

    public static ProjectRepresentation FromProjectOrEntryPointFilePath(string projectOrEntryPointFilePath)
        => string.Equals(Path.GetExtension(projectOrEntryPointFilePath), ".csproj", StringComparison.OrdinalIgnoreCase)
            ? new(projectPath: projectOrEntryPointFilePath, entryPointFilePath: null)
            : new(projectPath: null, entryPointFilePath: projectOrEntryPointFilePath);

    public ProjectRepresentation WithProjectGraphPath(string projectGraphPath)
        => new(projectGraphPath, PhysicalPath, EntryPointFilePath);

    public bool Equals(ProjectRepresentation other)
        => PathUtilities.OSSpecificPathComparer.Equals(ProjectGraphPath, other.ProjectGraphPath);

    public override int GetHashCode()
        => PathUtilities.OSSpecificPathComparer.GetHashCode(ProjectGraphPath);
}
