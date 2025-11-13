// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Represents the annotation for the JavaScript package manager used in a resource.
/// </summary>
/// <param name="executableName">The name of the executable used to run the package manager.</param>
/// <param name="runScriptCommand">The command used to run a script with the JavaScript package manager.</param>
public sealed class JavaScriptPackageManagerAnnotation(string executableName, string? runScriptCommand) : IResourceAnnotation
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
    /// Gets the string used to separate individual commands in a command sequence, or <see langword="null"/> if one shouldn't be used.
    /// Defaults to "--".
    /// </summary>
    public string? CommandSeparator { get; init; } = "--";

    /// <summary>
    /// Gets the URL with installation instructions for this package manager.
    /// </summary>
    public string? InstallHelpLink { get; init; }
}
