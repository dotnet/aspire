// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Aspire.Cli.Configuration;

namespace Aspire.Cli.Utils;

/// <summary>
/// Handles automatic background updates of the Aspire CLI.
/// </summary>
internal static class AutoUpdater
{
    private const string StagingDirectoryName = "staging";
    private const string VersionFileName = "version.txt";
    private const int DownloadTimeoutSeconds = 600;
    private const int ChecksumTimeoutSeconds = 120;

    /// <summary>
    /// Set when a staged update was applied at startup.
    /// Read by BaseCommand to show the auto-update notification.
    /// </summary>
    public static string? AppliedVersion { get; set; }

    /// <summary>
    /// Gets the path to the staging directory (~/.aspire/staging/).
    /// </summary>
    public static string GetStagingDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".aspire", StagingDirectoryName);
    }

    /// <summary>
    /// Checks whether auto-update should run.
    /// </summary>
    public static bool ShouldAutoUpdate(
        ICliHostEnvironment hostEnvironment,
        IFeatures features,
        CliExecutionContext executionContext)
    {
        // Skip if feature disabled via 'aspire config set features.autoUpdateEnabled false'
        if (!features.IsFeatureEnabled(KnownFeatures.AutoUpdateEnabled, true))
        {
            return false;
        }

        // Skip in CI environments
        if (hostEnvironment.IsRunningInCI)
        {
            return false;
        }

        // Skip if disabled via env var: ASPIRE_CLI_AUTO_UPDATE=false
        var autoUpdateEnv = executionContext.GetEnvironmentVariable("ASPIRE_CLI_AUTO_UPDATE");
        if (string.Equals(autoUpdateEnv, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Skip for dotnet tool installs (use 'dotnet tool update' instead)
        if (IsRunningAsDotNetTool())
        {
            return false;
        }

        // Skip if staging already has a pending update
        if (HasStagedUpdate())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Spawns a detached background process to download and stage the CLI update.
    /// </summary>
    public static void SpawnBackgroundDownload(string cliDownloadBaseUrl, string? version)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return;
        }

        try
        {
            var arguments = version is not null
                ? $"internal-auto-update {cliDownloadBaseUrl} {version}"
                : $"internal-auto-update {cliDownloadBaseUrl}";

            var psi = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            Process.Start(psi);
            // Don't wait — fire and forget
        }
        catch
        {
            // Silently ignore spawn failures
        }
    }

    /// <summary>
    /// Downloads the CLI archive and stages it for the next startup.
    /// This runs in a standalone background process with no DI.
    /// </summary>
    public static async Task<int> SilentDownloadAndStageAsync(string baseUrl, string? version)
    {
        try
        {
            var (os, arch) = DetectPlatform();
            var rid = $"{os}-{arch}";
            var ext = os == "win" ? "zip" : "tar.gz";
            var archiveFilename = $"aspire-cli-{rid}.{ext}";
            var checksumFilename = $"{archiveFilename}.sha512";
            var archiveUrl = $"{baseUrl}/{archiveFilename}";
            var checksumUrl = $"{baseUrl}/{checksumFilename}";

            var stagingDir = GetStagingDirectory();
            var exeName = OperatingSystem.IsWindows() ? "aspire.exe" : "aspire";

            // If staging already has an update, skip
            if (File.Exists(Path.Combine(stagingDir, exeName)))
            {
                return 0;
            }

            var tempDir = Directory.CreateTempSubdirectory("aspire-auto-update").FullName;

            try
            {
                var archivePath = Path.Combine(tempDir, archiveFilename);
                var checksumPath = Path.Combine(tempDir, checksumFilename);

                // Download archive and checksum
                using var httpClient = new HttpClient();

                await DownloadFileAsync(httpClient, archiveUrl, archivePath, DownloadTimeoutSeconds);
                await DownloadFileAsync(httpClient, checksumUrl, checksumPath, ChecksumTimeoutSeconds);

                // Validate checksum
                await ValidateChecksumAsync(archivePath, checksumPath);

                // Extract archive
                var extractDir = Path.Combine(tempDir, "extracted");
                await ArchiveHelper.ExtractAsync(archivePath, extractDir, CancellationToken.None);

                // Find the CLI exe in extracted files
                var newExePath = Path.Combine(extractDir, exeName);
                if (!File.Exists(newExePath))
                {
                    return 1;
                }

                // Stage the new binary
                Directory.CreateDirectory(stagingDir);
                File.Copy(newExePath, Path.Combine(stagingDir, exeName), overwrite: true);

                // Write version marker
                if (version is not null)
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(stagingDir, VersionFileName),
                        version);
                }

                return 0;
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// Checks for and applies a staged update. Called very early in Program.Main before DI setup.
    /// Uses the rename-to-.old trick for Windows locked file handling.
    /// </summary>
    /// <returns>The version that was applied, or null if no update was staged.</returns>
    public static string? TryApplyStagedUpdate()
    {
        try
        {
            var stagingDir = GetStagingDirectory();
            var exeName = OperatingSystem.IsWindows() ? "aspire.exe" : "aspire";
            var stagedExePath = Path.Combine(stagingDir, exeName);

            if (!File.Exists(stagedExePath))
            {
                return null;
            }

            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                return null;
            }

            // Read version before we move files
            string? version = null;
            var versionFile = Path.Combine(stagingDir, VersionFileName);
            if (File.Exists(versionFile))
            {
                version = File.ReadAllText(versionFile).Trim();
            }

            // Rename current exe to .old.{timestamp} (Windows allows renaming a running exe)
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var backupPath = $"{currentExePath}.old.{timestamp}";

            try
            {
                File.Move(currentExePath, backupPath);
            }
            catch
            {
                // Can't rename current exe (maybe another process has it locked) — abort
                return null;
            }

            try
            {
                // Copy staged exe to current location
                File.Copy(stagedExePath, currentExePath, overwrite: true);

                // On Unix, set executable permissions
                if (!OperatingSystem.IsWindows())
                {
                    var mode = File.GetUnixFileMode(currentExePath);
                    mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                    File.SetUnixFileMode(currentExePath, mode);
                }
            }
            catch
            {
                // Rollback — restore backup
                try
                {
                    if (File.Exists(currentExePath))
                    {
                        File.Delete(currentExePath);
                    }
                    File.Move(backupPath, currentExePath);
                }
                catch
                {
                    // Critical failure — both files may be in an inconsistent state
                }
                return null;
            }

            // Clean up staging directory
            try
            {
                Directory.Delete(stagingDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }

            // Clean up old backup files (best-effort — they may still be locked by other instances)
            CleanupOldBackupFiles(currentExePath);

            return version;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if there is a staged update ready to apply.
    /// </summary>
    public static bool HasStagedUpdate()
    {
        var stagingDir = GetStagingDirectory();
        var exeName = OperatingSystem.IsWindows() ? "aspire.exe" : "aspire";
        return File.Exists(Path.Combine(stagingDir, exeName));
    }

    private static bool IsRunningAsDotNetTool()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static void CleanupOldBackupFiles(string targetExePath)
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

            foreach (var backupFile in Directory.GetFiles(directory, searchPattern))
            {
                try
                {
                    File.Delete(backupFile);
                }
                catch
                {
                    // Ignore — file may still be locked by another running instance
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static (string os, string arch) DetectPlatform()
    {
        var os = DetectOperatingSystem();
        var arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.ProcessArchitecture}")
        };
        return (os, arch);
    }

    private static string DetectOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Check for musl-based systems (Alpine, etc.)
            try
            {
                var lddPath = "/usr/bin/ldd";
                if (File.Exists(lddPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = lddPath,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    using var process = Process.Start(psi);
                    if (process is not null)
                    {
                        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (output.Contains("musl", StringComparison.OrdinalIgnoreCase))
                        {
                            return "linux-musl";
                        }
                    }
                }
            }
            catch
            {
                // Fall back to regular linux
            }
            return "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }
        else
        {
            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }

    private static async Task DownloadFileAsync(HttpClient httpClient, string url, string outputPath, int timeoutSeconds)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cts.Token);
    }

    private static async Task ValidateChecksumAsync(string archivePath, string checksumPath)
    {
        var expectedChecksum = (await File.ReadAllTextAsync(checksumPath)).Trim().ToLowerInvariant();

        using var sha512 = SHA512.Create();
        await using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hashBytes = await sha512.ComputeHashAsync(fileStream);
        var actualChecksum = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (expectedChecksum != actualChecksum)
        {
            throw new InvalidOperationException($"Checksum validation failed. Expected: {expectedChecksum}, Actual: {actualChecksum}");
        }
    }
}
