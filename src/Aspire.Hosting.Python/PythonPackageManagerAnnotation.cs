// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// Represents the annotation for the Python package manager used in a resource.
/// </summary>
/// <param name="executableName">The name of the executable used to run the package manager.</param>
internal sealed class PythonPackageManagerAnnotation(string executableName) : IResourceAnnotation
{
    /// <summary>
    /// Gets the executable used to run the Python package manager.
    /// </summary>
    public string ExecutableName { get; } = executableName;
}
