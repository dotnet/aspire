// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Layout;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for creating AppHostServerProject instances with required dependencies.
/// </summary>
internal interface IAppHostServerProjectFactory
{
    IAppHostServerProject Create(string appPath);
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
    public IAppHostServerProject Create(string appPath)
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

        // Priority 2: Check if we have a bundle layout with a pre-built AppHost server
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
