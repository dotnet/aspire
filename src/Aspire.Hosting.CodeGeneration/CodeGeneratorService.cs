// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aspire.Hosting.Ats;
using Aspire.Hosting.CodeGeneration.Ats;
using Aspire.Hosting.CodeGeneration.Models;
using Aspire.Hosting.CodeGeneration.Models.Types;

namespace Aspire.Hosting.CodeGeneration;

/// <summary>
/// Orchestrates code generation for polyglot AppHosts.
/// This service handles the common workflow: loading assemblies, creating the application model,
/// calling the language-specific generator, writing output files, and caching.
/// </summary>
internal sealed class CodeGeneratorService
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
    public static async Task<int> GenerateAsync(
        string appPath,
        ICodeGenerator generator,
        IEnumerable<(string PackageId, string Version)> packages,
        IEnumerable<string> assemblySearchPaths,
        string outputFolderName,
        CancellationToken cancellationToken)
    {
        var packagesList = packages.ToList();
        var searchPaths = assemblySearchPaths.ToList();

        // Scan assemblies for capabilities and DTO types
        using var context = new AssemblyLoaderContext();
        var scanResult = ScanCapabilities(context, packagesList, searchPaths);

        // Generate the code using the language-specific generator
        var files = generator.GenerateDistributedApplication(scanResult.Capabilities, scanResult.DtoTypes);

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
    public static bool NeedsGeneration(
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

    private static AtsCapabilityScanner.ScanResult ScanCapabilities(
        AssemblyLoaderContext assemblyLoaderContext,
        List<(string PackageId, string Version)> packages,
        List<string> searchPaths)
    {
        // Load core runtime assemblies first
        assemblyLoaderContext.LoadAssembly("System.Private.CoreLib", searchPaths, loadDependencies: true);
        assemblyLoaderContext.LoadAssembly("System.Runtime", searchPaths, loadDependencies: true);

        // Load Aspire.Hosting for core types and type mappings
        var hostingAssembly = assemblyLoaderContext.LoadAssembly("Aspire.Hosting", searchPaths, loadDependencies: true);
        if (hostingAssembly is null)
        {
            return new AtsCapabilityScanner.ScanResult
            {
                Capabilities = [],
                TypeInfos = []
            };
        }

        // Create well-known types resolver
        var wellKnownTypes = new WellKnownTypes(assemblyLoaderContext);

        // Collect all assemblies to scan
        var assembliesToScan = new List<RoAssembly> { hostingAssembly };

        foreach (var (packageId, version) in packages)
        {
            var assembly = TryLoadPackageAssembly(assemblyLoaderContext, packageId, version, searchPaths);
            if (assembly is not null)
            {
                assembliesToScan.Add(assembly);
            }
        }

        // Create type mapping from all assemblies
        var typeMapping = AtsTypeMapping.FromAssemblies(
            assembliesToScan.Select(a => new RoAssemblyInfoWrapper(a)));

        // Scan capabilities from all assemblies using 2-pass scanning
        // This ensures cross-assembly type expansion works correctly
        // (e.g., withEnvironment from Aspire.Hosting expands to RedisResource from Aspire.Hosting.Redis)
        return AtsCapabilityScannerExtensions.ScanAssembliesWithTypeInfo(
            assembliesToScan,
            typeMapping);
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
