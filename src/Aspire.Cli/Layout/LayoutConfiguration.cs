// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private string? _cachedMuxerPath;
    private bool _muxerPathCached;

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
    /// Gets the path to the dotnet muxer executable.
    /// Set ASPIRE_ALLOW_GLOBAL_DOTNET=true to enable fallback to global dotnet in bundle mode.
    /// </summary>
    public string? GetMuxerPath()
    {
        if (_muxerPathCached)
        {
            return _cachedMuxerPath;
        }

        _cachedMuxerPath = ResolveMuxerPath();
        _muxerPathCached = true;
        return _cachedMuxerPath;
    }

    private string? ResolveMuxerPath()
    {
        var runtimePath = GetComponentPath(LayoutComponent.Runtime);
        if (runtimePath is not null)
        {
            var muxerName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
            var bundledPath = Path.Combine(runtimePath, muxerName);
            if (File.Exists(bundledPath))
            {
                return bundledPath;
            }
        }

        // In bundle mode, only fall back to global dotnet if explicitly allowed
        var allowGlobal = Environment.GetEnvironmentVariable("ASPIRE_ALLOW_GLOBAL_DOTNET");
        if (string.Equals(allowGlobal, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(allowGlobal, "1", StringComparison.OrdinalIgnoreCase))
        {
            return FindGlobalDotNet();
        }

        // Bundle mode with no bundled runtime and fallback disabled
        return null;
    }

    /// <summary>
    /// Finds the global dotnet executable on the system PATH.
    /// </summary>
    private static string? FindGlobalDotNet()
    {
        var dotnetName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        // Check DOTNET_ROOT first
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            var dotnetPath = Path.Combine(dotnetRoot, dotnetName);
            if (File.Exists(dotnetPath))
            {
                return dotnetPath;
            }
        }

        // Check PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv is not null)
        {
            var separator = OperatingSystem.IsWindows() ? ';' : ':';
            foreach (var dir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                var dotnetPath = Path.Combine(dir, dotnetName);
                if (File.Exists(dotnetPath))
                {
                    return dotnetPath;
                }
            }
        }

        // On Windows, check default install location
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var defaultPath = Path.Combine(programFiles, "dotnet", dotnetName);
            if (File.Exists(defaultPath))
            {
                return defaultPath;
            }
        }

        // Just return "dotnet" and let the OS resolve it
        return dotnetName;
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
    /// Gets the path to the AppHost Server executable or DLL.
    /// </summary>
    /// <returns>The path to Aspire.Hosting.RemoteHost executable if it exists, otherwise Aspire.Hosting.RemoteHost.dll.</returns>
    public string? GetAppHostServerPath()
    {
        var serverPath = GetComponentPath(LayoutComponent.AppHostServer);
        if (serverPath is null)
        {
            return null;
        }

        // Check for single-file exe first (bundle distribution)
        // On Windows: Aspire.Hosting.RemoteHost.exe, on Unix: Aspire.Hosting.RemoteHost (no extension)
        var exeName = OperatingSystem.IsWindows() ? "Aspire.Hosting.RemoteHost.exe" : "Aspire.Hosting.RemoteHost";
        var exePath = Path.Combine(serverPath, exeName);
        if (File.Exists(exePath))
        {
            return exePath;
        }

        // Fall back to DLL (dev mode or framework-dependent publish)
        return Path.Combine(serverPath, "Aspire.Hosting.RemoteHost.dll");
    }

    /// <summary>
    /// Gets the path to the AppHost Server DLL.
    /// </summary>
    [Obsolete("Use GetAppHostServerPath() instead which supports both exe and dll.")]
    public string? GetAppHostServerDllPath()
    {
        var serverPath = GetComponentPath(LayoutComponent.AppHostServer);
        return serverPath is not null ? Path.Combine(serverPath, "Aspire.Hosting.RemoteHost.dll") : null;
    }

    /// <summary>
    /// Gets the path to the NuGet Helper executable or DLL.
    /// </summary>
    /// <returns>The path to aspire-nuget executable if it exists, otherwise aspire-nuget.dll.</returns>
    public string? GetNuGetHelperPath()
    {
        var helperPath = GetComponentPath(LayoutComponent.NuGetHelper);
        if (helperPath is null)
        {
            return null;
        }

        // Check for single-file exe first (bundle distribution)
        // On Windows: aspire-nuget.exe, on Unix: aspire-nuget (no extension)
        var exeName = OperatingSystem.IsWindows() ? "aspire-nuget.exe" : "aspire-nuget";
        var exePath = Path.Combine(helperPath, exeName);
        if (File.Exists(exePath))
        {
            return exePath;
        }

        // Fall back to DLL (dev mode or framework-dependent publish)
        return Path.Combine(helperPath, "aspire-nuget.dll");
    }

    /// <summary>
    /// Gets the path to the NuGet Helper DLL.
    /// </summary>
    [Obsolete("Use GetNuGetHelperPath() instead which supports both exe and dll.")]
    public string? GetNuGetHelperDllPath()
    {
        var helperPath = GetComponentPath(LayoutComponent.NuGetHelper);
        return helperPath is not null ? Path.Combine(helperPath, "aspire-nuget.dll") : null;
    }

    /// <summary>
    /// Gets the path to the dev-certs DLL.
    /// </summary>
    public string? GetDevCertsDllPath()
    {
        var devCertsPath = GetComponentPath(LayoutComponent.DevCerts);
        return devCertsPath is not null ? Path.Combine(devCertsPath, "dotnet-dev-certs.dll") : null;
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
    /// Path to .NET runtime directory. Null in dev mode (uses global dotnet).
    /// </summary>
    public string? Runtime { get; set; } = "runtime";

    /// <summary>
    /// Path to pre-built AppHost Server.
    /// </summary>
    public string? ApphostServer { get; set; } = "apphost-server";

    /// <summary>
    /// Path to Aspire Dashboard. Null if resolved via NuGet.
    /// </summary>
    public string? Dashboard { get; set; } = "dashboard";

    /// <summary>
    /// Path to Developer Control Plane. Null if resolved via NuGet.
    /// </summary>
    public string? Dcp { get; set; } = "dcp";

    /// <summary>
    /// Path to NuGet Helper tool.
    /// </summary>
    public string? NugetHelper { get; set; } = "tools/aspire-nuget";

    /// <summary>
    /// Path to dev-certs tool.
    /// </summary>
    public string? DevCerts { get; set; } = "tools/dev-certs";
}
