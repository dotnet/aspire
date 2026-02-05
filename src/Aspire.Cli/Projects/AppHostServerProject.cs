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
/// Chooses between PrebuiltAppHostServer (bundle mode) and DotNetSdkBasedAppHostServerProject
/// based on layout availability.
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

        // Priority 1: Check if we have a bundle layout with a pre-built AppHost server
        var layout = layoutDiscovery.DiscoverLayout();
        if (layout is not null && layout.GetAppHostServerPath() is string serverPath && File.Exists(serverPath))
        {
            return new PrebuiltAppHostServer(
                appPath,
                socketPath,
                layout,
                bundleNuGetService,
                packagingService,
                loggerFactory.CreateLogger<PrebuiltAppHostServer>());
        }

        // Priority 2: Check for local Aspire repo development (ASPIRE_REPO_ROOT)
        var repoRoot = Environment.GetEnvironmentVariable("ASPIRE_REPO_ROOT");
        if (!string.IsNullOrEmpty(repoRoot) && Directory.Exists(repoRoot))
        {
            return new DevAppHostServerProject(
                appPath,
                socketPath,
                repoRoot,
                dotNetCliRunner,
                packagingService,
                configurationService,
                loggerFactory.CreateLogger<DevAppHostServerProject>());
        }

        // Priority 3: Fall back to SDK-based project (requires .NET SDK)
        return new DotNetSdkBasedAppHostServerProject(
            appPath,
            socketPath,
            dotNetCliRunner,
            packagingService,
            configurationService,
            loggerFactory.CreateLogger<DotNetSdkBasedAppHostServerProject>());
    }
}
