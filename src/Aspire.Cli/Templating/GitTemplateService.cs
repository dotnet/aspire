// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating;

internal sealed class GitTemplateService(
    IInteractionService interactionService,
    IDotNetCliRunner dotNetCliRunner,
    ILogger<GitTemplateService> logger) : IGitTemplateService
{
    private const string TemplateHostFileName = "templatehost.cs";

    public async Task<int> ApplyGitTemplateAsync(string templatePathOrUrl, string destinationPath, CancellationToken cancellationToken)
    {
        string templateDir;
        string? tempDir = null;

        if (IsRemoteUrl(templatePathOrUrl))
        {
            tempDir = Path.Combine(Path.GetTempPath(), $"aspire-git-template-{Guid.NewGuid():N}");

            logger.LogDebug("Cloning template from URL: {Url}", templatePathOrUrl);

            var cloneExitCode = await GitCloneAsync(templatePathOrUrl, tempDir, cancellationToken);
            if (cloneExitCode != 0)
            {
                interactionService.DisplayError($"Failed to clone template repository: '{templatePathOrUrl}'");
                CleanupTempDir(tempDir);
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            templateDir = tempDir;
        }
        else
        {
            if (!Directory.Exists(templatePathOrUrl))
            {
                interactionService.DisplayError($"Template path does not exist: '{templatePathOrUrl}'");
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            templateDir = templatePathOrUrl;
        }

        try
        {
            // Check for templatehost.cs — if found, launch as an apphost
            var templateHostFile = Path.Combine(templateDir, TemplateHostFileName);
            if (File.Exists(templateHostFile))
            {
                logger.LogDebug("Found {TemplateHostFile}, launching as apphost", TemplateHostFileName);
                return await LaunchTemplateHostAsync(templateHostFile, destinationPath, cancellationToken);
            }

            // No templatehost — just copy files
            logger.LogDebug("No {TemplateHostFile} found, copying files directly", TemplateHostFileName);
            CopyDirectory(templateDir, destinationPath);
            interactionService.DisplaySuccess($"Template applied to '{destinationPath}'");
            return ExitCodeConstants.Success;
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    private async Task<int> LaunchTemplateHostAsync(string templateHostFile, string destinationPath, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(templateHostFile);

        var env = new Dictionary<string, string>
        {
            ["ASPIRE_TEMPLATE_OUTPUT_PATH"] = destinationPath
        };

        var backchannelTcs = new TaskCompletionSource<IAppHostCliBackchannel>();
        var options = new DotNetCliRunnerInvocationOptions();

        logger.LogDebug("Launching template host: {TemplateHostFile}", templateHostFile);

        var exitCode = await dotNetCliRunner.RunAsync(
            projectFile: fileInfo,
            watch: false,
            noBuild: false,
            noRestore: false,
            args: [],
            env: env,
            backchannelCompletionSource: backchannelTcs,
            options: options,
            cancellationToken: cancellationToken);

        if (exitCode == ExitCodeConstants.Success)
        {
            interactionService.DisplaySuccess($"Template applied to '{destinationPath}'");
        }
        else
        {
            interactionService.DisplayError("Template host exited with an error.");
        }

        return exitCode;
    }

    private static bool IsRemoteUrl(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("git@", StringComparison.OrdinalIgnoreCase);
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
            // Skip the templatehost.cs file from the copy
            var fileName = Path.GetFileName(file);
            if (fileName.Equals(TemplateHostFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destFile = Path.Combine(destinationDir, fileName);
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

    private void CleanupTempDir(string? tempDir)
    {
        if (tempDir is not null && Directory.Exists(tempDir))
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
