// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Python;

internal static partial class PythonVersionDetector
{
    /// <summary>
    /// Detects the Python version from .python-version file or pyproject.toml.
    /// </summary>
    /// <param name="appDirectory">The directory containing the Python application.</param>
    /// <returns>The detected Python version in major.minor format (e.g., "3.13"), or null if not found.</returns>
    public static string? DetectVersion(string appDirectory)
    {
        // First, try .python-version file (most specific)
        var pythonVersionFile = Path.Combine(appDirectory, ".python-version");
        if (File.Exists(pythonVersionFile))
        {
            var version = File.ReadAllText(pythonVersionFile).Trim();
            if (!string.IsNullOrWhiteSpace(version))
            {
                // Extract major.minor (e.g., "3.13" from "3.13.0" or "3.13")
                var parts = version.Split('.');
                if (parts.Length >= 2)
                {
                    return $"{parts[0]}.{parts[1]}";
                }
            }
        }

        // Second, try pyproject.toml
        var pyprojectFile = Path.Combine(appDirectory, "pyproject.toml");
        if (File.Exists(pyprojectFile))
        {
            var content = File.ReadAllText(pyprojectFile);
            // Look for requires-python = ">=X.Y" or "==X.Y"
            var match = RequiresPythonRegex().Match(content);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    [GeneratedRegex(@"requires-python\s*=\s*[""'](?:>=|==)?(\d+\.\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex RequiresPythonRegex();
}
