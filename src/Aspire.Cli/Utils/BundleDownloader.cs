// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Handles downloading and updating the Aspire Bundle.
/// </summary>
internal interface IBundleDownloader
{
    /// <summary>
    /// Downloads the latest bundle version.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the downloaded bundle archive.</returns>
    Task<string> DownloadLatestBundleAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the latest available bundle version.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest version string.</returns>
    Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether a bundle update is available.
    /// </summary>
    /// <param name="currentVersion">Current bundle version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if an update is available.</returns>
    Task<bool> IsUpdateAvailableAsync(string currentVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a downloaded bundle update to the specified installation directory.
    /// Handles file locking by staging updates and using atomic swaps where possible.
    /// </summary>
    /// <param name="archivePath">Path to the downloaded bundle archive.</param>
    /// <param name="installPath">Target installation directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or if a restart is required.</returns>
    Task<BundleUpdateResult> ApplyUpdateAsync(string archivePath, string installPath, CancellationToken cancellationToken);
}

/// <summary>
/// Result of applying a bundle update.
/// </summary>
internal sealed class BundleUpdateResult
{
    /// <summary>
    /// Whether the update was successfully applied.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Whether a restart is required to complete the update.
    /// </summary>
    public bool RestartRequired { get; init; }

    /// <summary>
    /// Path to a script that should be run to complete the update (Windows only).
    /// </summary>
    public string? PendingUpdateScript { get; init; }

    /// <summary>
    /// Error message if the update failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The new version that was installed.
    /// </summary>
    public string? InstalledVersion { get; init; }

    public static BundleUpdateResult Succeeded(string version) => new()
    {
        Success = true,
        InstalledVersion = version
    };

    public static BundleUpdateResult RequiresRestart(string scriptPath) => new()
    {
        Success = true,
        RestartRequired = true,
        PendingUpdateScript = scriptPath
    };

    public static BundleUpdateResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

internal sealed class BundleDownloader : IBundleDownloader
{
    private const string GitHubRepo = "dotnet/aspire";
    private const string GitHubReleasesApi = $"https://api.github.com/repos/{GitHubRepo}/releases";
    private const int DownloadTimeoutSeconds = 600;
    private const int ApiTimeoutSeconds = 30;
    private const string PendingUpdateDir = ".pending-update";
    private const string BackupDir = ".backup";

    private static readonly HttpClient s_httpClient = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "aspire-bundle-updater/1.0" },
            { "Accept", "application/vnd.github+json" }
        }
    };

    private readonly ILogger<BundleDownloader> _logger;
    private readonly IInteractionService _interactionService;

    public BundleDownloader(
        ILogger<BundleDownloader> logger,
        IInteractionService interactionService)
    {
        _logger = logger;
        _interactionService = interactionService;
    }

    public async Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(ApiTimeoutSeconds));

            var response = await s_httpClient.GetStringAsync($"{GitHubReleasesApi}/latest", cts.Token);
            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("tag_name", out var tagName))
            {
                var version = tagName.GetString();
                // Remove 'v' prefix if present
                if (version?.StartsWith("v", StringComparison.OrdinalIgnoreCase) == true)
                {
                    version = version[1..];
                }
                return version;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get latest version from GitHub");
            return null;
        }
    }

    public async Task<bool> IsUpdateAvailableAsync(string currentVersion, CancellationToken cancellationToken)
    {
        var latestVersion = await GetLatestVersionAsync(cancellationToken);
        if (string.IsNullOrEmpty(latestVersion))
        {
            return false;
        }

        // Try to parse as semver and compare
        if (Version.TryParse(NormalizeVersion(currentVersion), out var current) &&
            Version.TryParse(NormalizeVersion(latestVersion), out var latest))
        {
            return latest > current;
        }

        // Fall back to string comparison
        return !string.Equals(currentVersion, latestVersion, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> DownloadLatestBundleAsync(CancellationToken cancellationToken)
    {
        var version = await GetLatestVersionAsync(cancellationToken);
        if (string.IsNullOrEmpty(version))
        {
            throw new InvalidOperationException("Failed to determine latest bundle version");
        }

        var rid = GetRuntimeIdentifier();
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "zip" : "tar.gz";
        var filename = $"aspire-bundle-{version}-{rid}.{extension}";
        var downloadUrl = $"https://github.com/{GitHubRepo}/releases/download/v{version}/{filename}";

        _logger.LogDebug("Downloading bundle from {Url}", downloadUrl);

        // Create temp directory
        var tempDir = Directory.CreateTempSubdirectory("aspire-bundle-download").FullName;
        var archivePath = Path.Combine(tempDir, filename);

        try
        {
            await _interactionService.ShowStatusAsync($"Downloading Aspire Bundle v{version}...", async () =>
            {
                const int maxRetries = 3;
                for (var attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromSeconds(DownloadTimeoutSeconds));

                        using var response = await s_httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                        response.EnsureSuccessStatusCode();

                        await using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
                        await using var fileStream = new FileStream(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await contentStream.CopyToAsync(fileStream, cts.Token);

                        return 0;
                    }
                    catch (HttpRequestException) when (attempt < maxRetries)
                    {
                        _logger.LogDebug("Download attempt {Attempt} failed, retrying...", attempt);
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                    }
                }

                return 0;
            });

            // Try to download and validate checksum
            var checksumUrl = $"{downloadUrl}.sha512";

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                var checksumContent = await s_httpClient.GetStringAsync(checksumUrl, cts.Token);
                await ValidateChecksumAsync(archivePath, checksumContent, cancellationToken);
                _interactionService.DisplayMessage("check_mark", "Checksum validated");
            }
            catch (HttpRequestException)
            {
                // Checksum file may not exist for all releases
                _logger.LogDebug("Checksum file not available, skipping validation");
            }

            return archivePath;
        }
        catch
        {
            // Clean up temp directory on failure
            CleanupDirectory(tempDir);
            throw;
        }
    }

    public async Task<BundleUpdateResult> ApplyUpdateAsync(string archivePath, string installPath, CancellationToken cancellationToken)
    {
        var stagingPath = Path.Combine(installPath, PendingUpdateDir);
        var backupPath = Path.Combine(installPath, BackupDir);

        try
        {
            // Step 1: Extract to staging directory
            _interactionService.DisplayMessage("package", "Extracting update...");
            CleanupDirectory(stagingPath);
            Directory.CreateDirectory(stagingPath);

            await ArchiveHelper.ExtractAsync(archivePath, stagingPath, cancellationToken);

            // Read version from extracted layout.json
            var version = await ReadVersionFromLayoutAsync(stagingPath);

            // Step 2: Try atomic swap approach first
            if (await TryAtomicSwapAsync(installPath, stagingPath, backupPath))
            {
                _interactionService.DisplaySuccess($"Updated to version {version ?? "unknown"}");
                CleanupDirectory(backupPath);
                return BundleUpdateResult.Succeeded(version ?? "unknown");
            }

            // Step 3: If atomic swap fails (files locked), try incremental update
            var lockedFiles = await TryIncrementalUpdateAsync(installPath, stagingPath);

            if (lockedFiles.Count == 0)
            {
                _interactionService.DisplaySuccess($"Updated to version {version ?? "unknown"}");
                CleanupDirectory(stagingPath);
                return BundleUpdateResult.Succeeded(version ?? "unknown");
            }

            // Step 4: If files are locked (Windows), create a pending update script
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var scriptPath = CreatePendingUpdateScript(installPath, stagingPath, lockedFiles);
                _interactionService.DisplayMessage("warning", "Some files are in use. Update will complete on next restart.");
                _interactionService.DisplayMessage("information", $"Or run: {scriptPath}");
                return BundleUpdateResult.RequiresRestart(scriptPath);
            }

            // On Unix, locked files are less common but handle gracefully
            _interactionService.DisplayMessage("warning", $"Could not update {lockedFiles.Count} locked files. Please close Aspire and try again.");
            return BundleUpdateResult.Failed($"Files locked: {string.Join(", ", lockedFiles.Take(5))}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply bundle update");
            return BundleUpdateResult.Failed(ex.Message);
        }
    }

    private Task<bool> TryAtomicSwapAsync(string installPath, string stagingPath, string backupPath)
    {
        // On Unix, we can try to do an atomic directory swap using rename
        // On Windows, this typically fails if any files are in use

        try
        {
            CleanupDirectory(backupPath);

            // Get list of items to move (excluding staging and backup dirs)
            var itemsToBackup = Directory.EnumerateFileSystemEntries(installPath)
                .Where(p => !p.EndsWith(PendingUpdateDir) && !p.EndsWith(BackupDir))
                .ToList();

            if (itemsToBackup.Count == 0)
            {
                // Fresh install, just move staging contents
                foreach (var item in Directory.EnumerateFileSystemEntries(stagingPath))
                {
                    var destPath = Path.Combine(installPath, Path.GetFileName(item));
                    if (Directory.Exists(item))
                    {
                        Directory.Move(item, destPath);
                    }
                    else
                    {
                        File.Move(item, destPath);
                    }
                }
                return Task.FromResult(true);
            }

            // Create backup directory
            Directory.CreateDirectory(backupPath);

            // Try to move all existing items to backup
            foreach (var item in itemsToBackup)
            {
                var destPath = Path.Combine(backupPath, Path.GetFileName(item));
                if (Directory.Exists(item))
                {
                    Directory.Move(item, destPath);
                }
                else
                {
                    File.Move(item, destPath);
                }
            }

            // Move staged items to install location
            foreach (var item in Directory.EnumerateFileSystemEntries(stagingPath))
            {
                var destPath = Path.Combine(installPath, Path.GetFileName(item));
                if (Directory.Exists(item))
                {
                    Directory.Move(item, destPath);
                }
                else
                {
                    File.Move(item, destPath);
                }
            }

            return Task.FromResult(true);
        }
        catch (IOException ex) when (IsFileLockedException(ex))
        {
            _logger.LogDebug(ex, "Atomic swap failed due to locked files, falling back to incremental update");

            // Restore from backup if partial swap occurred
            if (Directory.Exists(backupPath))
            {
                foreach (var item in Directory.EnumerateFileSystemEntries(backupPath))
                {
                    var destPath = Path.Combine(installPath, Path.GetFileName(item));
                    if (!File.Exists(destPath) && !Directory.Exists(destPath))
                    {
                        try
                        {
                            if (Directory.Exists(item))
                            {
                                Directory.Move(item, destPath);
                            }
                            else
                            {
                                File.Move(item, destPath);
                            }
                        }
                        catch
                        {
                            // Best effort restore
                        }
                    }
                }
            }

            return Task.FromResult(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogDebug(ex, "Atomic swap failed due to permission issues");
            return Task.FromResult(false);
        }
    }

    private Task<List<string>> TryIncrementalUpdateAsync(string installPath, string stagingPath)
    {
        var lockedFiles = new List<string>();

        foreach (var sourceFile in Directory.EnumerateFiles(stagingPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(stagingPath, sourceFile);

            // Validate no path traversal sequences to prevent writing outside install directory
            if (relativePath.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            {
                _logger.LogWarning("Skipping file with suspicious path: {Path}", relativePath);
                continue;
            }

            var destFile = Path.Combine(installPath, relativePath);

            // Additional safety: ensure destination is within install path
            var normalizedDest = Path.GetFullPath(destFile);
            var normalizedInstall = Path.GetFullPath(installPath);
            if (!normalizedDest.StartsWith(normalizedInstall, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Skipping file that would escape install directory: {Path}", relativePath);
                continue;
            }

            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Try to update the file with retry logic
            var updated = FileAccessRetrier.TryFileOperation(() =>
            {
                if (File.Exists(destFile))
                {
                    // Try rename-move-delete pattern (works even for running executables on Unix)
                    // Use GUID for unique backup filename to avoid collisions
                    var backupFile = $"{destFile}.old.{Guid.NewGuid():N}";
                    FileAccessRetrier.RetryOnFileAccessFailure(() =>
                    {
                        // Handle case where backup file already exists (shouldn't happen with GUID, but be safe)
                        if (File.Exists(backupFile))
                        {
                            FileAccessRetrier.SafeDeleteFile(backupFile);
                        }
                        File.Move(destFile, backupFile);
                    }, maxRetries: 3);

                    try
                    {
                        File.Move(sourceFile, destFile);
                        // Clean up backup
                        FileAccessRetrier.SafeDeleteFile(backupFile);
                    }
                    catch
                    {
                        // Restore backup on failure
                        if (File.Exists(backupFile) && !File.Exists(destFile))
                        {
                            File.Move(backupFile, destFile);
                        }
                        throw;
                    }
                }
                else
                {
                    File.Move(sourceFile, destFile);
                }
            });

            if (updated)
            {
                // Set executable permissions on Unix
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetExecutablePermissionIfNeeded(destFile);
                }
            }
            else
            {
                _logger.LogDebug("File locked, will update later: {File}", relativePath);
                lockedFiles.Add(relativePath);
            }
        }

        return Task.FromResult(lockedFiles);
    }

    private static string CreatePendingUpdateScript(string installPath, string stagingPath, List<string> lockedFiles)
    {
        var scriptPath = Path.Combine(installPath, "complete-update.cmd");

        var script = $"""
            @echo off
            echo Completing Aspire Bundle update...
            echo Waiting for locked files to be released...
            
            REM Wait a moment for processes to exit
            timeout /t 2 /nobreak > nul
            
            REM Try to copy locked files
            """;

        foreach (var file in lockedFiles)
        {
            // Skip files with path traversal sequences (silently - this is a static method)
            if (file.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(file))
            {
                continue;
            }

            var sourceFile = Path.Combine(stagingPath, file);
            var destFile = Path.Combine(installPath, file);
            script += $"""
            
            copy /Y "{sourceFile}" "{destFile}" > nul 2>&1
            if errorlevel 1 (
                echo Failed to update: {file}
            ) else (
                echo Updated: {file}
            )
            """;
        }

        script += $"""
            
            REM Cleanup staging directory
            rmdir /S /Q "{stagingPath}" > nul 2>&1
            
            echo.
            echo Update complete. You can delete this script.
            del "%~f0"
            """;

        File.WriteAllText(scriptPath, script);
        return scriptPath;
    }

    private static bool IsFileLockedException(IOException ex)
    {
        // Check for common file-in-use error codes
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;

        var hResult = ex.HResult & 0xFFFF;
        return hResult == ERROR_SHARING_VIOLATION || hResult == ERROR_LOCK_VIOLATION;
    }

    private async Task<string?> ReadVersionFromLayoutAsync(string path)
    {
        var layoutJsonPath = Path.Combine(path, "layout.json");
        if (!File.Exists(layoutJsonPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(layoutJsonPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("version", out var versionProp))
            {
                return versionProp.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read version from layout.json");
        }

        return null;
    }

    private static string GetRuntimeIdentifier()
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
            : "linux";

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}")
        };

        return $"{os}-{arch}";
    }

    private static string NormalizeVersion(string version)
    {
        // Remove prerelease suffixes for comparison
        var dashIndex = version.IndexOf('-');
        if (dashIndex > 0)
        {
            version = version[..dashIndex];
        }

        // Ensure we have at least major.minor.patch
        var parts = version.Split('.');
        return parts.Length switch
        {
            1 => $"{parts[0]}.0.0",
            2 => $"{parts[0]}.{parts[1]}.0",
            _ => version
        };
    }

    private async Task ValidateChecksumAsync(string archivePath, string checksumContent, CancellationToken cancellationToken)
    {
        var expectedChecksum = checksumContent
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim()
            .ToUpperInvariant();

        await using var stream = File.OpenRead(archivePath);
        var actualHash = await SHA512.HashDataAsync(stream, cancellationToken);
        var actualChecksum = Convert.ToHexString(actualHash);

        if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Checksum validation failed. Expected: {expectedChecksum}, Actual: {actualChecksum}");
        }

        _logger.LogDebug("Checksum validation passed");
    }

    private void SetExecutablePermissionIfNeeded(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Set executable bit for known executables
        var fileName = Path.GetFileName(filePath);
        if (fileName == "aspire" || fileName == "dotnet" || fileName.EndsWith(".sh"))
        {
            try
            {
                var mode = File.GetUnixFileMode(filePath);
                mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                File.SetUnixFileMode(filePath, mode);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to set executable permission on {FilePath}", filePath);
            }
        }
    }

    private void CleanupDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to cleanup directory {Path}", path);
        }
    }
}
