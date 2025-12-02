// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Agents.CopilotCli;

/// <summary>
/// Runs GitHub Copilot CLI commands.
/// </summary>
/// <param name="logger">The logger for diagnostic output.</param>
internal sealed class CopilotCliRunner(ILogger<CopilotCliRunner> logger) : ICopilotCliRunner
{
    /// <inheritdoc />
    public async Task<SemVersion?> GetVersionAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking for GitHub Copilot CLI installation");

        // Check if we're running in a VSCode terminal
        if (IsRunningInVSCode())
        {
            logger.LogDebug("Detected VSCode terminal environment. Assuming GitHub Copilot CLI is available to avoid potential hangs from interactive installation prompts.");
            // Return a dummy version to indicate Copilot is assumed to be available
            // The user will be prompted to configure it, and if they don't have it installed,
            // they'll be prompted to install it when they try to use it
            return new SemVersion(1, 0, 0);
        }

        var executablePath = PathLookupHelper.FindFullPathFromPath("copilot");
        if (executablePath is null)
        {
            logger.LogDebug("GitHub Copilot CLI is not installed or not found in PATH");
            return null;
        }

        try
        {
            var startInfo = new ProcessStartInfo(executablePath, "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var errorOutput = await errorTask.ConfigureAwait(false);
                logger.LogDebug("GitHub Copilot CLI returned non-zero exit code {ExitCode}: {Error}", process.ExitCode, errorOutput.Trim());
                return null;
            }

            var output = await outputTask.ConfigureAwait(false);
            var versionString = output.Trim();

            if (string.IsNullOrEmpty(versionString))
            {
                logger.LogDebug("GitHub Copilot CLI returned empty version output");
                return null;
            }

            // Version output may be on the first line if multi-line
            var lines = versionString.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                versionString = lines[0].Trim();
            }

            // Try to parse the version string (may have a 'v' prefix like "v1.2.3")
            if (versionString.StartsWith('v') || versionString.StartsWith('V'))
            {
                versionString = versionString[1..];
            }

            if (SemVersion.TryParse(versionString, SemVersionStyles.Any, out var version))
            {
                logger.LogDebug("Found GitHub Copilot CLI version: {Version}", version);
                return version;
            }

            logger.LogDebug("Could not parse GitHub Copilot CLI version from output: {Output}", output.Trim());
            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogDebug(ex, "GitHub Copilot CLI is not installed or not found in PATH");
            return null;
        }
    }

    /// <summary>
    /// Checks if the current process is running in a VSCode terminal.
    /// </summary>
    /// <returns>True if running in VSCode, false otherwise.</returns>
    private static bool IsRunningInVSCode()
    {
        // VSCode sets various environment variables when running a terminal
        // Check for any of these to detect if we're in a VSCode terminal
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_INJECTION")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_IPC_HOOK")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_GIT_ASKPASS_NODE")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_GIT_ASKPASS_EXTRA_ARGS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_GIT_ASKPASS_MAIN")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_GIT_IPC_HANDLE")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TERM_PROGRAM")) && 
                   Environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode";
    }
}
