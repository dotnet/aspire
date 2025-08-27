// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a Visual Studio Code extension recommendation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Id = {Id}, DisplayName = {DisplayName}")]
public sealed class VisualStudioCodeExtensionAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VisualStudioCodeExtensionAnnotation"/> class.
    /// </summary>
    /// <param name="id">The extension ID (e.g., "ms-python.python").</param>
    /// <param name="displayName">The display name of the extension.</param>
    /// <param name="description">The description of the extension (optional).</param>
    public VisualStudioCodeExtensionAnnotation(string id, string displayName, string? description = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(displayName);

        Id = id;
        DisplayName = displayName;
        Description = description;
    }

    /// <summary>
    /// Gets the extension ID (e.g., "ms-python.python").
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the extension.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the description of the extension.
    /// </summary>
    public string? Description { get; }
}