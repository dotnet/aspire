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

    /// <summary>
    /// Gets the launch settings associated with the project.
    /// </summary>
    public LaunchSettings? LaunchSettings => null;

    // Internal for testing.
    internal IConfiguration? Configuration => null;
}

[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}")]
internal sealed class ProjectMetadata(string projectPath) : IProjectMetadata
{
    public string ProjectPath { get; } = projectPath;
}
