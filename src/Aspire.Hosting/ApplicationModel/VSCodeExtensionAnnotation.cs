// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a Visual Studio Code extension recommendation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Id = {Id}")]
internal sealed class VSCodeExtensionAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VSCodeExtensionAnnotation"/> class.
    /// </summary>
    /// <param name="id">The extension ID (e.g., "ms-python.python").</param>
    public VSCodeExtensionAnnotation(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        Id = id;
    }

    /// <summary>
    /// Gets the extension ID (e.g., "ms-python.python").
    /// </summary>
    public string Id { get; }
}