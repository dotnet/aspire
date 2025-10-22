// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.NodeJs;

/// <summary>
/// Represents the annotation for the JavaScript package manager used in a resource.
/// </summary>
/// <param name="packageManager">The name of the JavaScript package manager.</param>
public sealed class JavaScriptPackageManagerAnnotation(string packageManager) : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the JavaScript package manager.
    /// </summary>
    public string PackageManager { get; } = packageManager;

    /// <summary>
    /// Gets the command line arguments for the JavaScript package manager.
    /// </summary>
    public string[] CommandLineArgs { get; init; } = [];

}