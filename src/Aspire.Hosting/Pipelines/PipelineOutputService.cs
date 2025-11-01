// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
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
    /// Lazily creates and stores the path to the temporary directory for pipeline output.
    /// </summary>
    private readonly Lazy<string> _tempDirectory;

    public PipelineOutputService(IOptions<PipelineOptions> options, IConfiguration configuration)
    {
        _outputPath = options.Value.OutputPath is not null ? Path.GetFullPath(options.Value.OutputPath) : null;
        _tempDirectory = new Lazy<string>(() => CreateTempDirectory(configuration));
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
        return _tempDirectory.Value;
    }

    /// <inheritdoc/>
    public string GetTempDirectory(IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var baseTempDir = GetTempDirectory();
        return Path.Combine(baseTempDir, resource.Name);
    }

    /// <summary>
    /// Creates a temporary directory for pipeline build artifacts.
    /// Uses AppHost:PathSha256 from configuration to create an isolated temp directory per app host,
    /// enabling multiple app hosts to run concurrently without conflicts.
    /// If AppHost:PathSha256 is not available, falls back to a generic "aspire" temp directory.
    /// </summary>
    private static string CreateTempDirectory(IConfiguration configuration)
    {
        var appHostSha = configuration["AppHost:PathSha256"];

        if (!string.IsNullOrEmpty(appHostSha))
        {
            return Directory.CreateTempSubdirectory($"aspire-{appHostSha}").FullName;
        }

        // Fallback if AppHost:PathSha256 is not available
        return Directory.CreateTempSubdirectory("aspire").FullName;
    }
}
