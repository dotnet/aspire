// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Git;

/// <summary>
/// Interface for executing Git commands.
/// </summary>
internal interface IGitCliRunner
{
    /// <summary>
    /// Finds the root directory of the Git repository containing the specified directory.
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root directory of the Git repository, or null if not found or Git is not available.</returns>
    Task<DirectoryInfo?> FindGitRootAsync(DirectoryInfo startDirectory, CancellationToken cancellationToken);
}

/// <summary>
/// Implementation for executing Git commands.
/// </summary>
internal sealed class GitCliRunner(ILogger<GitCliRunner> logger) : IGitCliRunner
{
    /// <inheritdoc/>
    public async Task<DirectoryInfo?> FindGitRootAsync(DirectoryInfo startDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                WorkingDirectory = startDirectory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    outputBuilder.AppendLine(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    errorBuilder.AppendLine(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                var gitRoot = outputBuilder.ToString().Trim();
                if (!string.IsNullOrEmpty(gitRoot) && Directory.Exists(gitRoot))
                {
                    logger.LogDebug("Found Git repository root at: {GitRoot}", gitRoot);
                    return new DirectoryInfo(gitRoot);
                }
            }
            else
            {
                logger.LogDebug("Git command failed with exit code {ExitCode}: {Error}", 
                    process.ExitCode, errorBuilder.ToString().Trim());
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Git not available or other error - this is not critical, just log and return null
            logger.LogDebug(ex, "Failed to execute git command, Git may not be installed or available in PATH");
        }

        return null;
    }
}
