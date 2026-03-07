// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Handles downloading the Aspire CLI.
/// </summary>
internal interface ICliDownloader
{
    Task<string> DownloadLatestCliAsync(string channelName, CancellationToken cancellationToken);
}

internal class CliDownloader(
    ILogger<CliDownloader> logger,
    IInteractionService interactionService,
    IPackagingService packagingService) : ICliDownloader
{
    private const int ArchiveDownloadTimeoutSeconds = 600;
    private const int ChecksumDownloadTimeoutSeconds = 120;
    
    private static readonly HttpClient s_httpClient = new();

    public async Task<string> DownloadLatestCliAsync(string channelName, CancellationToken cancellationToken)
    {
        // Get the channel information from PackagingService
        var channels = await packagingService.GetChannelsAsync(cancellationToken);
        var channel = channels.FirstOrDefault(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
        
        if (channel is null)
        {
            throw new ArgumentException($"Unsupported channel '{channelName}'. Available channels: {string.Join(", ", channels.Select(c => c.Name))}");
        }

        if (string.IsNullOrEmpty(channel.CliDownloadBaseUrl))
        {
            throw new InvalidOperationException($"Channel '{channelName}' does not support CLI downloads.");
        }

        var baseUrl = channel.CliDownloadBaseUrl;

        var (archiveUrl, checksumUrl, archiveFilename) = CliUpdateHelper.GetDownloadUrls(baseUrl);

        // Create temp directory for download
        var tempDir = Directory.CreateTempSubdirectory("aspire-cli-download").FullName;

        try
        {
            var archivePath = Path.Combine(tempDir, archiveFilename);
            var checksumPath = Path.Combine(tempDir, $"{archiveFilename}.sha512");

            // Download archive
            _ = await interactionService.ShowStatusAsync($"Downloading Aspire CLI from: {archiveUrl}", async () =>
            {
                logger.LogDebug("Downloading archive from {Url} to {Path}", archiveUrl, archivePath);
                await CliUpdateHelper.DownloadFileAsync(s_httpClient, archiveUrl, archivePath, ArchiveDownloadTimeoutSeconds, cancellationToken);

                // Download checksum
                logger.LogDebug("Downloading checksum from {Url} to {Path}", checksumUrl, checksumPath);
                await CliUpdateHelper.DownloadFileAsync(s_httpClient, checksumUrl, checksumPath, ChecksumDownloadTimeoutSeconds, cancellationToken);
                
                return 0; // Return dummy value for ShowStatusAsync
            });

            // Validate checksum
            interactionService.DisplayMessage(KnownEmojis.CheckMark, "Validating downloaded file...");
            await CliUpdateHelper.ValidateChecksumAsync(archivePath, checksumPath, cancellationToken);

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

}
