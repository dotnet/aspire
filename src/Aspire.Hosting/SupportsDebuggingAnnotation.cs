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
/// <param name="launchConfigurationProducer">The launch configuration for the executable, if applicable.</param>
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}, Type = {Type}")]
[Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class SupportsDebuggingAnnotation(
    string resourceType,
    string projectPath,
    string debugAdapterId,
    string? requiredExtensionId,
    Func<ExecutableLaunchConfiguration>? launchConfigurationProducer) : IResourceAnnotation
{
    /// <summary>
    /// Gets the type of resource that can be debugged (e.g., "python", "project").
    /// </summary>
    public string ResourceType { get; } = resourceType ?? throw new ArgumentNullException(nameof(resourceType));

    /// <summary>
    /// Gets the project path.
    /// </summary>
    public string ProjectPath { get; } = projectPath ?? throw new ArgumentNullException(nameof(projectPath));

    /// <summary>
    /// Gets the debug adapter ID.
    /// </summary>
    public string DebugAdapterId { get; } = debugAdapterId ?? throw new ArgumentNullException(nameof(debugAdapterId));

    /// <summary>
    /// Gets the required extension ID.
    /// </summary>
    public string? RequiredExtensionId { get; } = requiredExtensionId;

    /// <summary>
    /// Gets the base launch configuration for the executable.
    /// </summary>
    public Func<ExecutableLaunchConfiguration>? LaunchConfigurationProducer { get; } = launchConfigurationProducer;
}
