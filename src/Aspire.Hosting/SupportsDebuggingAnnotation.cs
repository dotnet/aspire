// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
///
/// <param name="resourceType">The type of resource that can be debugged (e.g., "python", "project").</param>
/// <param name="projectPath">The entrypoint of the resource.</param>
/// <param name="debugAdapterId">The debug adapter ID to use for debugging.</param>
/// <param name="requiredExtensionId">The ID of the required extension that provides the debug adapter.</param>
/// <param name="launchConfiguration">The launch configuration for the executable, if applicable.</param>
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}, Type = {Type}")]
[Experimental("ASPIREEXTENSION001")]
public sealed class SupportsDebuggingAnnotation(
    string resourceType,
    string projectPath,
    string debugAdapterId,
    string? requiredExtensionId,
    ExecutableLaunchConfiguration? launchConfiguration) : IResourceAnnotation
{
    private readonly string _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
    private readonly string _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
    private readonly string _debugAdapterId = debugAdapterId ?? throw new ArgumentNullException(nameof(debugAdapterId));
    private readonly string _requiredExtensionId = requiredExtensionId ?? string.Empty;

    /// <summary>
    /// Gets the type of resource that can be debugged (e.g., "python", "project").
    /// </summary>
    public string ResourceType => _resourceType;

    /// <summary>
    /// Gets the project path.
    /// </summary>
    public string ProjectPath => _projectPath;

    /// <summary>
    /// Gets the debug adapter ID.
    /// </summary>
    public string DebugAdapterId => _debugAdapterId;

    /// <summary>
    /// Gets the required extension ID.
    /// </summary>
    public string? RequiredExtensionId => _requiredExtensionId;

    /// <summary>
    /// Gets the launch configuration for the executable.
    /// </summary>
    public ExecutableLaunchConfiguration? LaunchConfiguration { get; } = launchConfiguration;
}
