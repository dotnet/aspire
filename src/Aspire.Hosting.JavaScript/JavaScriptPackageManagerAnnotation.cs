// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Represents the annotation for the JavaScript package manager used in a resource.
/// </summary>
/// <param name="executableName">The name of the executable used to run the package manager.</param>
/// <param name="runScriptCommand">The command used to run a script with the JavaScript package manager.</param>
/// <param name="cacheMount">The BuildKit cache mount path for the package manager, or null if not supported.</param>
public sealed class JavaScriptPackageManagerAnnotation(string executableName, string? runScriptCommand, string? cacheMount = null) : IResourceAnnotation
{
    /// <summary>
    /// Gets the executable used to run the JavaScript package manager.
    /// </summary>
    public string ExecutableName { get; } = executableName;

    /// <summary>
    /// Gets the command used to run a script with the JavaScript package manager.
    /// </summary>
    public string? ScriptCommand { get; } = runScriptCommand;

    /// <summary>
    /// Gets the BuildKit cache mount path for the package manager, or null if not supported.
    /// </summary>
    public string? CacheMount { get; } = cacheMount;

    /// <summary>
    /// Gets the file patterns for package dependency files. The first item in the tuple is the source pattern,
    /// and the second item is the destination pattern.
    /// </summary>
    public List<(string Source, string Destination)> PackageFilesPatterns { get; } = [];
}
