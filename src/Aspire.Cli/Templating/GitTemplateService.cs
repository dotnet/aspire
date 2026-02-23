// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating;

internal sealed class GitTemplateService(
    IInteractionService interactionService,
    ILogger<GitTemplateService> logger) : IGitTemplateService
{
    public async Task<int> ApplyGitTemplateAsync(string templatePathOrUrl, string destinationPath, CancellationToken cancellationToken)
    {
        if (IsRemoteUrl(templatePathOrUrl))
        {
            return await ApplyRemoteTemplateAsync(templatePathOrUrl, destinationPath, cancellationToken);
        }

        return ApplyLocalTemplate(templatePathOrUrl, destinationPath);
    }

    private static bool IsRemoteUrl(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("git@", StringComparison.OrdinalIgnoreCase);
    }

    private int ApplyLocalTemplate(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            interactionService.DisplayError($"Template path does not exist: '{sourcePath}'");
            return ExitCodeConstants.FailedToCreateNewProject;
        }

        logger.LogDebug("Copying template from local path: {SourcePath}", sourcePath);

        CopyDirectory(sourcePath, destinationPath);

        interactionService.DisplaySuccess($"Template applied to '{destinationPath}'");
        return ExitCodeConstants.Success;
    }

    private async Task<int> ApplyRemoteTemplateAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire-git-template-{Guid.NewGuid():N}");

        try
        {
            logger.LogDebug("Cloning template from URL: {Url}", url);

            var exitCode = await GitCloneAsync(url, tempDir, cancellationToken);
            if (exitCode != 0)
            {
                interactionService.DisplayError($"Failed to clone template repository: '{url}'");
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            CopyDirectory(tempDir, destinationPath);

            interactionService.DisplaySuccess($"Template applied to '{destinationPath}'");
            return ExitCodeConstants.Success;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to clean up temp directory: {TempDir}", tempDir);
                }
            }
        }
    }

    private static async Task<int> GitCloneAsync(string url, string targetDir, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo("git", ["clone", "--depth", "1", url, targetDir])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = Process.Start(psi);
        if (process is null)
        {
            return 1;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);

            // Skip .git directories at any level
            if (dirName.Equals(".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destDir = Path.Combine(destinationDir, dirName);
            CopyDirectory(dir, destDir);
        }
    }
}
