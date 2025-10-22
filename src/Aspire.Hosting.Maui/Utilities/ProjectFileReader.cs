// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Aspire.Hosting.Maui.Utilities;

/// <summary>
/// Provides utilities for reading and parsing MAUI project files.
/// </summary>
internal static class ProjectFileReader
{
    /// <summary>
    /// Gets the target framework matching the specified platform from the project file.
    /// </summary>
    /// <param name="projectPath">The path to the project file to parse.</param>
    /// <param name="platformIdentifier">The platform identifier to search for (e.g., "windows", "android", "ios", "maccatalyst").</param>
    /// <returns>The matching TFM if found, otherwise null.</returns>
    /// <remarks>
    /// This method parses all TargetFrameworks and TargetFramework elements in the project file,
    /// including conditional elements. It searches for a target framework containing the specified
    /// platform identifier (case-insensitive) and returns the first match.
    /// </remarks>
    public static string? GetPlatformTargetFramework(string projectPath, string platformIdentifier)
    {
        try
        {
            var projectDoc = XDocument.Load(projectPath);

            // Check all TargetFrameworks and TargetFramework elements (including conditional ones)
            var allTargetFrameworkElements = projectDoc.Descendants()
                .Where(e => e.Name.LocalName == "TargetFrameworks" || e.Name.LocalName == "TargetFramework");

            foreach (var element in allTargetFrameworkElements)
            {
                var value = element.Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                // Check if any TFM in the value contains the platform identifier and return the first one
                var platformTfm = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault(tfm => tfm.Contains($"-{platformIdentifier}", StringComparison.OrdinalIgnoreCase));

                if (platformTfm != null)
                {
                    return platformTfm;
                }
            }

            return null;
        }
        catch
        {
            // If we can't read the project file, return null to indicate unknown
            return null;
        }
    }
}
