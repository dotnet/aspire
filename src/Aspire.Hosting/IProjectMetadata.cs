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
    /// Gets the fully-qualified path to the project.
    /// </summary>
    public string ProjectPath { get; }

    // These are for testing
    internal LaunchSettings? LaunchSettings => null;
    internal IConfiguration? Configuration => null;
}

[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}")]
internal sealed class ProjectMetadata(string projectPath) : IProjectMetadata
{
    private readonly string _originalProjectPath = projectPath;
    public string ProjectPath
    {
        get
        {
            string? root = Environment.GetEnvironmentVariable("ASPIRE_PROJECT_ROOT");
            if (string.IsNullOrEmpty(root))
            {
                return _originalProjectPath;
            }
            string projectPath = Path.Combine(root, Path.GetFileName(Path.GetDirectoryName(_originalProjectPath)!), Path.GetFileName(_originalProjectPath));
            System.Console.WriteLine($"Using root: {root}, and returning {projectPath}");
            return projectPath;
        }
    }
}
