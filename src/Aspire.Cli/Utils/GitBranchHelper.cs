// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Utils;

/// <summary>
/// Resolves the current git branch for a given file path.
/// </summary>
internal static class GitBranchHelper
{
    /// <summary>
    /// Gets the current git branch for the directory containing the specified file path.
    /// </summary>
    /// <param name="filePath">A file path whose parent directory is used as the git working directory.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The branch name, or <c>null</c> if git is unavailable or the path is not in a git repository.</returns>
    public static async Task<string?> GetCurrentBranchAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                return null;
            }

            var branch = output.Trim();
            return string.IsNullOrEmpty(branch) ? null : branch;
        }
        catch
        {
            return null;
        }
    }
}
