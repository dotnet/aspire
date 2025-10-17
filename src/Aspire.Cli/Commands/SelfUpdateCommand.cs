// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class SelfUpdateCommand : BaseCommand
{
    private readonly ILogger<SelfUpdateCommand> _logger;
    private readonly ICliDownloader _cliDownloader;

    public SelfUpdateCommand(
        ILogger<SelfUpdateCommand> logger,
        ICliDownloader cliDownloader,
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext)
        : base("update", "Updates the Aspire CLI to the latest version", features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cliDownloader);

        _logger = logger;
        _cliDownloader = cliDownloader;

        var qualityOption = new Option<string>("--quality");
        qualityOption.Description = "Quality level to update to (release, staging, dev)";
        qualityOption.DefaultValueFactory = (result) => "release";
        Options.Add(qualityOption);
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var quality = parseResult.GetValue<string>("--quality") ?? "release";

        try
        {
            // Get current executable path
            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                InteractionService.DisplayError("Unable to determine the current executable path.");
                return ExitCodeConstants.InvalidCommand;
            }

            InteractionService.DisplayMessage(":package:", $"Current CLI location: {currentExePath}");
            InteractionService.DisplayMessage(":arrow_up:", $"Updating to quality level: {quality}");

            // Download the latest CLI
            var archivePath = await _cliDownloader.DownloadLatestCliAsync(quality, cancellationToken);

            // Extract and update
            await ExtractAndUpdateAsync(currentExePath, archivePath, cancellationToken);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update CLI");
            InteractionService.DisplayError($"Failed to update CLI: {ex.Message}");
            return ExitCodeConstants.InvalidCommand;
        }
    }

    private async Task ExtractAndUpdateAsync(string currentExePath, string archivePath, CancellationToken cancellationToken)
    {
        var installDir = Path.GetDirectoryName(currentExePath);
        if (string.IsNullOrEmpty(installDir))
        {
            throw new InvalidOperationException("Unable to determine installation directory.");
        }

        var exeName = Path.GetFileName(currentExePath);
        var tempExtractDir = Path.Combine(Path.GetTempPath(), $"aspire-cli-extract-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempExtractDir);

            // Extract archive
            InteractionService.DisplayMessage(":package:", "Extracting new CLI...");
            await ExtractArchiveAsync(archivePath, tempExtractDir, cancellationToken);

            // Find the aspire executable in the extracted files
            var newExePath = Path.Combine(tempExtractDir, exeName);
            if (!File.Exists(newExePath))
            {
                throw new FileNotFoundException($"Extracted CLI executable not found: {newExePath}");
            }

            // Backup current executable
            var backupPath = $"{currentExePath}.old";
            InteractionService.DisplayMessage(":floppy_disk:", "Backing up current CLI...");
            _logger.LogDebug("Creating backup: {BackupPath}", backupPath);

            // Remove old backup if it exists
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            // Rename current executable to .old
            File.Move(currentExePath, backupPath);

            try
            {
                // Copy new executable to install location
                InteractionService.DisplayMessage(":wrench:", "Installing new CLI...");
                File.Copy(newExePath, currentExePath, overwrite: true);

                // On Unix systems, ensure the executable bit is set
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetExecutablePermission(currentExePath);
                }

                // Test the new executable and display its version
                _logger.LogDebug("Testing new CLI executable and displaying version");
                var newVersion = await GetNewVersionAsync(currentExePath, cancellationToken);
                if (newVersion is null)
                {
                    throw new InvalidOperationException("New CLI executable failed verification test.");
                }

                // If we get here, the update was successful, remove the backup
                _logger.LogDebug("Update successful, removing backup");
                File.Delete(backupPath);
            }
            catch
            {
                // If anything goes wrong, restore the backup
                _logger.LogWarning("Update failed, restoring backup");
                if (File.Exists(backupPath))
                {
                    if (File.Exists(currentExePath))
                    {
                        File.Delete(currentExePath);
                    }
                    File.Move(backupPath, currentExePath);
                }
                throw;
            }
        }
        finally
        {
            // Clean up temp directories
            CleanupDirectory(tempExtractDir);
            CleanupDirectory(Path.GetDirectoryName(archivePath)!);
        }
    }

    private static async Task ExtractArchiveAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(archivePath).ToLowerInvariant();

        if (extension == ".zip" || archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destinationPath, overwriteFiles: true);
        }
        else if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            await using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gzipStream, destinationPath, overwriteFiles: true, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"Unsupported archive format: {archivePath}");
        }
    }

    private static void SetExecutablePermission(string filePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var mode = File.GetUnixFileMode(filePath);
                mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                File.SetUnixFileMode(filePath, mode);
            }
            catch
            {
                // Best effort, ignore failures
            }
        }
    }

    private async Task<string?> GetNewVersionAsync(string exePath, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            
            if (process.ExitCode == 0)
            {
                var version = output.Trim();
                InteractionService.DisplaySuccess($"Updated to version: {version}");
                return version;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private void CleanupDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up directory {Directory}", directory);
        }
    }
}
