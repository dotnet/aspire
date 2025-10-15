// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Aspire.Hosting.Python;

internal static partial class PythonVersionDetector
{
    /// <summary>
    /// Detects the Python version from .python-version file, pyproject.toml, or virtual environment.
    /// </summary>
    /// <param name="appDirectory">The directory containing the Python application.</param>
    /// <param name="virtualEnvironment">The virtual environment to check as a fallback.</param>
    /// <returns>The detected Python version in major.minor format (e.g., "3.13"), or null if not found.</returns>
    public static string? DetectVersion(string appDirectory, VirtualEnvironment virtualEnvironment)
    {
        // First, try .python-version file (most specific)
        var pythonVersionFile = Path.Combine(appDirectory, ".python-version");
        if (File.Exists(pythonVersionFile))
        {
            var version = File.ReadAllText(pythonVersionFile).Trim();
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
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

        // Third, try detecting from virtual environment as ultimate fallback
        return DetectVersionFromVirtualEnvironment(virtualEnvironment);
    }

    /// <summary>
    /// Detects the Python version by executing the Python executable from the virtual environment.
    /// </summary>
    /// <param name="virtualEnvironment">The virtual environment.</param>
    /// <returns>The detected Python version in major.minor format, or null if detection fails.</returns>
    private static string? DetectVersionFromVirtualEnvironment(VirtualEnvironment virtualEnvironment)
    {
        var pythonExecutable = virtualEnvironment.GetExecutable("python");

        if (!File.Exists(pythonExecutable))
        {
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            if (!process.WaitForExit(5000))
            {
                return null;
            }

            // Python 2.x outputs to stderr, Python 3.x to stdout
            var output = process.StandardOutput.ReadToEnd();
            if (string.IsNullOrWhiteSpace(output))
            {
                output = process.StandardError.ReadToEnd();
            }

            // Parse "Python X.Y.Z" format
            var match = PythonVersionOutputRegex().Match(output);
            if (match.Success && match.Groups.Count > 2)
            {
                return $"{match.Groups[1].Value}.{match.Groups[2].Value}";
            }
        }
        catch
        {
            // Ignore errors during version detection
            return null;
        }

        return null;
    }

    [GeneratedRegex(@"requires-python\s*=\s*[""'](?:>=|==)?(\d+\.\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex RequiresPythonRegex();

    [GeneratedRegex(@"Python\s+(\d+)\.(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PythonVersionOutputRegex();
}
