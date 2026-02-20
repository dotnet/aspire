// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;

namespace Aspire.Cli.Layout;

/// <summary>
/// Known layout component types.
/// </summary>
public enum LayoutComponent
{
    /// <summary>CLI executable.</summary>
    Cli,
    /// <summary>.NET runtime.</summary>
    Runtime,
    /// <summary>Pre-built AppHost Server.</summary>
    AppHostServer,
    /// <summary>Aspire Dashboard.</summary>
    Dashboard,
    /// <summary>Developer Control Plane.</summary>
    Dcp,
    /// <summary>NuGet Helper tool.</summary>
    NuGetHelper,
    /// <summary>Dev-certs tool.</summary>
    DevCerts
}

/// <summary>
/// Configuration for the Aspire bundle layout.
/// Specifies paths to all components in a self-contained bundle.
/// </summary>
public sealed class LayoutConfiguration
{
    /// <summary>
    /// Bundle version (e.g., "13.2.0" or "dev" for local development).
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Target platform (e.g., "linux-x64", "win-x64").
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// .NET runtime version included in the bundle (e.g., "10.0.0").
    /// </summary>
    public string? RuntimeVersion { get; set; }

    /// <summary>
    /// Root path of the layout.
    /// </summary>
    public string? LayoutPath { get; set; }

    /// <summary>
    /// Component paths relative to LayoutPath.
    /// </summary>
    public LayoutComponents Components { get; set; } = new();

    /// <summary>
    /// List of integrations included in the bundle.
    /// </summary>
    public List<string> BuiltInIntegrations { get; set; } = [];

    /// <summary>
    /// Gets the absolute path to a component.
    /// </summary>
    public string? GetComponentPath(LayoutComponent component)
    {
        if (string.IsNullOrEmpty(LayoutPath))
        {
            return null;
        }

        var relativePath = component switch
        {
            LayoutComponent.Cli => Components.Cli,
            LayoutComponent.Runtime => Components.Runtime,
            LayoutComponent.AppHostServer => Components.ApphostServer,
            LayoutComponent.Dashboard => Components.Dashboard,
            LayoutComponent.Dcp => Components.Dcp,
            LayoutComponent.NuGetHelper => Components.NugetHelper,
            LayoutComponent.DevCerts => Components.DevCerts,
            _ => null
        };

        return relativePath is not null ? Path.Combine(LayoutPath, relativePath) : null;
    }

    /// <summary>
    /// Gets the path to the dotnet muxer executable from the bundled runtime.
    /// </summary>
    public string? GetMuxerPath()
    {
        var runtimePath = GetComponentPath(LayoutComponent.Runtime);
        if (runtimePath is null)
        {
            return null;
        }

        var bundledPath = Path.Combine(runtimePath, BundleDiscovery.GetDotNetExecutableName());
        return File.Exists(bundledPath) ? bundledPath : null;
    }

    /// <summary>
    /// Gets the path to the dotnet executable. Alias for GetMuxerPath.
    /// </summary>
    public string? GetDotNetExePath() => GetMuxerPath();

    /// <summary>
    /// Gets the path to the DCP directory.
    /// </summary>
    public string? GetDcpPath() => GetComponentPath(LayoutComponent.Dcp);

    /// <summary>
    /// Gets the path to the Dashboard directory.
    /// </summary>
    public string? GetDashboardPath() => GetComponentPath(LayoutComponent.Dashboard);

    /// <summary>
    /// Gets the path to the AppHost Server executable.
    /// </summary>
    /// <returns>The path to aspire-server.exe.</returns>
    public string? GetAppHostServerPath()
    {
        var serverPath = GetComponentPath(LayoutComponent.AppHostServer);
        if (serverPath is null)
        {
            return null;
        }

        return Path.Combine(serverPath, BundleDiscovery.GetExecutableFileName(BundleDiscovery.AppHostServerExecutableName));
    }

    /// <summary>
    /// Gets the path to the NuGet Helper executable.
    /// </summary>
    /// <returns>The path to aspire-nuget.exe.</returns>
    public string? GetNuGetHelperPath()
    {
        var helperPath = GetComponentPath(LayoutComponent.NuGetHelper);
        if (helperPath is null)
        {
            return null;
        }

        return Path.Combine(helperPath, BundleDiscovery.GetExecutableFileName(BundleDiscovery.NuGetHelperExecutableName));
    }

    /// <summary>
    /// Gets the path to the dev-certs DLL (requires dotnet muxer to run).
    /// </summary>
    public string? GetDevCertsPath()
    {
        var devCertsPath = GetComponentPath(LayoutComponent.DevCerts);
        return devCertsPath is not null ? Path.Combine(devCertsPath, BundleDiscovery.GetDllFileName(BundleDiscovery.DevCertsExecutableName)) : null;
    }
}

/// <summary>
/// Component paths within the layout.
/// </summary>
public sealed class LayoutComponents
{
    /// <summary>
    /// Path to CLI executable (e.g., "aspire" or "aspire.exe").
    /// </summary>
    public string? Cli { get; set; } = "aspire";

    /// <summary>
    /// Path to .NET runtime directory.
    /// </summary>
    public string? Runtime { get; set; } = BundleDiscovery.RuntimeDirectoryName;

    /// <summary>
    /// Path to pre-built AppHost Server.
    /// </summary>
    public string? ApphostServer { get; set; } = BundleDiscovery.AppHostServerDirectoryName;

    /// <summary>
    /// Path to Aspire Dashboard.
    /// </summary>
    public string? Dashboard { get; set; } = BundleDiscovery.DashboardDirectoryName;

    /// <summary>
    /// Path to Developer Control Plane.
    /// </summary>
    public string? Dcp { get; set; } = BundleDiscovery.DcpDirectoryName;

    /// <summary>
    /// Path to NuGet Helper tool.
    /// </summary>
    public string? NugetHelper { get; set; } = BundleDiscovery.NuGetHelperDirectoryName;

    /// <summary>
    /// Path to dev-certs tool.
    /// </summary>
    public string? DevCerts { get; set; } = BundleDiscovery.DevCertsDirectoryName;
}
