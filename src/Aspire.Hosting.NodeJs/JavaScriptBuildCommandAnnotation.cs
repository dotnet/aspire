// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents the annotation for the JavaScript package manager's build command.
/// </summary>
/// <param name="command">The executable command name</param>
/// <param name="args">The command line arguments for the JavaScript package manager's install command.</param>
public sealed class JavaScriptBuildCommandAnnotation(string command, string[] args) : IResourceAnnotation
{
    /// <summary>
    /// Gets the executable command name.
    /// </summary>
    public string Command { get; } = command;

    /// <summary>
    /// Gets the command-line arguments supplied to the application.
    /// </summary>
    public string[] Args { get; } = args;
}
