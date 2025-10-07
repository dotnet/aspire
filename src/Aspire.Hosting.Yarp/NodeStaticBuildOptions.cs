// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Yarp;

/// <summary>
/// Configuration options for Node.js static build in YARP npm resources.
/// </summary>
[Experimental("ASPIREHOSTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
internal sealed class NodeStaticBuildOptions
{
    /// <summary>
    /// Gets or sets the package manager to use (e.g., "npm", "pnpm", "yarn").
    /// </summary>
    public string PackageManager { get; set; } = "npm";

    /// <summary>
    /// Gets or sets the install command (e.g., "install", "ci", "install --frozen-lockfile").
    /// </summary>
    public string InstallCommand { get; set; } = "install";

    /// <summary>
    /// Gets or sets the build command (e.g., "run build").
    /// </summary>
    public string BuildCommand { get; set; } = "run build";

    /// <summary>
    /// Gets or sets the output directory containing the built static assets.
    /// </summary>
    public string OutputDir { get; set; } = "dist";

    /// <summary>
    /// Gets or sets the Node.js version to use.
    /// </summary>
    public string NodeVersion { get; set; } = "22";
}
