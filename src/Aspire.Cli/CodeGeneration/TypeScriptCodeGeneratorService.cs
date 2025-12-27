// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Cli.Rosetta;
using Aspire.Hosting.CodeGeneration;
using Aspire.Hosting.CodeGeneration.TypeScript;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Code generator service for TypeScript AppHost projects.
/// Uses the shared CodeGeneratorService with the TypeScript code generator.
/// </summary>
internal sealed class TypeScriptCodeGeneratorService : ICodeGenerator
{
    private const string GeneratedFolderName = ".modules";

    private readonly ILogger<TypeScriptCodeGeneratorService> _logger;
    private readonly IConfiguration _configuration;
    private readonly CodeGeneratorService _codeGeneratorService;
    private readonly TypeScriptCodeGenerator _typeScriptGenerator;

    public TypeScriptCodeGeneratorService(ILogger<TypeScriptCodeGeneratorService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _codeGeneratorService = new CodeGeneratorService();
        _typeScriptGenerator = new TypeScriptCodeGenerator();
    }

    /// <inheritdoc />
    public AppHostType SupportedType => AppHostType.TypeScript;

    /// <inheritdoc />
    public async Task GenerateAsync(
        string appPath,
        IEnumerable<(string PackageId, string Version)> packages,
        CancellationToken cancellationToken)
    {
        var packagesList = packages.ToList();
        _logger.LogDebug("Generating TypeScript code for {Count} packages", packagesList.Count);

        // Build assembly search paths
        var projectModel = new ProjectModel(appPath);
        var searchPaths = BuildAssemblySearchPaths(projectModel.BuildPath);

        // Use the shared code generator service
        var fileCount = await _codeGeneratorService.GenerateAsync(
            appPath,
            _typeScriptGenerator,
            packagesList,
            searchPaths,
            GeneratedFolderName,
            cancellationToken);

        _logger.LogInformation("Generated {Count} TypeScript files in {Path}",
            fileCount, Path.Combine(appPath, GeneratedFolderName));
    }

    /// <inheritdoc />
    public bool NeedsGeneration(string appPath, IEnumerable<(string PackageId, string Version)> packages)
    {
        // In dev mode (ASPIRE_REPO_ROOT set), always regenerate to pick up code changes
        if (!string.IsNullOrEmpty(_configuration["ASPIRE_REPO_ROOT"]))
        {
            _logger.LogDebug("Dev mode detected (ASPIRE_REPO_ROOT set), skipping generation cache");
            return true;
        }

        return _codeGeneratorService.NeedsGeneration(appPath, packages, GeneratedFolderName);
    }

    /// <summary>
    /// Builds the list of paths to search for assemblies.
    /// </summary>
    private static List<string> BuildAssemblySearchPaths(string buildPath)
    {
        var searchPaths = new List<string> { buildPath };

        // Add NuGet cache if available
        var nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");
        if (Directory.Exists(nugetCache))
        {
            searchPaths.Add(nugetCache);
        }

        // Add runtime directory
        var runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        searchPaths.Add(runtimeDirectory);

        // Add ASP.NET Core shared framework directory (contains HealthChecks, etc.)
        var aspnetCoreDirectory = runtimeDirectory.Replace("Microsoft.NETCore.App", "Microsoft.AspNetCore.App");
        if (Directory.Exists(aspnetCoreDirectory))
        {
            searchPaths.Add(aspnetCoreDirectory);
        }

        return searchPaths;
    }
}
