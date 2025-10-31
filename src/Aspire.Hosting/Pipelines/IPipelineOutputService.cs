// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Service for managing pipeline output directories.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPipelineOutputService
{
    /// <summary>
    /// Gets the output directory for deployment artifacts.
    /// </summary>
    /// <returns>The path to the output directory for deployment artifacts.</returns>
    string GetOutputDirectory();

    /// <summary>
    /// Gets a temporary directory for build artifacts.
    /// </summary>
    /// <returns>The path to a temporary directory for build artifacts.</returns>
    string GetTempDirectory();
}
