// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.RegularExpressions;
using Aspire.Hosting.Execution;

namespace Aspire.Hosting.Python;

internal static partial class PythonVersionDetector
{
    /// <summary>
    /// Detects the Python version from .python-version file, pyproject.toml, or virtual environment.
    /// </summary>
    /// <param name="appDirectory">The directory containing the Python application.</param>
    /// <param name="virtualEnvironment">The virtual environment to check as a fallback.</param>
    /// <param name="shell">The virtual shell to use for process execution.</param>
    /// <returns>The detected Python version in major.minor format (e.g., "3.13"), or null if not found.</returns>
    public static async Task<string?> DetectVersionAsync(string appDirectory, VirtualEnvironment? virtualEnvironment, IVirtualShell shell)
    {
        // First, try .python-version file (most specific)
        var pythonVersionFile = Path.Combine(appDirectory, ".python-version");
        if (File.Exists(pythonVersionFile))
        {
            var version = (await File.ReadAllTextAsync(pythonVersionFile).ConfigureAwait(false)).Trim();
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }
        }

        // Second, try pyproject.toml
        var pyprojectFile = Path.Combine(appDirectory, "pyproject.toml");
        if (File.Exists(pyprojectFile))
        {
            var content = await File.ReadAllTextAsync(pyprojectFile).ConfigureAwait(false);
            // Look for requires-python = ">=X.Y" or "==X.Y"
            var match = RequiresPythonRegex().Match(content);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        // Third, try detecting from virtual environment as ultimate fallback
        if (virtualEnvironment != null)
        {
            return await DetectVersionFromVirtualEnvironmentAsync(virtualEnvironment, shell).ConfigureAwait(false);
        }

        return null;
    }

    /// <summary>
    /// Detects the Python version by executing the Python executable from the virtual environment.
    /// </summary>
    /// <param name="virtualEnvironment">The virtual environment.</param>
    /// <param name="shell">The virtual shell to use for process execution.</param>
    /// <returns>The detected Python version in major.minor format, or null if detection fails.</returns>
    private static async Task<string?> DetectVersionFromVirtualEnvironmentAsync(VirtualEnvironment virtualEnvironment, IVirtualShell shell)
    {
        var pythonExecutable = virtualEnvironment.GetExecutable("python");

        if (!File.Exists(pythonExecutable))
        {
            return null;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await shell
                .Command(pythonExecutable, ["--version"])
                .RunAsync(cts.Token)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                return null;
            }

            // Python 2.x outputs to stderr, Python 3.x to stdout
            var output = result.Stdout;
            if (string.IsNullOrWhiteSpace(output))
            {
                output = result.Stderr;
            }

            // Parse "Python X.Y.Z" format
            var match = PythonVersionOutputRegex().Match(output ?? string.Empty);
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
