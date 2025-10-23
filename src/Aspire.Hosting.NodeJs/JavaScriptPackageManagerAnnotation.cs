// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents the annotation for the JavaScript package manager's install command line arguments.
/// </summary>
/// <param name="command"></param>
/// <param name="args">The command line arguments for the JavaScript package manager's install command.</param>
public record JavaScriptInstallCommandAnnotation(string command, string[] args) : IResourceAnnotation
{
}

/// <summary>
/// Represents the annotation for the JavaScript package manager's run command line arguments.
/// </summary>
public record JavaScriptRunCommandAnnotation(string command, string[] args) : IResourceAnnotation
{
}

/// <summary>
/// Represents the annotation for the JavaScript package manager's build command line arguments.
/// </summary>
public record JavaScriptBuildCommandAnnotation(string command, string[] args) : IResourceAnnotation
{
}
