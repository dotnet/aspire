// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration;

/// <summary>
/// Orchestrates code generation for polyglot AppHosts.
/// This service handles the common workflow: loading assemblies, creating the application model,
/// calling the language-specific generator, writing output files, and caching.
/// </summary>
public sealed class CodeGeneratorService
{
    private const string HashFileName = ".codegen-hash";

    /// <summary>
    /// Generates SDK code for the specified packages using the provided code generator.
    /// </summary>
    /// <param name="appPath">The path to the app.</param>
    /// <param name="generator">The language-specific code generator.</param>
    /// <param name="packages">The Aspire packages to generate code for.</param>
    /// <param name="assemblySearchPaths">Paths to search for assemblies (build output, NuGet cache, runtime directories).</param>
    /// <param name="outputFolderName">The name of the output folder (e.g., ".modules").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of files generated.</returns>
    public async Task<int> GenerateAsync(
        string appPath,
        ICodeGenerator generator,
        IEnumerable<(string PackageId, string Version)> packages,
        IEnumerable<string> assemblySearchPaths,
        string outputFolderName,
        CancellationToken cancellationToken)
    {
        var packagesList = packages.ToList();
        var searchPaths = assemblySearchPaths.ToList();

        // Create the application model by loading assemblies
        using var model = CreateApplicationModel(appPath, packagesList, searchPaths);

        // Generate the code using the language-specific generator
        var files = generator.GenerateDistributedApplication(model);

        // Write the files to the generated folder
        var generatedPath = Path.Combine(appPath, outputFolderName);
        Directory.CreateDirectory(generatedPath);

        foreach (var (relativePath, content) in files)
        {
            var fullPath = Path.Combine(generatedPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
        }

        // Save the hash for future comparison
        SaveGenerationHash(generatedPath, packagesList);

        return files.Count;
    }

    /// <summary>
    /// Checks if code generation is needed based on the current state.
    /// </summary>
    /// <param name="appPath">The path to the app.</param>
    /// <param name="packages">The packages to check.</param>
    /// <param name="outputFolderName">The name of the output folder.</param>
    /// <returns>True if generation is needed, false otherwise.</returns>
    public bool NeedsGeneration(
        string appPath,
        IEnumerable<(string PackageId, string Version)> packages,
        string outputFolderName)
    {
        var packagesList = packages.ToList();
        var generatedPath = Path.Combine(appPath, outputFolderName);

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

    private static CodeGenApplicationModel CreateApplicationModel(
        string appPath,
        List<(string PackageId, string Version)> packages,
        List<string> searchPaths)
    {
        // Load assemblies from the build output and other search paths
        var assemblyLoaderContext = new AssemblyLoaderContext();
        var integrations = new List<IntegrationModel>();

        // Load core runtime assemblies first (required for WellKnownTypes)
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
                continue;
            }

            // Create WellKnownTypes from the first loaded assembly context
            wellKnownTypes ??= new WellKnownTypes(assemblyLoaderContext);

            var integration = IntegrationModel.Create(wellKnownTypes, assembly);
            integrations.Add(integration);
        }

        return CodeGenApplicationModel.Create(integrations, appPath, assemblyLoaderContext);
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

    private static void SaveGenerationHash(string generatedPath, List<(string PackageId, string Version)> packages)
    {
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
