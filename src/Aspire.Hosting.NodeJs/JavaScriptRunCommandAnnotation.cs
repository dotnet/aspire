// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents the annotation for the JavaScript resource's initial run command line arguments.
/// </summary>
/// <remarks>
/// The Resource contains the command name, while this annotation contains only the arguments.
/// These arguments are applied to the command before any user supplied arguments.
/// </remarks>
public sealed class JavaScriptRunCommandAnnotation(string[] args) : IResourceAnnotation
{
    /// <summary>
    /// Gets the command-line arguments supplied to the application.
    /// </summary>
    public string[] Args { get; } = args;
}
