// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Handles downloading the Aspire CLI.
/// </summary>
internal interface ICliDownloader
{
    Task<string> DownloadLatestCliAsync(string quality, CancellationToken cancellationToken);
}

internal class CliDownloader(
    ILogger<CliDownloader> logger,
    IInteractionService interactionService) : ICliDownloader
{
    private const int ArchiveDownloadTimeoutSeconds = 600;
    private const int ChecksumDownloadTimeoutSeconds = 120;

    private static readonly Dictionary<string, string> s_qualityBaseUrls = new()
    {
        ["daily"] = "https://aka.ms/dotnet/9/aspire/daily",
        ["staging"] = "https://aka.ms/dotnet/9/aspire/rc/daily",
        ["stable"] = "https://aka.ms/dotnet/9/aspire/ga/daily"
    };

    public async Task<string> DownloadLatestCliAsync(string quality, CancellationToken cancellationToken)
    {
        if (!s_qualityBaseUrls.TryGetValue(quality, out var baseUrl))
        {
            throw new ArgumentException($"Unsupported quality '{quality}'. Supported values are: dev, staging, release.");
        }

        var (os, arch) = DetectPlatform();
        var runtimeIdentifier = $"{os}-{arch}";
        var extension = os == "win" ? "zip" : "tar.gz";
        var archiveFilename = $"aspire-cli-{runtimeIdentifier}.{extension}";
        var checksumFilename = $"{archiveFilename}.sha512";
        var archiveUrl = $"{baseUrl}/{archiveFilename}";
        var checksumUrl = $"{baseUrl}/{checksumFilename}";

        // Create temp directory for download
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire-cli-download-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var archivePath = Path.Combine(tempDir, archiveFilename);
            var checksumPath = Path.Combine(tempDir, checksumFilename);

            // Download archive
            interactionService.DisplayMessage(":down_arrow:", $"Downloading Aspire CLI from: {archiveUrl}");
            logger.LogDebug("Downloading archive from {Url} to {Path}", archiveUrl, archivePath);
            await DownloadFileAsync(archiveUrl, archivePath, ArchiveDownloadTimeoutSeconds, cancellationToken);

            // Download checksum
            logger.LogDebug("Downloading checksum from {Url} to {Path}", checksumUrl, checksumPath);
            await DownloadFileAsync(checksumUrl, checksumPath, ChecksumDownloadTimeoutSeconds, cancellationToken);

            // Validate checksum
            interactionService.DisplayMessage(":check_mark:", "Validating downloaded file...");
            await ValidateChecksumAsync(archivePath, checksumPath, cancellationToken);

            interactionService.DisplaySuccess("Download completed successfully");
            return archivePath;
        }
        catch
        {
            // Clean up temp directory on failure
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clean up temporary directory {TempDir}", tempDir);
            }
            throw;
        }
    }

    private static (string os, string arch) DetectPlatform()
    {
        var os = DetectOperatingSystem();
        var arch = DetectArchitecture();
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
            // Check if it's musl-based (Alpine, etc.)
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

    private static string DetectArchitecture()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        return arch switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {arch}")
        };
    }

    private static async Task DownloadFileAsync(string url, string outputPath, int timeoutSeconds, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cancellationToken);
    }

    private static async Task ValidateChecksumAsync(string archivePath, string checksumPath, CancellationToken cancellationToken)
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
}
