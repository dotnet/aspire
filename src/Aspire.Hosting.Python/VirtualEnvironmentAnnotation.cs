// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// Annotation to control Python virtual environment behavior.
/// </summary>
/// <param name="autoCreate">Whether to automatically create the virtual environment if it doesn't exist.</param>
public class VirtualEnvironmentAnnotation(bool autoCreate = true) : IResourceAnnotation
{
    /// <summary>
    /// Gets whether to automatically create the virtual environment if it doesn't exist.
    /// </summary>
    public bool AutoCreate { get; } = autoCreate;
}