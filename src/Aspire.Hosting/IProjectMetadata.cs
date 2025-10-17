// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Represents metadata about a project resource.
/// </summary>
public interface IProjectMetadata : IResourceAnnotation
{
    /// <summary>
    /// Gets the fully-qualified path to the project or file-based app file.
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Gets the launch settings associated with the project.
    /// </summary>
    public LaunchSettings? LaunchSettings => null;

    // Internal for testing.
    internal IConfiguration? Configuration => null;

    /// <summary>
    /// Gets a value indicating whether building the project before running it should be suppressed.
    /// </summary>
    public bool SuppressBuild => false;

    internal bool IsFileBasedApp => string.Equals(Path.GetExtension(ProjectPath), ".cs", StringComparison.OrdinalIgnoreCase);
}

[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}")]
internal sealed class ProjectMetadata(string projectPath) : IProjectMetadata
{
    public string ProjectPath { get; } = ResolveProjectPath(projectPath);

    public bool SuppressBuild => false;

    private static string ResolveProjectPath(string path)
    {
        if (Directory.Exists(path))
        {
            // Path is a directory, assume it's a project directory
            var projectFiles = Directory.GetFiles(path, "*.csproj", new EnumerationOptions
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = false,
                IgnoreInaccessible = true
            });

            if (projectFiles.Length == 0)
            {
                // No project files found, just let it pass through and be handled later
                return path;
            }
            else if (projectFiles.Length > 1)
            {
                throw new InvalidOperationException($"The specified project directory '{path}' contains multiple project files. Please specify the path to a specific project file.");
            }
            else
            {
                return Path.GetFullPath(projectFiles[0]);
            }
        }

        return path;
    }
}
