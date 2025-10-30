// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents the annotation for the JavaScript package manager's install command.
/// </summary>
/// <param name="args">The command line arguments for the JavaScript package manager's install command.</param>
public sealed class JavaScriptInstallCommandAnnotation(string[] args) : IResourceAnnotation
{
    /// <summary>
    /// Gets the command-line arguments supplied to the application.
    /// </summary>
    public string[] Args { get; } = args;
}
