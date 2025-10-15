// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// Stores the entrypoint configuration for a Python application resource.
/// </summary>
internal sealed class PythonEntrypointAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the type of entrypoint — Executable, Script, or Module.
    /// </summary>
    public required EntrypointType Type { get; set; }

    /// <summary>
    /// Gets or sets the entrypoint string (script path, module name, or executable name).
    /// </summary>
    public required string Entrypoint { get; set; }
}
