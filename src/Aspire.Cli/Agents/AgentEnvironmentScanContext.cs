// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Agents;

/// <summary>
/// Supplies the workspace boundary for a read-only environment scan.
/// </summary>
internal sealed record AgentEnvironmentScanContext
{
    public required DirectoryInfo WorkingDirectory { get; init; }

    // Workspace-free Global setup must not inspect project markers.
    public required DirectoryInfo? RepositoryRoot { get; init; }

    public DirectoryInfo? FindProjectDirectory(string directoryName, DirectoryInfo homeDirectory)
        => AgentPath.ProjectDirectories(WorkingDirectory, RepositoryRoot).FirstOrDefault(directory =>
            Path.GetRelativePath(homeDirectory.FullName, directory.FullName) != "." &&
            Directory.Exists(Path.Combine(directory.FullName, directoryName)));
}
