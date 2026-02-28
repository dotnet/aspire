// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.GitHub;

/// <summary>
/// Runs GitHub CLI (gh) commands by invoking the <c>gh</c> executable.
/// </summary>
internal sealed class GitHubCliRunner(ILogger<GitHubCliRunner> logger) : IGitHubCliRunner
{
    public async Task<bool> IsInstalledAsync(CancellationToken cancellationToken)
    {
        var path = PathLookupHelper.FindFullPathFromPath("gh");
        if (path is null)
        {
            logger.LogDebug("GitHub CLI (gh) is not installed or not found in PATH.");
            return false;
        }

        return await Task.FromResult(true).ConfigureAwait(false);
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken)
    {
        // Use 'auth token' instead of 'auth status' because 'auth status' returns non-zero
        // if any account (including inactive ones) has an invalid token.
        var (exitCode, _) = await RunAsync(["auth", "token"], cancellationToken).ConfigureAwait(false);
        return exitCode == 0;
    }

    public async Task<string?> GetUsernameAsync(CancellationToken cancellationToken)
    {
        var (exitCode, output) = await RunAsync(["api", "user", "--jq", ".login"], cancellationToken).ConfigureAwait(false);

        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            logger.LogDebug("Failed to get GitHub username (exit code {ExitCode}).", exitCode);
            return null;
        }

        return output.Trim();
    }

    public async Task<IReadOnlyList<string>> GetOrganizationsAsync(CancellationToken cancellationToken)
    {
        var (exitCode, output) = await RunAsync(
            ["api", "user/orgs", "--jq", ".[].login"],
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            logger.LogDebug("Failed to get GitHub organizations (exit code {ExitCode}).", exitCode);
            return [];
        }

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task<bool> RepoExistsAsync(string owner, string repo, CancellationToken cancellationToken)
    {
        var (exitCode, _) = await RunAsync(
            ["repo", "view", $"{owner}/{repo}", "--json", "name"],
            cancellationToken).ConfigureAwait(false);

        return exitCode == 0;
    }

    private async Task<(int ExitCode, string Output)> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var executablePath = PathLookupHelper.FindFullPathFromPath("gh");
        if (executablePath is null)
        {
            return (-1, string.Empty);
        }

        try
        {
            var startInfo = new ProcessStartInfo(executablePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var errorOutput = await errorTask.ConfigureAwait(false);
                logger.LogDebug("gh {Args} returned exit code {ExitCode}: {Error}",
                    string.Join(' ', args), process.ExitCode, errorOutput.Trim());
            }

            var output = await outputTask.ConfigureAwait(false);
            return (process.ExitCode, output);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogDebug(ex, "Failed to run gh {Args}.", string.Join(' ', args));
            return (-1, string.Empty);
        }
    }
}
