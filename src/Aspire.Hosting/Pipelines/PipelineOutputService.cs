// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Default implementation of <see cref="IPipelineOutputService"/>.
/// </summary>
[Experimental("ASPIREPIPELINES004", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class PipelineOutputService : IPipelineOutputService
{
    /// <summary>
    /// Stores the resolved output directory path, or <c>null</c> if not specified.
    /// </summary>
    private readonly string? _outputPath;

    /// <summary>
    /// Stores the path to the temporary directory for pipeline output.
    /// </summary>
    private readonly string _tempDirectory;

    public PipelineOutputService(IOptions<PipelineOptions> options, IDirectoryService directoryService)
    {
        ArgumentNullException.ThrowIfNull(directoryService);

        _outputPath = options.Value.OutputPath is not null ? Path.GetFullPath(options.Value.OutputPath) : null;
        _tempDirectory = directoryService.TempDirectory.GetSubdirectoryPath("pipelines");
    }

    /// <inheritdoc/>
    public string GetOutputDirectory()
    {
        return _outputPath ?? Path.Combine(Environment.CurrentDirectory, "aspire-output");
    }

    /// <inheritdoc/>
    public string GetOutputDirectory(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var baseOutputDir = GetOutputDirectory();
        return Path.Combine(baseOutputDir, resource.Name);
    }

    /// <inheritdoc/>
    public string GetTempDirectory()
    {
        return _tempDirectory;
    }

    /// <inheritdoc/>
    public string GetTempDirectory(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var baseTempDir = GetTempDirectory();
        return Path.Combine(baseTempDir, resource.Name);
    }
}
