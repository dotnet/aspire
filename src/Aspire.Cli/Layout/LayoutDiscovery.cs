// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Layout;

/// <summary>
/// Service for discovering and loading Aspire bundle layouts.
/// Uses priority-based resolution: environment variables > relative paths from CLI location.
/// </summary>
public interface ILayoutDiscovery
{
    /// <summary>
    /// Attempts to discover a valid layout configuration.
    /// </summary>
    /// <param name="projectDirectory">Optional project directory (unused, kept for API compatibility).</param>
    /// <returns>Layout configuration if found and valid, null otherwise.</returns>
    LayoutConfiguration? DiscoverLayout(string? projectDirectory = null);

    /// <summary>
    /// Gets the path to a specific component, checking environment variable overrides first.
    /// </summary>
    string? GetComponentPath(LayoutComponent component, string? projectDirectory = null);

    /// <summary>
    /// Checks if bundle mode is available and should be used.
    /// </summary>
    bool IsBundleModeAvailable(string? projectDirectory = null);
}

/// <summary>
/// Implementation of layout discovery with priority-based resolution.
/// </summary>
public sealed class LayoutDiscovery : ILayoutDiscovery
{
    private readonly ILogger<LayoutDiscovery> _logger;

    public LayoutDiscovery(ILogger<LayoutDiscovery> logger)
    {
        _logger = logger;
    }

    public LayoutConfiguration? DiscoverLayout(string? projectDirectory = null)
    {
        // 1. Try environment variable for layout path
        var envLayoutPath = Environment.GetEnvironmentVariable(BundleDiscovery.LayoutPathEnvVar);
        if (!string.IsNullOrEmpty(envLayoutPath))
        {
            _logger.LogDebug("Found ASPIRE_LAYOUT_PATH: {Path}", envLayoutPath);
            var config = TryLoadLayoutFromPath(envLayoutPath);
            if (config is not null)
            {
                return LogEnvironmentOverrides(config);
            }
        }

        // 2. Try relative paths from CLI executable
        var relativeLayout = TryDiscoverRelativeLayout();
        if (relativeLayout is not null)
        {
            _logger.LogDebug("Discovered layout relative to CLI: {Path}", relativeLayout.LayoutPath);
            return LogEnvironmentOverrides(relativeLayout);
        }

        _logger.LogDebug("No bundle layout discovered");
        return null;
    }

    public string? GetComponentPath(LayoutComponent component, string? projectDirectory = null)
    {
        // Check environment variable overrides first
        var envPath = component switch
        {
            LayoutComponent.Runtime => Environment.GetEnvironmentVariable(BundleDiscovery.RuntimePathEnvVar),
            LayoutComponent.Dcp => Environment.GetEnvironmentVariable(BundleDiscovery.DcpPathEnvVar),
            LayoutComponent.Dashboard => Environment.GetEnvironmentVariable(BundleDiscovery.DashboardPathEnvVar),
            LayoutComponent.AppHostServer => Environment.GetEnvironmentVariable(BundleDiscovery.AppHostServerPathEnvVar),
            _ => null
        };

        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
        {
            return envPath;
        }

        // Fall back to layout configuration
        var layout = DiscoverLayout(projectDirectory);
        return layout?.GetComponentPath(component);
    }

    public bool IsBundleModeAvailable(string? projectDirectory = null)
    {
        // Check if user explicitly wants SDK mode
        var useSdk = Environment.GetEnvironmentVariable(BundleDiscovery.UseGlobalDotNetEnvVar);
        if (string.Equals(useSdk, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(useSdk, "1", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("SDK mode forced via {EnvVar}", BundleDiscovery.UseGlobalDotNetEnvVar);
            return false;
        }

        var layout = DiscoverLayout(projectDirectory);
        if (layout is null)
        {
            return false;
        }

        // Validate that essential components exist
        return ValidateLayout(layout);
    }

    private LayoutConfiguration? TryLoadLayoutFromPath(string layoutPath)
    {
        _logger.LogDebug("TryLoadLayoutFromPath: {Path}", layoutPath);
        
        if (!Directory.Exists(layoutPath))
        {
            _logger.LogDebug("Layout path does not exist: {Path}", layoutPath);
            return null;
        }

        _logger.LogDebug("Layout path exists, checking directory structure...");
        
        // Log directory contents for debugging
        try
        {
            var entries = Directory.GetFileSystemEntries(layoutPath).Select(Path.GetFileName).ToArray();
            _logger.LogDebug("Layout directory contents: {Contents}", string.Join(", ", entries));
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not list directory contents: {Error}", ex.Message);
        }

        // Infer layout from directory structure (well-known relative paths)
        return TryInferLayout(layoutPath);
    }

    private LayoutConfiguration? TryDiscoverRelativeLayout()
    {
        // Get CLI executable location
        var cliPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(cliPath))
        {
            _logger.LogDebug("TryDiscoverRelativeLayout: ProcessPath is null or empty");
            return null;
        }

        var cliDir = Path.GetDirectoryName(cliPath);
        if (string.IsNullOrEmpty(cliDir))
        {
            _logger.LogDebug("TryDiscoverRelativeLayout: Could not get directory from ProcessPath");
            return null;
        }

        _logger.LogDebug("TryDiscoverRelativeLayout: CLI at {Path}, checking for layout...", cliDir);

        // Check if CLI is in a bundle layout
        // First, check if components are siblings of the CLI (flat layout):
        //   {layout}/aspire + {layout}/runtime/ + {layout}/dashboard/ + ...
        var layout = TryInferLayout(cliDir);
        if (layout is not null)
        {
            return layout;
        }

        // Next, check the parent directory (bin/ layout where CLI is in a subdirectory):
        //   {layout}/bin/aspire + {layout}/runtime/ + {layout}/dashboard/ + ...
        var parentDir = Path.GetDirectoryName(cliDir);
        if (!string.IsNullOrEmpty(parentDir))
        {
            _logger.LogDebug("TryDiscoverRelativeLayout: Checking parent directory {Path}...", parentDir);
            layout = TryInferLayout(parentDir);
            if (layout is not null)
            {
                return layout;
            }
        }

        return null;
    }

    private LayoutConfiguration? TryInferLayout(string layoutPath)
    {
        // Check for essential directories using BundleDiscovery constants
        var runtimePath = Path.Combine(layoutPath, BundleDiscovery.RuntimeDirectoryName);
        var dashboardPath = Path.Combine(layoutPath, BundleDiscovery.DashboardDirectoryName);
        var dcpPath = Path.Combine(layoutPath, BundleDiscovery.DcpDirectoryName);
        var serverPath = Path.Combine(layoutPath, BundleDiscovery.AppHostServerDirectoryName);

        _logger.LogDebug("TryInferLayout: Checking layout at {Path}", layoutPath);
        _logger.LogDebug("  {Dir}/: {Exists}", BundleDiscovery.RuntimeDirectoryName, Directory.Exists(runtimePath) ? "exists" : "MISSING");
        _logger.LogDebug("  {Dir}/: {Exists}", BundleDiscovery.DashboardDirectoryName, Directory.Exists(dashboardPath) ? "exists" : "MISSING");
        _logger.LogDebug("  {Dir}/: {Exists}", BundleDiscovery.DcpDirectoryName, Directory.Exists(dcpPath) ? "exists" : "MISSING");
        _logger.LogDebug("  {Dir}/: {Exists}", BundleDiscovery.AppHostServerDirectoryName, Directory.Exists(serverPath) ? "exists" : "MISSING");

        if (!Directory.Exists(runtimePath) || !Directory.Exists(dashboardPath) || 
            !Directory.Exists(dcpPath) || !Directory.Exists(serverPath))
        {
            _logger.LogDebug("TryInferLayout: Layout rejected - missing required directories");
            return null;
        }

        // Check for muxer
        var muxerName = BundleDiscovery.GetDotNetExecutableName();
        var muxerPath = Path.Combine(runtimePath, muxerName);
        _logger.LogDebug("  runtime/{Muxer}: {Exists}", muxerName, File.Exists(muxerPath) ? "exists" : "MISSING");
        
        if (!File.Exists(muxerPath))
        {
            _logger.LogDebug("TryInferLayout: Layout rejected - muxer not found");
            return null;
        }

        _logger.LogDebug("TryInferLayout: Layout is valid");

        // Infer a basic layout configuration
        return new LayoutConfiguration
        {
            LayoutPath = layoutPath,
            Components = new LayoutComponents()
        };
    }

    private LayoutConfiguration LogEnvironmentOverrides(LayoutConfiguration config)
    {
        // Environment variables for specific components take precedence
        // These will be checked at GetComponentPath time, but we note them here for logging
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(BundleDiscovery.RuntimePathEnvVar)))
        {
            _logger.LogDebug("Runtime path override from {EnvVar}", BundleDiscovery.RuntimePathEnvVar);
        }
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(BundleDiscovery.DcpPathEnvVar)))
        {
            _logger.LogDebug("DCP path override from {EnvVar}", BundleDiscovery.DcpPathEnvVar);
        }
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(BundleDiscovery.DashboardPathEnvVar)))
        {
            _logger.LogDebug("Dashboard path override from {EnvVar}", BundleDiscovery.DashboardPathEnvVar);
        }

        return config;
    }

    private bool ValidateLayout(LayoutConfiguration layout)
    {
        // Check that muxer exists (global dotnet in dev mode, bundled in production)
        var muxerPath = layout.GetMuxerPath();
        if (muxerPath is null || !File.Exists(muxerPath))
        {
            _logger.LogDebug("Layout validation failed: muxer not found at {Path}", muxerPath);
            return false;
        }

        // Check that AppHostServer exists
        var serverPath = layout.GetAppHostServerPath();
        if (serverPath is null || !File.Exists(serverPath))
        {
            _logger.LogDebug("Layout validation failed: AppHostServer not found at {Path}", serverPath);
            return false;
        }

        // Require DCP and Dashboard for valid layouts
        var dcpPath = layout.GetComponentPath(LayoutComponent.Dcp);
        if (dcpPath is null || !Directory.Exists(dcpPath))
        {
            _logger.LogDebug("Layout validation failed: DCP not found");
            return false;
        }

        var dashboardPath = layout.GetComponentPath(LayoutComponent.Dashboard);
        if (dashboardPath is null || !Directory.Exists(dashboardPath))
        {
            _logger.LogDebug("Layout validation failed: Dashboard not found");
            return false;
        }

        return true;
    }
}
