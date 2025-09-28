// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies that the resource can be debugged by the Aspire Extension.
///
/// <param name="projectPath">The entrypoint of the resource.</param>
/// <param name="debugAdapterId">The debug adapter ID to use for debugging.</param>
/// <param name="requiredExtensionId">The ID of the required extension that provides the debug adapter.</param>
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, ProjectPath = {ProjectPath}, Type = {Type}")]
[Experimental("ASPIREEXTENSION001")]
public sealed class SupportsDebuggingAnnotation(string projectPath, string debugAdapterId, string? requiredExtensionId) : IResourceAnnotation
{
    private readonly string _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
    private readonly string _debugAdapterId = debugAdapterId ?? throw new ArgumentNullException(nameof(debugAdapterId));
    private readonly string _requiredExtensionId = requiredExtensionId ?? string.Empty;

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
}
