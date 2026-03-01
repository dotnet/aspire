// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Aspire.Cli.Utils;

/// <summary>
/// Shared utilities for CLI update operations used by both interactive self-update and background auto-update.
/// </summary>
internal static class CliUpdateHelper
{
    /// <summary>
    /// Gets the platform-appropriate executable name for the Aspire CLI.
    /// </summary>
    public static string ExeName => OperatingSystem.IsWindows() ? "aspire.exe" : "aspire";

    /// <summary>
    /// Detects the current platform's OS and architecture for download purposes.
    /// </summary>
    public static (string Os, string Arch) DetectPlatform()
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

    /// <summary>
    /// Checks whether the CLI is running as a dotnet tool (vs a native binary).
    /// </summary>
    public static bool IsRunningAsDotNetTool()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Replaces the current CLI executable with a new one using the backup-and-swap pattern.
    /// Handles Windows locked-file workaround by renaming the running exe to .old.{timestamp}.
    /// </summary>
    /// <returns>The path to the backup file, or null if no backup was needed.</returns>
    public static string? ReplaceExecutable(string currentExePath, string newExePath)
    {
        string? backupPath = null;

        // Backup current executable if it exists
        if (File.Exists(currentExePath))
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            backupPath = $"{currentExePath}.old.{timestamp}";
            File.Move(currentExePath, backupPath);
        }

        try
        {
            File.Copy(newExePath, currentExePath, overwrite: true);

            if (!OperatingSystem.IsWindows())
            {
                SetExecutablePermission(currentExePath);
            }
        }
        catch
        {
            // Rollback — restore backup
            if (backupPath is not null)
            {
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
            }
            throw;
        }

        return backupPath;
    }

    /// <summary>
    /// Sets executable permissions on a file (Unix only).
    /// </summary>
    public static void SetExecutablePermission(string filePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            var mode = File.GetUnixFileMode(filePath);
            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(filePath, mode);
        }
    }

    /// <summary>
    /// Removes old backup files matching the pattern {exeName}.old.* in the same directory.
    /// </summary>
    public static void CleanupOldBackupFiles(string targetExePath)
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

    /// <summary>
    /// Downloads a file from a URL to a local path with a timeout.
    /// </summary>
    public static async Task DownloadFileAsync(HttpClient httpClient, string url, string outputPath, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cts.Token);
    }

    /// <summary>
    /// Validates a downloaded file against a SHA-512 checksum file.
    /// </summary>
    public static async Task ValidateChecksumAsync(string archivePath, string checksumPath, CancellationToken cancellationToken = default)
    {
        var expectedChecksum = (await File.ReadAllTextAsync(checksumPath, cancellationToken)).Trim().ToLowerInvariant();

        using var sha512 = SHA512.Create();
        await using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hashBytes = await sha512.ComputeHashAsync(fileStream, cancellationToken);
        var actualChecksum = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (expectedChecksum != actualChecksum)
        {
            throw new InvalidOperationException($"Checksum validation failed. Expected: {expectedChecksum}, Actual: {actualChecksum}");
        }
    }

    /// <summary>
    /// Builds the download URLs for a CLI archive and its checksum.
    /// </summary>
    public static (string ArchiveUrl, string ChecksumUrl, string ArchiveFilename) GetDownloadUrls(string baseUrl)
    {
        var (os, arch) = DetectPlatform();
        var rid = $"{os}-{arch}";
        var ext = os == "win" ? "zip" : "tar.gz";
        var archiveFilename = $"aspire-cli-{rid}.{ext}";
        var checksumFilename = $"{archiveFilename}.sha512";
        return ($"{baseUrl}/{archiveFilename}", $"{baseUrl}/{checksumFilename}", archiveFilename);
    }

    private static string DetectOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
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
}
