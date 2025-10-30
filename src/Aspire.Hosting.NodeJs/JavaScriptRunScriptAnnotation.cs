// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents the annotation for the script used during run mode in a JavaScript resource.
/// </summary>
/// <param name="scriptName">The name of the JavaScript package manager's run script.</param>
/// <param name="args">The command line arguments for the JavaScript package manager's run script.</param>
public sealed class JavaScriptRunScriptAnnotation(string scriptName, string[]? args) : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the script to run.
    /// </summary>
    public string ScriptName { get; } = scriptName;

    /// <summary>
    /// Gets the command-line arguments for the script.
    /// </summary>
    public string[] Args { get; } = args ?? [];
}
