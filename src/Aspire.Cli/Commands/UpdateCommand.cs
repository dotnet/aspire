// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class UpdateCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IPackagingService _packagingService;
    private readonly IProjectUpdater _projectUpdater;
    private readonly ILogger<UpdateCommand> _logger;
    private readonly ICliDownloader? _cliDownloader;
    private readonly ICliUpdateNotifier _updateNotifier;

    public UpdateCommand(
        IProjectLocator projectLocator, 
        IPackagingService packagingService, 
        IProjectUpdater projectUpdater, 
        ILogger<UpdateCommand> logger,
        ICliDownloader? cliDownloader,
        IInteractionService interactionService, 
        IFeatures features, 
        ICliUpdateNotifier updateNotifier, 
        CliExecutionContext executionContext) 
        : base("update", UpdateCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(projectUpdater);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(updateNotifier);

        _projectLocator = projectLocator;
        _packagingService = packagingService;
        _projectUpdater = projectUpdater;
        _logger = logger;
        _cliDownloader = cliDownloader;
        _updateNotifier = updateNotifier;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = UpdateCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        // Only add --self option if not running as dotnet tool
        if (!CliHostEnvironment.IsRunningAsDotNetTool())
        {
            var selfOption = new Option<bool>("--self");
            selfOption.Description = "Update the Aspire CLI itself to the latest version";
            Options.Add(selfOption);

            var qualityOption = new Option<string?>("--quality");
            qualityOption.Description = "Quality level to update to when using --self (stable, staging, daily)";
            Options.Add(qualityOption);
        }
    }

    protected override bool UpdateNotificationsEnabled => false;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var isSelfUpdate = parseResult.GetValue<bool>("--self");

        // If --self is specified, handle CLI self-update
        if (isSelfUpdate)
        {
            if (_cliDownloader is null)
            {
                InteractionService.DisplayError("CLI self-update is not available in this environment.");
                return ExitCodeConstants.InvalidCommand;
            }

            return await ExecuteSelfUpdateAsync(parseResult, cancellationToken);
        }

        // Otherwise, handle project update
        try
        {
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var projectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, createSettingsFile: true, cancellationToken);
            if (projectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var channels = await _packagingService.GetChannelsAsync(cancellationToken);

            var channel = await InteractionService.PromptForSelectionAsync(
                UpdateCommandStrings.SelectChannelPrompt,
                channels,
                (c) => $"{c.Name} ({c.SourceDetails})",
                cancellationToken);

            await _projectUpdater.UpdateProjectAsync(projectFile!, channel, cancellationToken);
            
            // After successful project update, check if CLI update is available and prompt
            if (_cliDownloader is not null && _updateNotifier.IsUpdateAvailable())
            {
                var shouldUpdateCli = await InteractionService.ConfirmAsync(
                    UpdateCommandStrings.UpdateCliAfterProjectUpdatePrompt,
                    defaultValue: true,
                    cancellationToken);
                
                if (shouldUpdateCli)
                {
                    // Use the same channel that was selected for the project update
                    return await ExecuteSelfUpdateAsync(parseResult, cancellationToken, channel.Name);
                }
            }
        }
        catch (ProjectUpdaterException ex)
        {
            var message = Markup.Escape(ex.Message);
            InteractionService.DisplayError(message);
            return ExitCodeConstants.FailedToUpgradeProject;
        }
        catch (ProjectLocatorException ex)
        {
            // Check if this is a "no project found" error and prompt for self-update
            if (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
            {
                // Only prompt for self-update if not running as dotnet tool and downloader is available
                if (_cliDownloader is not null)
                {
                    var shouldUpdateCli = await InteractionService.ConfirmAsync(
                        UpdateCommandStrings.NoAppHostFoundUpdateCliPrompt,
                        defaultValue: true,
                        cancellationToken);
                    
                    if (shouldUpdateCli)
                    {
                        return await ExecuteSelfUpdateAsync(parseResult, cancellationToken);
                    }
                }
            }
            
            return HandleProjectLocatorException(ex, InteractionService);
        }

        return 0;
    }

    private async Task<int> ExecuteSelfUpdateAsync(ParseResult parseResult, CancellationToken cancellationToken, string? selectedQuality = null)
    {
        var quality = selectedQuality ?? parseResult.GetValue<string?>("--quality");

        // If quality is not specified, prompt the user
        if (string.IsNullOrEmpty(quality))
        {
            var qualities = new[] { "stable", "staging", "daily" };
            quality = await InteractionService.PromptForSelectionAsync(
                "Select the quality level to update to:",
                qualities,
                q => q,
                cancellationToken);
        }

        try
        {
            // Get current executable path for display purposes only
            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                InteractionService.DisplayError("Unable to determine the current executable path.");
                return ExitCodeConstants.InvalidCommand;
            }

            InteractionService.DisplayMessage("package", $"Current CLI location: {currentExePath}");
            InteractionService.DisplayMessage("up_arrow", $"Updating to quality level: {quality}");

            // Download the latest CLI
            var archivePath = await _cliDownloader!.DownloadLatestCliAsync(quality, cancellationToken);

            // Extract and update to $HOME/.aspire/bin
            await ExtractAndUpdateAsync(archivePath, cancellationToken);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update CLI");
            InteractionService.DisplayError($"Failed to update CLI: {ex.Message}");
            return ExitCodeConstants.InvalidCommand;
        }
    }

    private async Task ExtractAndUpdateAsync(string archivePath, CancellationToken cancellationToken)
    {
        // Always install to $HOME/.aspire/bin
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(homeDir))
        {
            throw new InvalidOperationException("Unable to determine home directory.");
        }

        var installDir = Path.Combine(homeDir, ".aspire", "bin");
        Directory.CreateDirectory(installDir);

        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aspire.exe" : "aspire";
        var targetExePath = Path.Combine(installDir, exeName);
        var tempExtractDir = Directory.CreateTempSubdirectory("aspire-cli-extract").FullName;

        try
        {

            // Extract archive
            InteractionService.DisplayMessage("package", "Extracting new CLI...");
            await ExtractArchiveAsync(archivePath, tempExtractDir, cancellationToken);

            // Find the aspire executable in the extracted files
            var newExePath = Path.Combine(tempExtractDir, exeName);
            if (!File.Exists(newExePath))
            {
                throw new FileNotFoundException($"Extracted CLI executable not found: {newExePath}");
            }

            // Backup current executable if it exists
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var backupPath = $"{targetExePath}.old.{unixTimestamp}";
            if (File.Exists(targetExePath))
            {
                InteractionService.DisplayMessage("floppy_disk", "Backing up current CLI...");
                _logger.LogDebug("Creating backup: {BackupPath}", backupPath);

                // Clean up old backup files
                CleanupOldBackupFiles(targetExePath);

                // Rename current executable to .old.[timestamp]
                File.Move(targetExePath, backupPath);
            }

            try
            {
                // Copy new executable to install location
                InteractionService.DisplayMessage("wrench", $"Installing new CLI to {installDir}...");
                File.Copy(newExePath, targetExePath, overwrite: true);

                // On Unix systems, ensure the executable bit is set
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetExecutablePermission(targetExePath);
                }

                // Test the new executable and display its version
                _logger.LogDebug("Testing new CLI executable and displaying version");
                var newVersion = await GetNewVersionAsync(targetExePath, cancellationToken);
                if (newVersion is null)
                {
                    throw new InvalidOperationException("New CLI executable failed verification test.");
                }

                // If we get here, the update was successful, clean up old backups
                CleanupOldBackupFiles(targetExePath);

                // Display helpful message about PATH
                if (!IsInPath(installDir))
                {
                    InteractionService.DisplayMessage("information", $"Note: {installDir} is not in your PATH. Add it to use the updated CLI globally.");
                }
            }
            catch
            {
                // If anything goes wrong, restore the backup
                _logger.LogWarning("Update failed, restoring backup");
                if (File.Exists(backupPath))
                {
                    if (File.Exists(targetExePath))
                    {
                        File.Delete(targetExePath);
                    }
                    File.Move(backupPath, targetExePath);
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

    private static bool IsInPath(string directory)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return false;
        }

        var pathSeparator = Path.PathSeparator;
        var paths = pathEnv.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);
        
        return paths.Any(p => 
            string.Equals(Path.GetFullPath(p.Trim()), Path.GetFullPath(directory), 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                    ? StringComparison.OrdinalIgnoreCase 
                    : StringComparison.Ordinal));
    }

    private static async Task ExtractArchiveAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
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

    private void SetExecutablePermission(string filePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var mode = File.GetUnixFileMode(filePath);
                mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                File.SetUnixFileMode(filePath, mode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set executable permission on {FilePath}", filePath);
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

    internal void CleanupOldBackupFiles(string targetExePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(targetExePath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            var exeName = Path.GetFileName(targetExePath);
            var searchPattern = $"{exeName}.old.*";

            var oldBackupFiles = Directory.GetFiles(directory, searchPattern);
            foreach (var backupFile in oldBackupFiles)
            {
                try
                {
                    File.Delete(backupFile);
                    _logger.LogDebug("Deleted old backup file: {BackupFile}", backupFile);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to delete old backup file: {BackupFile}", backupFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to cleanup old backup files for: {TargetExePath}", targetExePath);
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
