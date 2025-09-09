// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Provides fallback XML parsing capabilities when MSBuild evaluation fails.
/// Used primarily for AppHost projects with unresolvable SDK versions.
/// </summary>
internal sealed class FallbackProjectParser
{
    private readonly ILogger<FallbackProjectParser> _logger;

    public FallbackProjectParser(ILogger<FallbackProjectParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses a project file using direct XML parsing to extract basic project information.
    /// Returns a synthetic JsonDocument that mimics MSBuild's GetProjectItemsAndProperties output.
    /// </summary>
    public JsonDocument ParseProject(FileInfo projectFile)
    {
        try
        {
            _logger.LogDebug("Parsing project file '{ProjectFile}' using fallback XML parser", projectFile.FullName);

            var doc = XDocument.Load(projectFile.FullName);
            var root = doc.Root;

            if (root?.Name.LocalName != "Project")
            {
                throw new InvalidOperationException($"Invalid project file format: {projectFile.FullName}");
            }

            // Extract SDK information
            var aspireHostingSdkVersion = ExtractAspireHostingSdkVersion(root);

            // Extract package references
            var packageReferences = ExtractPackageReferences(root);

            // Extract project references
            var projectReferences = ExtractProjectReferences(root, projectFile);

            // Build the synthetic JSON structure
            var jsonBuilder = new StringBuilder();
            jsonBuilder.AppendLine("{");
            
            // Items section
            jsonBuilder.AppendLine("  \"Items\": {");
            
            // PackageReference items
            jsonBuilder.AppendLine("    \"PackageReference\": [");
            for (int i = 0; i < packageReferences.Length; i++)
            {
                var pkg = packageReferences[i];
                jsonBuilder.Append("      {");
                jsonBuilder.Append(CultureInfo.InvariantCulture, $"\"Identity\": \"{EscapeJsonString(pkg.Identity)}\", ");
                jsonBuilder.Append(CultureInfo.InvariantCulture, $"\"Version\": \"{EscapeJsonString(pkg.Version)}\"");
                jsonBuilder.Append('}');
                if (i < packageReferences.Length - 1)
                {
                    jsonBuilder.Append(',');
                }
                jsonBuilder.AppendLine();
            }
            jsonBuilder.AppendLine("    ],");
            
            // ProjectReference items
            jsonBuilder.AppendLine("    \"ProjectReference\": [");
            for (int i = 0; i < projectReferences.Length; i++)
            {
                var proj = projectReferences[i];
                jsonBuilder.Append("      {");
                jsonBuilder.Append(CultureInfo.InvariantCulture, $"\"Identity\": \"{EscapeJsonString(proj.Identity)}\", ");
                jsonBuilder.Append(CultureInfo.InvariantCulture, $"\"FullPath\": \"{EscapeJsonString(proj.FullPath)}\"");
                jsonBuilder.Append('}');
                if (i < projectReferences.Length - 1)
                {
                    jsonBuilder.Append(',');
                }
                jsonBuilder.AppendLine();
            }
            jsonBuilder.AppendLine("    ]");
            
            jsonBuilder.AppendLine("  },");
            
            // Properties section
            jsonBuilder.AppendLine("  \"Properties\": {");
            jsonBuilder.AppendLine(CultureInfo.InvariantCulture, $"    \"AspireHostingSDKVersion\": \"{EscapeJsonString(aspireHostingSdkVersion)}\"");
            jsonBuilder.AppendLine("  },");
            
            // Fallback flag
            jsonBuilder.AppendLine("  \"Fallback\": true");
            jsonBuilder.AppendLine("}");

            var json = jsonBuilder.ToString();
            return JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse project file '{ProjectFile}' using fallback XML parser", projectFile.FullName);
            throw new ProjectUpdaterException($"Failed to parse project file '{projectFile.FullName}' using fallback XML parser: {ex.Message}", ex);
        }
    }

    private static string? ExtractAspireHostingSdkVersion(XElement projectRoot)
    {
        // Look for <Sdk Name="Aspire.AppHost.Sdk" Version="..." />
        var sdkElement = projectRoot
            .Elements("Sdk")
            .FirstOrDefault(e => e.Attribute("Name")?.Value == "Aspire.AppHost.Sdk");

        return sdkElement?.Attribute("Version")?.Value;
    }

    private static PackageReferenceInfo[] ExtractPackageReferences(XElement projectRoot)
    {
        var packageReferences = new List<PackageReferenceInfo>();

        // Find all PackageReference elements
        var packageRefElements = projectRoot
            .Descendants("PackageReference")
            .Where(e => !string.IsNullOrEmpty(e.Attribute("Include")?.Value) || !string.IsNullOrEmpty(e.Attribute("Update")?.Value));

        foreach (var element in packageRefElements)
        {
            var identity = element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value;
            if (string.IsNullOrEmpty(identity))
            {
                continue;
            }

            // Try to get version from attribute first, then from child element
            var version = element.Attribute("Version")?.Value ?? 
                         element.Element("Version")?.Value;

            var packageRef = new PackageReferenceInfo
            {
                Identity = identity,
                Version = version ?? string.Empty
            };

            packageReferences.Add(packageRef);
        }

        return packageReferences.ToArray();
    }

    private static ProjectReferenceInfo[] ExtractProjectReferences(XElement projectRoot, FileInfo projectFile)
    {
        var projectReferences = new List<ProjectReferenceInfo>();

        // Find all ProjectReference elements
        var projectRefElements = projectRoot
            .Descendants("ProjectReference")
            .Where(e => !string.IsNullOrEmpty(e.Attribute("Include")?.Value));

        foreach (var element in projectRefElements)
        {
            var include = element.Attribute("Include")?.Value;
            if (string.IsNullOrEmpty(include))
            {
                continue;
            }

            // Convert relative path to absolute path
            var fullPath = Path.IsPathRooted(include) 
                ? include 
                : Path.GetFullPath(Path.Combine(projectFile.DirectoryName!, include));

            var projectRef = new ProjectReferenceInfo
            {
                Identity = include,
                FullPath = fullPath
            };

            projectReferences.Add(projectRef);
        }

        return projectReferences.ToArray();
    }

    private static string EscapeJsonString(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

internal record PackageReferenceInfo
{
    public required string Identity { get; init; }
    public required string Version { get; init; }
}

internal record ProjectReferenceInfo
{
    public required string Identity { get; init; }
    public required string FullPath { get; init; }
}