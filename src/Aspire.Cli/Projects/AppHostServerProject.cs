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
/// Factory for creating AppHost server project instances.
/// The factory determines which implementation to use based on environment and layout:
/// - Dev mode (ASPIRE_REPO_ROOT set): DevAppHostServerProject with project references
/// - Bundle mode (layout found): PrebuiltAppHostServer with pre-built server
/// - SDK mode (default): DotNetSdkBasedAppHostServerProject with NuGet packages
/// </summary>
internal interface IAppHostServerProjectFactory
{
    /// <summary>
    /// Creates an AppHost server project for the specified app path.
    /// </summary>
    /// <param name="appPath">The path to the user's polyglot app host.</param>
    /// <returns>An AppHost server project instance.</returns>
    IAppHostServerProject Create(string appPath);
}

/// <summary>
/// Factory implementation that creates the appropriate AppHost server project based on environment.
/// </summary>
internal sealed class AppHostServerProjectFactory : IAppHostServerProjectFactory
{
    private readonly IDotNetCliRunner _dotNetCliRunner;
    private readonly IPackagingService _packagingService;
    private readonly IConfigurationService _configurationService;
    private readonly ILayoutDiscovery _layoutDiscovery;
    private readonly BundleNuGetService _bundleNuGetService;
    private readonly ILogger<DevAppHostServerProject> _devLogger;
    private readonly ILogger<DotNetSdkBasedAppHostServerProject> _sdkLogger;
    private readonly ILogger<PrebuiltAppHostServer> _prebuiltLogger;

#if DEBUG
    private readonly string? _repoRoot;
#endif

    public AppHostServerProjectFactory(
        IDotNetCliRunner dotNetCliRunner,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        ILayoutDiscovery layoutDiscovery,
        BundleNuGetService bundleNuGetService,
        ILogger<DevAppHostServerProject> devLogger,
        ILogger<DotNetSdkBasedAppHostServerProject> sdkLogger,
        ILogger<PrebuiltAppHostServer> prebuiltLogger)
    {
        _dotNetCliRunner = dotNetCliRunner;
        _packagingService = packagingService;
        _configurationService = configurationService;
        _layoutDiscovery = layoutDiscovery;
        _bundleNuGetService = bundleNuGetService;
        _devLogger = devLogger;
        _sdkLogger = sdkLogger;
        _prebuiltLogger = prebuiltLogger;
#if DEBUG
        _repoRoot = Environment.GetEnvironmentVariable("ASPIRE_REPO_ROOT");
#endif
    }

    public IAppHostServerProject Create(string appPath)
    {
        var socketPath = GetSocketPath(appPath);

#if DEBUG
        // Dev mode (ASPIRE_REPO_ROOT set) - only available in debug builds for local Aspire development
        if (!string.IsNullOrEmpty(_repoRoot))
        {
            _devLogger.LogDebug("Using dev mode (DevAppHostServerProject) - ASPIRE_REPO_ROOT is set");
            return new DevAppHostServerProject(appPath, socketPath, _repoRoot, _dotNetCliRunner, _packagingService, _configurationService, _devLogger);
        }
#endif

        // Priority 1: Bundle mode (layout found) - for shipped CLI without .NET SDK
        var layout = _layoutDiscovery.DiscoverLayout();
        if (layout is not null)
        {
            _prebuiltLogger.LogDebug("Using bundle mode (PrebuiltAppHostServer)");
            return new PrebuiltAppHostServer(appPath, socketPath, layout, _bundleNuGetService, _packagingService, _prebuiltLogger);
        }

        // Priority 2: SDK mode (default) - for users with .NET SDK installed
        _sdkLogger.LogDebug("Using SDK mode (DotNetSdkBasedAppHostServerProject)");
        return new DotNetSdkBasedAppHostServerProject(appPath, socketPath, _dotNetCliRunner, _packagingService, _configurationService, _sdkLogger);
    }

    private static string GetSocketPath(string appPath)
    {
        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(appPath));
        var socketName = Convert.ToHexString(pathHash)[..12].ToLowerInvariant() + ".sock";

        // On Windows, named pipes use just a name, not a file path.
        // The .NET NamedPipeServerStream and clients will automatically
        // use the \\.\pipe\ prefix.
        if (OperatingSystem.IsWindows())
        {
            return socketName;
        }

        // On Unix/macOS, use Unix domain sockets with a file path
        var socketDir = Path.Combine(Path.GetTempPath(), ".aspire", "sockets");
        Directory.CreateDirectory(socketDir);

        return Path.Combine(socketDir, socketName);
    }
}
