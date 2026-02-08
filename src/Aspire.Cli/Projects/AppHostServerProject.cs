// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Layout;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for creating AppHostServerProject instances with required dependencies.
/// </summary>
internal interface IAppHostServerProjectFactory
{
    Task<IAppHostServerProject> CreateAsync(string appPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory implementation that creates IAppHostServerProject instances.
/// Chooses between DotNetBasedAppHostServerProject (dev mode) and PrebuiltAppHostServer (bundle mode).
/// </summary>
internal sealed class AppHostServerProjectFactory(
    IDotNetCliRunner dotNetCliRunner,
    IPackagingService packagingService,
    IConfigurationService configurationService,
    ILayoutDiscovery layoutDiscovery,
    BundleNuGetService bundleNuGetService,
    ILoggerFactory loggerFactory) : IAppHostServerProjectFactory
{
    public async Task<IAppHostServerProject> CreateAsync(string appPath, CancellationToken cancellationToken = default)
    {
        // Normalize the path
        var normalizedPath = Path.GetFullPath(appPath);
        normalizedPath = new Uri(normalizedPath).LocalPath;
        normalizedPath = OperatingSystem.IsWindows() ? normalizedPath.ToLowerInvariant() : normalizedPath;

        // Generate socket path based on app path hash (deterministic for same project)
        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        var socketName = Convert.ToHexString(pathHash)[..12].ToLowerInvariant() + ".sock";

        string socketPath;
        if (OperatingSystem.IsWindows())
        {
            // Windows uses named pipes
            socketPath = socketName;
        }
        else
        {
            // Unix uses domain sockets
            var socketDir = Path.Combine(Path.GetTempPath(), ".aspire", "sockets");
            Directory.CreateDirectory(socketDir);
            socketPath = Path.Combine(socketDir, socketName);
        }

        // Priority 1: Check for dev mode (ASPIRE_REPO_ROOT or running from Aspire source repo)
        var repoRoot = DetectAspireRepoRoot();
        if (repoRoot is not null)
        {
            return new DotNetBasedAppHostServerProject(
                appPath,
                socketPath,
                repoRoot,
                dotNetCliRunner,
                packagingService,
                configurationService,
                loggerFactory.CreateLogger<DotNetBasedAppHostServerProject>());
        }

        // Priority 2: Ensure bundle is extracted if we have an embedded payload
        await EnsureBundleAsync(cancellationToken);

        // Priority 3: Check if we have a bundle layout with a pre-built AppHost server
        var layout = layoutDiscovery.DiscoverLayout();
        if (layout is not null && layout.GetAppHostServerPath() is string serverPath && File.Exists(serverPath))
        {
            return new PrebuiltAppHostServer(
                appPath,
                socketPath,
                layout,
                bundleNuGetService,
                packagingService,
                configurationService,
                loggerFactory.CreateLogger<PrebuiltAppHostServer>());
        }

        throw new InvalidOperationException(
            "No Aspire AppHost server is available. Either set the ASPIRE_REPO_ROOT environment variable " +
            "to the root of the Aspire repository for development, or ensure the Aspire CLI is installed " +
            "with a valid bundle layout.");
    }

    /// <summary>
    /// Extracts the embedded bundle payload if the CLI binary is a self-extracting bundle
    /// and no valid layout has been discovered yet.
    /// </summary>
    private async Task EnsureBundleAsync(CancellationToken cancellationToken)
    {
        // Check if the current process has an embedded bundle payload
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return;
        }

        var trailer = BundleTrailer.TryRead(processPath);
        if (trailer is null)
        {
            return; // No embedded payload (dev build or already-extracted CLI)
        }

        // Determine extraction directory: parent of the CLI binary's directory.
        // If CLI is at ~/.aspire/bin/aspire, extract to ~/.aspire/ so layout discovery
        // finds components via the bin/ layout pattern ({layout}/bin/aspire + {layout}/runtime/).
        var cliDir = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(cliDir))
        {
            return;
        }

        var extractDir = Path.GetDirectoryName(cliDir) ?? cliDir;

        // If layout exists and version matches, skip extraction
        if (layoutDiscovery.DiscoverLayout() is not null)
        {
            var existingHash = BundleTrailer.ReadVersionMarker(extractDir);
            if (existingHash == trailer.VersionHash)
            {
                return; // Already extracted with matching version
            }
        }

        var logger = loggerFactory.CreateLogger<AppHostServerProjectFactory>();
        logger.LogInformation("Extracting embedded bundle to {Path}...", extractDir);

        await ExtractPayloadAsync(processPath, trailer, extractDir, cancellationToken);

        // Write version marker so subsequent runs skip extraction
        BundleTrailer.WriteVersionMarker(extractDir, trailer.VersionHash);

        // Verify extraction succeeded
        if (layoutDiscovery.DiscoverLayout() is null)
        {
            logger.LogWarning("Bundle extraction completed but layout discovery still failed");
        }
    }

    /// <summary>
    /// Extracts the embedded tar.gz payload from the CLI binary to the specified directory.
    /// The tarball contains a top-level directory which is stripped during extraction.
    /// </summary>
    internal static async Task ExtractPayloadAsync(string processPath, BundleTrailerInfo trailer, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destinationPath);

        using var payloadStream = BundleTrailer.OpenPayload(processPath, trailer);
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

        // Set executable permissions on Unix for key binaries
        if (!OperatingSystem.IsWindows())
        {
            SetExecutablePermissions(destinationPath);
        }
    }

    /// <summary>
    /// Sets executable permissions on key binaries after extraction on Unix systems.
    /// </summary>
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    private static void SetExecutablePermissions(string layoutPath)
    {
        var muxerName = BundleDiscovery.GetDotNetExecutableName();
        string[] executablePaths =
        [
            Path.Combine(layoutPath, BundleDiscovery.RuntimeDirectoryName, muxerName),
            Path.Combine(layoutPath, BundleDiscovery.DcpDirectoryName, "dcp"),
        ];

        foreach (var path in executablePaths)
        {
            if (File.Exists(path))
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
    }

    /// <summary>
    /// Detects the Aspire repository root for dev mode.
    /// Checks ASPIRE_REPO_ROOT env var first, then walks up from the CLI executable
    /// looking for a git repo containing Aspire.slnx.
    /// </summary>
    private static string? DetectAspireRepoRoot()
    {
        // Check explicit environment variable
        var envRoot = Environment.GetEnvironmentVariable("ASPIRE_REPO_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot))
        {
            return envRoot;
        }

        // Auto-detect: walk up from the CLI executable looking for .git + Aspire.slnx
        var cliPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(cliPath))
        {
            return null;
        }

        var dir = Path.GetDirectoryName(cliPath);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")) &&
                File.Exists(Path.Combine(dir, "Aspire.slnx")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
