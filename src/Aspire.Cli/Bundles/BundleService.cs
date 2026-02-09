// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using Aspire.Cli.Layout;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Bundles;

/// <summary>
/// Manages extraction of the embedded bundle payload from self-extracting CLI binaries.
/// </summary>
internal sealed class BundleService(ILayoutDiscovery layoutDiscovery, ILogger<BundleService> logger) : IBundleService
{
    /// <summary>
    /// Well-known layout subdirectories that are cleaned before re-extraction.
    /// The bin/ directory is intentionally excluded since it contains the running CLI binary.
    /// </summary>
    internal static readonly string[] s_layoutDirectories = [
        BundleDiscovery.RuntimeDirectoryName,
        BundleDiscovery.DashboardDirectoryName,
        BundleDiscovery.DcpDirectoryName,
        BundleDiscovery.AppHostServerDirectoryName,
        "tools"
    ];

    /// <inheritdoc/>
    public async Task EnsureExtractedAsync(CancellationToken cancellationToken = default)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            logger.LogDebug("ProcessPath is null or empty, skipping bundle extraction.");
            return;
        }

        var extractDir = GetDefaultExtractDir(processPath);
        if (extractDir is null)
        {
            logger.LogDebug("Could not determine extraction directory from {ProcessPath}, skipping.", processPath);
            return;
        }

        logger.LogDebug("Ensuring bundle is extracted from {ProcessPath} to {ExtractDir}.", processPath, extractDir);
        var result = await ExtractAsync(processPath, extractDir, force: false, cancellationToken);

        if (result is BundleExtractResult.ExtractionFailed)
        {
            throw new InvalidOperationException(
                "Bundle extraction failed. Run 'aspire setup --force' to retry, or reinstall the Aspire CLI.");
        }
    }

    /// <inheritdoc/>
    public async Task<BundleExtractResult> ExtractAsync(string binaryPath, string destinationPath, bool force = false, CancellationToken cancellationToken = default)
    {
        var trailer = BundleTrailer.TryRead(binaryPath);
        if (trailer is null)
        {
            logger.LogDebug("No bundle trailer found in {BinaryPath}.", binaryPath);
            return BundleExtractResult.NoPayload;
        }

        logger.LogDebug("Bundle trailer found: PayloadOffset={Offset}, PayloadSize={Size}, VersionHash={Hash}.",
            trailer.PayloadOffset, trailer.PayloadSize, trailer.VersionHash);

        // Use a file lock for cross-process synchronization
        logger.LogDebug("Acquiring bundle extraction lock for {Path}...", destinationPath);
        using var fileLock = FileLock.Acquire(destinationPath, ".aspire-bundle-lock");
        logger.LogDebug("Bundle extraction lock acquired.");

        try
        {
            // Re-check after acquiring lock — another process may have already extracted
            if (!force && layoutDiscovery.DiscoverLayout() is not null)
            {
                var existingHash = BundleTrailer.ReadVersionMarker(destinationPath);
                if (existingHash == trailer.VersionHash)
                {
                    logger.LogDebug("Bundle already extracted and up to date (hash: {Hash}).", existingHash);
                    return BundleExtractResult.AlreadyUpToDate;
                }

                logger.LogDebug("Version mismatch: existing={ExistingHash}, bundle={BundleHash}. Re-extracting.", existingHash, trailer.VersionHash);
            }

            return await ExtractCoreAsync(binaryPath, destinationPath, trailer, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract bundle to {Path}", destinationPath);
            return BundleExtractResult.ExtractionFailed;
        }
    }

    private async Task<BundleExtractResult> ExtractCoreAsync(string binaryPath, string destinationPath, BundleTrailerInfo trailer, CancellationToken cancellationToken)
    {
        logger.LogInformation("Extracting embedded bundle to {Path}...", destinationPath);

        // Clean existing layout directories before extraction to avoid file conflicts
        logger.LogDebug("Cleaning existing layout directories in {Path}.", destinationPath);
        CleanLayoutDirectories(destinationPath);

        var sw = Stopwatch.StartNew();
        await ExtractPayloadAsync(binaryPath, trailer, destinationPath, cancellationToken);
        sw.Stop();
        logger.LogDebug("Payload extraction completed in {ElapsedMs}ms.", sw.ElapsedMilliseconds);

        // Write version marker so subsequent runs skip extraction
        BundleTrailer.WriteVersionMarker(destinationPath, trailer.VersionHash);
        logger.LogDebug("Version marker written (hash: {Hash}).", trailer.VersionHash);

        // Verify extraction produced a valid layout
        if (layoutDiscovery.DiscoverLayout() is null)
        {
            logger.LogError("Extraction completed but no valid layout found in {Path}.", destinationPath);
            return BundleExtractResult.ExtractionFailed;
        }

        logger.LogDebug("Bundle extraction verified successfully.");
        return BundleExtractResult.Extracted;
    }

    /// <summary>
    /// Determines the default extraction directory for the current CLI binary.
    /// If CLI is at ~/.aspire/bin/aspire, returns ~/.aspire/ so layout discovery
    /// finds components via the bin/ layout pattern.
    /// </summary>
    internal static string? GetDefaultExtractDir(string processPath)
    {
        var cliDir = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(cliDir))
        {
            return null;
        }

        return Path.GetDirectoryName(cliDir) ?? cliDir;
    }

    /// <summary>
    /// Removes well-known layout subdirectories before re-extraction.
    /// Preserves the bin/ directory (which contains the CLI binary itself).
    /// </summary>
    internal static void CleanLayoutDirectories(string layoutPath)
    {
        foreach (var dir in s_layoutDirectories)
        {
            var fullPath = Path.Combine(layoutPath, dir);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive: true);
            }
        }

        // Remove version marker so it's rewritten after extraction
        var markerPath = Path.Combine(layoutPath, BundleTrailer.VersionMarkerFileName);
        if (File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }
    }

    /// <summary>
    /// Extracts the embedded tar.gz payload from the CLI binary to the specified directory.
    /// Uses system tar on Unix (more robust with platform-specific archives) and .NET TarReader on Windows.
    /// </summary>
    internal static async Task ExtractPayloadAsync(string binaryPath, BundleTrailerInfo trailer, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationPath);

        if (OperatingSystem.IsWindows())
        {
            await ExtractPayloadWithTarReaderAsync(binaryPath, trailer, destinationPath, cancellationToken);
        }
        else
        {
            await ExtractPayloadWithSystemTarAsync(binaryPath, trailer, destinationPath, cancellationToken);
        }
    }

    /// <summary>
    /// Extracts using the system tar command (Unix). More robust than .NET TarReader
    /// for archives created by macOS tar which may contain extended attribute headers.
    /// </summary>
    private static async Task ExtractPayloadWithSystemTarAsync(string binaryPath, BundleTrailerInfo trailer, string destinationPath, CancellationToken cancellationToken)
    {
        var tempArchive = Path.GetTempFileName();
        try
        {
            using (var payloadStream = BundleTrailer.OpenPayload(binaryPath, trailer))
            await using (var tempFile = File.Create(tempArchive))
            {
                await payloadStream.CopyToAsync(tempFile, cancellationToken);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "tar",
                RedirectStandardError = true,
                UseShellExecute = false
            };

            psi.ArgumentList.Add("-xzf");
            psi.ArgumentList.Add(tempArchive);
            psi.ArgumentList.Add("--strip-components=1");
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(destinationPath);

            using var process = Process.Start(psi);
            if (process is null)
            {
                throw new InvalidOperationException("Failed to start tar process for bundle extraction.");
            }

            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to extract bundle (exit code {process.ExitCode}): {stderr}");
            }
        }
        finally
        {
            File.Delete(tempArchive);
        }
    }

    /// <summary>
    /// Extracts using .NET TarReader (Windows, or fallback).
    /// </summary>
    private static async Task ExtractPayloadWithTarReaderAsync(string binaryPath, BundleTrailerInfo trailer, string destinationPath, CancellationToken cancellationToken)
    {
        using var payloadStream = BundleTrailer.OpenPayload(binaryPath, trailer);
        await using var gzipStream = new GZipStream(payloadStream, CompressionMode.Decompress);
        await using var tarReader = new TarReader(gzipStream);

        while (await tarReader.GetNextEntryAsync(cancellationToken: cancellationToken) is { } entry)
        {
            // Strip the top-level directory (equivalent to tar --strip-components=1)
            var name = entry.Name;
            var slashIndex = name.IndexOf('/');
            if (slashIndex < 0)
            {
                continue; // Top-level directory entry itself, skip
            }

            var relativePath = name[(slashIndex + 1)..];
            if (string.IsNullOrEmpty(relativePath))
            {
                continue;
            }

            var fullPath = Path.Combine(destinationPath, relativePath);

            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(fullPath);
                    break;

                case TarEntryType.RegularFile:
                    var dir = Path.GetDirectoryName(fullPath);
                    if (dir is not null)
                    {
                        Directory.CreateDirectory(dir);
                    }
                    await entry.ExtractToFileAsync(fullPath, overwrite: true, cancellationToken);
                    break;

                case TarEntryType.SymbolicLink:
                    var linkDir = Path.GetDirectoryName(fullPath);
                    if (linkDir is not null)
                    {
                        Directory.CreateDirectory(linkDir);
                    }
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                    File.CreateSymbolicLink(fullPath, entry.LinkName);
                    break;
            }
        }
    }
}
