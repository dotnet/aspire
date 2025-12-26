// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Projects;
using Aspire.Cli.Rosetta;
using Aspire.Hosting.CodeGeneration;
using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;
using LibCodeGen = Aspire.Hosting.CodeGeneration.TypeScript;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Code generator service for TypeScript AppHost projects.
/// Uses the Aspire.Hosting.CodeGeneration.TypeScript library to generate rich TypeScript SDK code.
/// </summary>
internal sealed class TypeScriptCodeGeneratorService : ICodeGenerator
{
    private const string GeneratedFolderName = ".modules";
    private const string HashFileName = ".codegen-hash";

    private readonly ILogger<TypeScriptCodeGeneratorService> _logger;
    private readonly IConfiguration _configuration;

    public TypeScriptCodeGeneratorService(ILogger<TypeScriptCodeGeneratorService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
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

        // Use the project model - build should already be complete
        // (TypeScriptAppHostProject builds before calling code generation)
        var projectModel = new ProjectModel(appPath);

        // Create the application model by loading assemblies from the build output
        using var model = await CreateApplicationModelAsync(appPath, projectModel, packagesList);

        // Generate the code using the library
        var generator = new LibCodeGen.TypeScriptCodeGenerator();
        var files = generator.GenerateDistributedApplication(model);

        // Write the files to the generated folder
        var generatedPath = Path.Combine(appPath, GeneratedFolderName);
        Directory.CreateDirectory(generatedPath);

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(generatedPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
            _logger.LogDebug("Generated: {FilePath}", fullPath);
        }

        // Save the hash for future comparison
        SaveGenerationHash(appPath, packagesList);

        _logger.LogInformation("Generated {Count} TypeScript files in {Path}", files.Count, generatedPath);
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

        var packagesList = packages.ToList();
        var generatedPath = Path.Combine(appPath, GeneratedFolderName);

        // If the generated folder doesn't exist, we need to generate
        if (!Directory.Exists(generatedPath))
        {
            return true;
        }

        // Check if the hash matches
        var hashPath = Path.Combine(generatedPath, HashFileName);
        if (!File.Exists(hashPath))
        {
            return true;
        }

        var savedHash = File.ReadAllText(hashPath);
        var currentHash = ComputePackagesHash(packagesList);

        return savedHash != currentHash;
    }

    private Task<ApplicationModel> CreateApplicationModelAsync(
        string appPath,
        ProjectModel projectModel,
        List<(string PackageId, string Version)> packages)
    {
        // Load assemblies from the build output and NuGet cache
        var assemblyLoaderContext = new AssemblyLoaderContext();
        var integrations = new List<IntegrationModel>();

        // Find assembly paths from the project's build output
        var buildPath = projectModel.BuildPath;
        var searchPaths = new List<string> { buildPath };

        // Also search in NuGet cache if available
        var nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");
        if (Directory.Exists(nugetCache))
        {
            searchPaths.Add(nugetCache);
        }

        // Load core runtime assemblies first (required for WellKnownTypes)
        // These are in the build output when SelfContained=true, otherwise fall back to the runtime directories
        var runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

        // Add runtime directory to search paths so referenced assemblies can be found
        searchPaths.Add(runtimeDirectory);

        // Also add the ASP.NET Core shared framework directory (contains HealthChecks, etc.)
        var aspnetCoreDirectory = runtimeDirectory.Replace("Microsoft.NETCore.App", "Microsoft.AspNetCore.App");
        if (Directory.Exists(aspnetCoreDirectory))
        {
            searchPaths.Add(aspnetCoreDirectory);
        }

        // Load core runtime assemblies using full search paths so dependencies can be resolved
        assemblyLoaderContext.LoadAssembly("System.Private.CoreLib", searchPaths, loadDependencies: true);
        assemblyLoaderContext.LoadAssembly("System.Runtime", searchPaths, loadDependencies: true);

        // Load Aspire.Hosting for well-known types
        assemblyLoaderContext.LoadAssembly("Aspire.Hosting", searchPaths, loadDependencies: true);

        WellKnownTypes? wellKnownTypes = null;

        foreach (var (packageId, version) in packages)
        {
            // Try to load the assembly for this package
            var assembly = TryLoadPackageAssembly(assemblyLoaderContext, packageId, version, searchPaths);
            if (assembly is null)
            {
                _logger.LogWarning("Could not load assembly for package {PackageId}", packageId);
                continue;
            }

            // Create WellKnownTypes from the first loaded assembly context
            wellKnownTypes ??= new WellKnownTypes(assemblyLoaderContext);

            var integration = IntegrationModel.Create(wellKnownTypes, assembly);
            integrations.Add(integration);
        }

        var model = ApplicationModel.Create(integrations, appPath, assemblyLoaderContext);
        return Task.FromResult(model);
    }

    private static RoAssembly? TryLoadPackageAssembly(
        AssemblyLoaderContext context,
        string packageId,
        string version,
        List<string> searchPaths)
    {
        // Try common assembly naming patterns
        var assemblyNames = new[]
        {
            packageId,
            packageId.Replace(".", string.Empty),
        };

        foreach (var assemblyName in assemblyNames)
        {
            var assembly = context.LoadAssembly(assemblyName, searchPaths, loadDependencies: true);
            if (assembly is not null)
            {
                return assembly;
            }
        }

        // Try to find in NuGet cache with version path
        var nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");

        var packagePath = Path.Combine(nugetCache, packageId.ToLowerInvariant(), version.ToLowerInvariant());
        if (Directory.Exists(packagePath))
        {
            // Look for DLLs in lib/net* folders
            var libPath = Path.Combine(packagePath, "lib");
            if (Directory.Exists(libPath))
            {
                var frameworks = Directory.GetDirectories(libPath)
                    .Where(d => Path.GetFileName(d).StartsWith("net", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d); // Prefer newer frameworks

                foreach (var frameworkPath in frameworks)
                {
                    // Include the framework path along with all search paths for dependency resolution
                    var allPaths = new List<string> { frameworkPath };
                    allPaths.AddRange(searchPaths);
                    var assembly = context.LoadAssembly(packageId, allPaths, loadDependencies: true);
                    if (assembly is not null)
                    {
                        return assembly;
                    }
                }
            }
        }

        return null;
    }

    private static void SaveGenerationHash(string appPath, List<(string PackageId, string Version)> packages)
    {
        var generatedPath = Path.Combine(appPath, GeneratedFolderName);
        var hashPath = Path.Combine(generatedPath, HashFileName);
        var hash = ComputePackagesHash(packages);
        File.WriteAllText(hashPath, hash);
    }

    private static string ComputePackagesHash(List<(string PackageId, string Version)> packages)
    {
        var sb = new StringBuilder();
        foreach (var (packageId, version) in packages.OrderBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append(CultureInfo.InvariantCulture, $"{packageId}:{version};");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
