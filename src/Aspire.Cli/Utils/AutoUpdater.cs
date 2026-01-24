// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Aspire.Cli.Configuration;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Hosting;
using Aspire.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Handles automatic background updates of the CLI.
/// </summary>
internal interface IAutoUpdater
{
    /// <summary>
    /// Starts a background update check and install if an update is available.
    /// This is fire-and-forget - if the CLI exits before update completes, no harm.
    /// </summary>
    /// <param name="args">Command line arguments to check if running update --self.</param>
    void StartBackgroundUpdate(string[] args);
}

internal sealed class AutoUpdater(
    ILogger<AutoUpdater> logger,
    IConfiguration configuration,
    IConfigurationService configurationService,
    IPackagingService packagingService,
    INuGetPackageCache nuGetPackageCache,
    ICliInstaller cliInstaller,
    CliExecutionContext executionContext,
    TimeProvider timeProvider) : IAutoUpdater
{
    private const string AutoUpdateMutexName = "AspireCliAutoUpdate";
    private const string LastAutoUpdateCheckKey = "lastAutoUpdateCheck";
    private static readonly TimeSpan s_stableThrottleDuration = TimeSpan.FromHours(24);

    private static readonly HttpClient s_httpClient = new();

    public void StartBackgroundUpdate(string[] args)
    {
        // Fire and forget - don't await
        _ = TryUpdateInBackgroundAsync(args);
    }

    private async Task TryUpdateInBackgroundAsync(string[] args)
    {
        try
        {
            // Check if running `aspire update --self` - skip auto-update to avoid conflicts
            if (IsUpdateSelfCommand(args))
            {
                logger.LogDebug("Auto-update skipped: running update --self command");
                return;
            }

            // Check if auto-update is disabled via environment variable
            if (IsAutoUpdateDisabled())
            {
                logger.LogDebug("Auto-update is disabled via environment variable");
                return;
            }

            // Check if running as dotnet tool (can't self-update)
            if (IsRunningAsDotNetTool())
            {
                logger.LogDebug("Auto-update skipped: running as dotnet tool");
                return;
            }

            // Check if this is a PR or hive build (shouldn't auto-update)
            if (IsPrOrHiveBuild())
            {
                logger.LogDebug("Auto-update skipped: PR or hive build detected");
                return;
            }

            // Get the configured channel
            var channel = await GetConfiguredChannelAsync();
            if (channel is null)
            {
                logger.LogDebug("Auto-update skipped: no channel configured");
                return;
            }

            // Check throttle for stable channel
            if (!await ShouldCheckForUpdateAsync(channel))
            {
                logger.LogDebug("Auto-update skipped: throttled for channel {Channel}", channel);
                return;
            }

            // Try to acquire mutex - if already held by another process, skip
            using var mutex = new Mutex(false, AutoUpdateMutexName, out _);
            bool acquired;
            try
            {
                acquired = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // Another process held the mutex but crashed - we now own it
                acquired = true;
            }

            if (!acquired)
            {
                logger.LogDebug("Auto-update skipped: another update is in progress");
                return;
            }

            try
            {
                await PerformUpdateAsync(channel, CancellationToken.None);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex)
        {
            // Silent failure - log and continue
            logger.LogDebug(ex, "Auto-update failed silently");
        }
    }

    private bool IsAutoUpdateDisabled()
    {
        var disabled = configuration[KnownConfigNames.CliAutoUpdateDisabled];
        return string.Equals(disabled, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(disabled, "1", StringComparison.Ordinal);
    }

    private static bool IsUpdateSelfCommand(string[] args)
    {
        // Check if args contain "update" and "--self"
        var hasUpdate = args.Any(a => string.Equals(a, "update", StringComparison.OrdinalIgnoreCase));
        var hasSelf = args.Any(a => string.Equals(a, "--self", StringComparison.OrdinalIgnoreCase));
        return hasUpdate && hasSelf;
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

    private bool IsPrOrHiveBuild()
    {
        // Check if there are PR hives on this machine - if so, this is likely a dev machine
        // running a PR build and shouldn't auto-update
        var prHiveCount = executionContext.GetPrHiveCount();
        if (prHiveCount > 0)
        {
            logger.LogDebug("Detected {PrHiveCount} PR hives - assuming PR/dev build", prHiveCount);
            return true;
        }

        // Check if the current executable is in a hive directory
        var currentExePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(currentExePath))
        {
            var hivesPath = executionContext.HivesDirectory.FullName;
            if (currentExePath.StartsWith(hivesPath, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Current executable is in hives directory - assuming hive build");
                return true;
            }
        }

        return false;
    }

    private async Task<string?> GetConfiguredChannelAsync()
    {
        // Get channel from global settings
        var channel = await configurationService.GetConfigurationAsync("channel", CancellationToken.None);
        
        // Default to stable if no channel is configured
        return string.IsNullOrEmpty(channel) ? PackageChannelNames.Stable : channel;
    }

    private async Task<bool> ShouldCheckForUpdateAsync(string channel)
    {
        // Daily and staging channels: always check (no throttle)
        if (string.Equals(channel, PackageChannelNames.Daily, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channel, PackageChannelNames.Staging, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Stable channel: check once per 24 hours
        var lastCheckKey = $"{LastAutoUpdateCheckKey}.{channel}";
        var lastCheckStr = await configurationService.GetConfigurationAsync(lastCheckKey, CancellationToken.None);
        
        if (string.IsNullOrEmpty(lastCheckStr))
        {
            return true;
        }

        if (DateTimeOffset.TryParse(lastCheckStr, out var lastCheck))
        {
            var now = timeProvider.GetUtcNow();
            var elapsed = now - lastCheck;
            
            if (elapsed < s_stableThrottleDuration)
            {
                logger.LogDebug("Last update check was {Elapsed} ago, throttle duration is {ThrottleDuration}", elapsed, s_stableThrottleDuration);
                return false;
            }
        }

        return true;
    }

    private async Task PerformUpdateAsync(string channelName, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking for auto-update on channel {Channel}", channelName);

        // Record the check time (for throttling)
        var lastCheckKey = $"{LastAutoUpdateCheckKey}.{channelName}";
        await configurationService.SetConfigurationAsync(lastCheckKey, timeProvider.GetUtcNow().ToString("O"), isGlobal: true, cancellationToken);

        // Check if an update is available
        var currentVersion = PackageUpdateHelpers.GetCurrentPackageVersion();
        if (currentVersion is null)
        {
            logger.LogDebug("Unable to determine current CLI version");
            return;
        }

        var availablePackages = await nuGetPackageCache.GetCliPackagesAsync(
            workingDirectory: executionContext.WorkingDirectory,
            prerelease: true,
            nugetConfigFile: null,
            cancellationToken: cancellationToken);

        var newerVersion = PackageUpdateHelpers.GetNewerVersion(logger, currentVersion, availablePackages);
        if (newerVersion is null)
        {
            logger.LogDebug("No newer version available (current: {CurrentVersion})", currentVersion);
            return;
        }

        logger.LogDebug("Newer version available: {NewerVersion} (current: {CurrentVersion})", newerVersion, currentVersion);

        // Get channel info for download URL
        var channels = await packagingService.GetChannelsAsync(cancellationToken);
        var channel = channels.FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        
        if (channel is null || string.IsNullOrEmpty(channel.CliDownloadBaseUrl))
        {
            logger.LogDebug("Channel {ChannelName} does not support CLI downloads", channelName);
            return;
        }

        // Download and install
        var archivePath = await DownloadCliArchiveAsync(channel.CliDownloadBaseUrl, cancellationToken);
        if (archivePath is null)
        {
            return;
        }

        try
        {
            // Use shared installer - don't clean up backups for auto-update (keep safety net)
            var result = await cliInstaller.InstallFromArchiveAsync(archivePath, cleanupBackups: false, cancellationToken);
            
            if (result.Success)
            {
                logger.LogDebug("Auto-update completed successfully to version {Version}", result.Version);
            }
            else
            {
                logger.LogDebug("Auto-update failed: {ErrorMessage}", result.ErrorMessage);
            }
        }
        finally
        {
            // Clean up downloaded archive
            try
            {
                var archiveDir = Path.GetDirectoryName(archivePath);
                if (archiveDir is not null && Directory.Exists(archiveDir))
                {
                    Directory.Delete(archiveDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to clean up archive directory");
            }
        }
    }

    private async Task<string?> DownloadCliArchiveAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var (os, arch) = CliPlatformDetector.DetectPlatform();
        var runtimeIdentifier = $"{os}-{arch}";
        var extension = os == "win" ? "zip" : "tar.gz";
        var archiveFilename = $"aspire-cli-{runtimeIdentifier}.{extension}";
        var checksumFilename = $"{archiveFilename}.sha512";
        var archiveUrl = $"{baseUrl}/{archiveFilename}";
        var checksumUrl = $"{baseUrl}/{checksumFilename}";

        var tempDir = Directory.CreateTempSubdirectory("aspire-cli-autoupdate").FullName;

        try
        {
            var archivePath = Path.Combine(tempDir, archiveFilename);
            var checksumPath = Path.Combine(tempDir, checksumFilename);

            // Download archive and checksum
            logger.LogDebug("Downloading CLI from {Url}", archiveUrl);
            await DownloadFileAsync(archiveUrl, archivePath, cancellationToken);
            await DownloadFileAsync(checksumUrl, checksumPath, cancellationToken);

            // Validate checksum
            await ValidateChecksumAsync(archivePath, checksumPath, cancellationToken);

            return archivePath;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to download CLI archive");
            
            // Clean up temp directory on failure
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            return null;
        }
    }

    private static async Task DownloadFileAsync(string url, string outputPath, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMinutes(10));

        using var response = await s_httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cts.Token);
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
            throw new InvalidOperationException("Checksum validation failed");
        }
    }
}
