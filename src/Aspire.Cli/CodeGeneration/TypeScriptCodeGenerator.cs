// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Projects;
using Aspire.Hosting.CodeGeneration.Models;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Code generator for TypeScript AppHost projects.
/// </summary>
internal sealed class TypeScriptCodeGenerator : ICodeGenerator
{
    private const string GeneratedFolderName = ".aspire-gen";
    private const string HashFileName = ".codegen-hash";

    private readonly ILogger<TypeScriptCodeGenerator> _logger;

    public TypeScriptCodeGenerator(ILogger<TypeScriptCodeGenerator> logger)
    {
        _logger = logger;
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

        // Create the application model from the packages
        var model = CreateApplicationModel(packagesList);

        // Generate the code
        var generator = new Aspire.Hosting.CodeGeneration.TypeScript.TypeScriptCodeGenerator();
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

    private static ApplicationModel CreateApplicationModel(List<(string PackageId, string Version)> packages)
    {
        var integrations = new List<IntegrationModel>();

        foreach (var (packageId, version) in packages)
        {
            // Create a basic integration model for each package
            // In a full implementation, this would scan the actual assemblies
            var integration = new IntegrationModel
            {
                PackageId = packageId,
                Version = version,
                ExtensionMethods = CreateExtensionMethodsForPackage(packageId)
            };

            integrations.Add(integration);
        }

        return new ApplicationModel
        {
            Integrations = integrations
        };
    }

    private static List<ExtensionMethodModel> CreateExtensionMethodsForPackage(string packageId)
    {
        // Extract the resource name from the package ID
        // e.g., "Aspire.Hosting.Redis" -> "Redis"
        var parts = packageId.Split('.');
        var resourceName = parts.Length > 2 ? parts[^1] : parts[^1];

        // Create common extension methods for this integration
        var methods = new List<ExtensionMethodModel>();

        // Add the main "AddXxx" method
        if (!string.Equals(packageId, "Aspire.Hosting", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(packageId, "Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase))
        {
            methods.Add(new ExtensionMethodModel
            {
                Name = string.Create(CultureInfo.InvariantCulture, $"Add{resourceName}"),
                ExtendedType = "IDistributedApplicationBuilder",
                ReturnType = string.Create(CultureInfo.InvariantCulture, $"IResourceBuilder<{resourceName}Resource>"),
                ResourceType = string.Create(CultureInfo.InvariantCulture, $"{resourceName}Resource"),
                ContainingType = string.Create(CultureInfo.InvariantCulture, $"{resourceName}BuilderExtensions"),
                Parameters =
                [
                    new ParameterModel
                    {
                        Name = "builder",
                        Type = "IDistributedApplicationBuilder",
                        IsThis = true
                    },
                    new ParameterModel
                    {
                        Name = "name",
                        Type = "string"
                    }
                ]
            });
        }

        return methods;
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
