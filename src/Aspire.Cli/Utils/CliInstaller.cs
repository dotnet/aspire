// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Result of a CLI installation operation.
/// </summary>
internal sealed record CliInstallResult
{
    public bool Success { get; init; }
    public string? Version { get; init; }
    public string? ErrorMessage { get; init; }
    public string? BackupPath { get; init; }

    public static CliInstallResult Succeeded(string version) => new() { Success = true, Version = version };
    public static CliInstallResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Shared utility for installing CLI updates. Used by both UpdateCommand (interactive) and AutoUpdater (background).
/// </summary>
internal interface ICliInstaller
{
    /// <summary>
    /// Installs a new CLI from an archive file.
    /// </summary>
    /// <param name="archivePath">Path to the downloaded archive (zip or tar.gz).</param>
    /// <param name="cleanupBackups">Whether to clean up old backup files after successful install.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure and the new version if successful.</returns>
    Task<CliInstallResult> InstallFromArchiveAsync(string archivePath, bool cleanupBackups, CancellationToken cancellationToken);
}

internal sealed class CliInstaller(ILogger<CliInstaller> logger) : ICliInstaller
{
    public async Task<CliInstallResult> InstallFromArchiveAsync(string archivePath, bool cleanupBackups, CancellationToken cancellationToken)
    {
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath))
        {
            return CliInstallResult.Failed("Unable to determine current CLI location.");
        }

        var installDir = Path.GetDirectoryName(currentExePath);
        if (string.IsNullOrEmpty(installDir))
        {
            return CliInstallResult.Failed($"Unable to determine installation directory from: {currentExePath}");
        }

        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aspire.exe" : "aspire";
        var targetExePath = Path.Combine(installDir, exeName);
        var tempExtractDir = Directory.CreateTempSubdirectory("aspire-cli-extract").FullName;
        string? backupPath = null;

        try
        {
            // Extract archive
            logger.LogDebug("Extracting archive {ArchivePath} to {TempDir}", archivePath, tempExtractDir);
            await ExtractArchiveAsync(archivePath, tempExtractDir, cancellationToken);

            // Find the aspire executable in the extracted files
            var newExePath = Path.Combine(tempExtractDir, exeName);
            if (!File.Exists(newExePath))
            {
                return CliInstallResult.Failed($"Extracted CLI executable not found: {newExePath}");
            }

            // Verify the new executable works BEFORE replacing the current one
            logger.LogDebug("Verifying new executable in temp location");
            var tempVersion = await GetExecutableVersionAsync(newExePath, cancellationToken);
            if (tempVersion is null)
            {
                return CliInstallResult.Failed("New CLI executable failed verification test.");
            }

            // Clean up old backup files before creating a new one
            if (cleanupBackups)
            {
                CleanupOldBackupFiles(targetExePath);
            }

            // Backup current executable if it exists
            if (File.Exists(targetExePath))
            {
                var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                backupPath = $"{targetExePath}.old.{unixTimestamp}";
                logger.LogDebug("Creating backup: {BackupPath}", backupPath);
                File.Move(targetExePath, backupPath);
            }

            try
            {
                // Copy new executable to install location
                logger.LogDebug("Installing new CLI to {InstallDir}", installDir);
                File.Copy(newExePath, targetExePath, overwrite: true);

                // On Unix systems, ensure the executable bit is set
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetExecutablePermission(targetExePath);
                }

                // Verify the installed executable works
                logger.LogDebug("Verifying installed executable");
                var installedVersion = await GetExecutableVersionAsync(targetExePath, cancellationToken);
                if (installedVersion is null)
                {
                    throw new InvalidOperationException("Installed CLI executable failed verification.");
                }

                return CliInstallResult.Succeeded(installedVersion) with { BackupPath = backupPath };
            }
            catch
            {
                // Restore backup on failure using atomic move with overwrite
                logger.LogWarning("Installation failed, restoring backup");
                if (backupPath is not null && File.Exists(backupPath))
                {
                    try
                    {
                        File.Move(backupPath, targetExePath, overwrite: true);
                    }
                    catch (Exception restoreEx)
                    {
                        logger.LogError(restoreEx, "Failed to restore backup from {BackupPath}", backupPath);
                    }
                }
                throw;
            }
        }
        finally
        {
            // Clean up temp directory
            CleanupDirectory(tempExtractDir);
        }
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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            var mode = File.GetUnixFileMode(filePath);
            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(filePath, mode);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to set executable permission on {FilePath}", filePath);
        }
    }

    private static async Task<string?> GetExecutableVersionAsync(string exePath, CancellationToken cancellationToken)
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
                return output.Trim();
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
                    logger.LogDebug("Deleted old backup file: {BackupFile}", backupFile);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to delete old backup file: {BackupFile}", backupFile);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to cleanup old backup files for: {TargetExePath}", targetExePath);
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
            logger.LogDebug(ex, "Failed to clean up directory {Directory}", directory);
        }
    }
}
