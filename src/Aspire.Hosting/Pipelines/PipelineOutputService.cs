// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Default implementation of <see cref="IPipelineOutputService"/>.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal sealed class PipelineOutputService : IPipelineOutputService
{
    private readonly string? _outputPath;
    private readonly Lazy<string> _tempDirectory;

    public PipelineOutputService(string? outputPath, IConfiguration configuration)
    {
        _outputPath = outputPath;
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
