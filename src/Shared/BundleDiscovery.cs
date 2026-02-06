// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is source-linked into multiple projects:
// - Aspire.Hosting
// - Aspire.Cli
// Do not add project-specific dependencies.

using System.Runtime.InteropServices;

namespace Aspire.Shared;

/// <summary>
/// Shared logic for discovering Aspire bundle components.
/// Used by both CLI and Aspire.Hosting to ensure consistent discovery behavior.
/// </summary>
internal static class BundleDiscovery
{
    // ═══════════════════════════════════════════════════════════════════════
    // ENVIRONMENT VARIABLE CONSTANTS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Environment variable for the root of the bundle layout.
    /// </summary>
    public const string LayoutPathEnvVar = "ASPIRE_LAYOUT_PATH";

    /// <summary>
    /// Environment variable for overriding the DCP path.
    /// </summary>
    public const string DcpPathEnvVar = "ASPIRE_DCP_PATH";

    /// <summary>
    /// Environment variable for overriding the Dashboard path.
    /// </summary>
    public const string DashboardPathEnvVar = "ASPIRE_DASHBOARD_PATH";

    /// <summary>
    /// Environment variable for overriding the .NET runtime path.
    /// </summary>
    public const string RuntimePathEnvVar = "ASPIRE_RUNTIME_PATH";

    /// <summary>
    /// Environment variable for overriding the AppHost Server path.
    /// </summary>
    public const string AppHostServerPathEnvVar = "ASPIRE_APPHOST_SERVER_PATH";

    /// <summary>
    /// Environment variable to force SDK mode (skip bundle detection).
    /// </summary>
    public const string UseGlobalDotNetEnvVar = "ASPIRE_USE_GLOBAL_DOTNET";

    /// <summary>
    /// Environment variable indicating development mode (Aspire repo checkout).
    /// </summary>
    public const string RepoRootEnvVar = "ASPIRE_REPO_ROOT";

    // ═══════════════════════════════════════════════════════════════════════
    // BUNDLE LAYOUT DIRECTORY NAMES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Directory name for DCP in the bundle layout.
    /// </summary>
    public const string DcpDirectoryName = "dcp";

    /// <summary>
    /// Directory name for Dashboard in the bundle layout.
    /// </summary>
    public const string DashboardDirectoryName = "dashboard";

    /// <summary>
    /// Directory name for .NET runtime in the bundle layout.
    /// </summary>
    public const string RuntimeDirectoryName = "runtime";

    /// <summary>
    /// Directory name for AppHost Server in the bundle layout.
    /// </summary>
    public const string AppHostServerDirectoryName = "aspire-server";

    /// <summary>
    /// Directory name for NuGet Helper tool in the bundle layout.
    /// </summary>
    public const string NuGetHelperDirectoryName = "tools/aspire-nuget";

    /// <summary>
    /// Directory name for dev-certs tool in the bundle layout.
    /// </summary>
    public const string DevCertsDirectoryName = "tools/dev-certs";

    // ═══════════════════════════════════════════════════════════════════════
    // EXECUTABLE NAMES (without path, just the file name)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executable name for the AppHost Server.
    /// </summary>
    public const string AppHostServerExecutableName = "aspire-server";

    /// <summary>
    /// Executable name for the Dashboard. Must match AspireDashboardBinaryName in Directory.Build.props.
    /// </summary>
    public const string DashboardExecutableName = "aspire-dashboard";

    /// <summary>
    /// Executable name for the NuGet Helper tool.
    /// </summary>
    public const string NuGetHelperExecutableName = "aspire-nuget";

    /// <summary>
    /// Executable name for the dev-certs tool.
    /// </summary>
    public const string DevCertsExecutableName = "dotnet-dev-certs";

    // ═══════════════════════════════════════════════════════════════════════
    // DISCOVERY METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Attempts to discover DCP from a base directory.
    /// Checks for the expected bundle layout structure.
    /// </summary>
    /// <param name="baseDirectory">The base directory to search from (e.g., CLI location or entry assembly directory).</param>
    /// <param name="dcpCliPath">The full path to the DCP executable if found.</param>
    /// <param name="dcpExtensionsPath">The full path to the DCP extensions directory if found.</param>
    /// <param name="dcpBinPath">The full path to the DCP bin directory if found.</param>
    /// <returns>True if DCP was found, false otherwise.</returns>
    public static bool TryDiscoverDcpFromDirectory(
        string baseDirectory,
        out string? dcpCliPath,
        out string? dcpExtensionsPath,
        out string? dcpBinPath)
    {
        dcpCliPath = null;
        dcpExtensionsPath = null;
        dcpBinPath = null;

        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return false;
        }

        var dcpDir = Path.Combine(baseDirectory, DcpDirectoryName);
        var dcpExePath = GetDcpExecutablePath(dcpDir);

        if (File.Exists(dcpExePath))
        {
            dcpCliPath = dcpExePath;
            dcpExtensionsPath = Path.Combine(dcpDir, "ext");
            dcpBinPath = Path.Combine(dcpExtensionsPath, "bin");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to discover Dashboard from a base directory.
    /// </summary>
    /// <param name="baseDirectory">The base directory to search from.</param>
    /// <param name="dashboardPath">The full path to the Dashboard directory if found.</param>
    /// <returns>True if Dashboard was found, false otherwise.</returns>
    public static bool TryDiscoverDashboardFromDirectory(
        string baseDirectory,
        out string? dashboardPath)
    {
        dashboardPath = null;

        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return false;
        }

        var dashboardDir = Path.Combine(baseDirectory, DashboardDirectoryName);
        var dashboardExe = Path.Combine(dashboardDir, GetExecutableFileName(DashboardExecutableName));

        if (File.Exists(dashboardExe))
        {
            dashboardPath = dashboardDir;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to discover DCP relative to the entry assembly.
    /// This is used by Aspire.Hosting when no environment variables are set.
    /// </summary>
    public static bool TryDiscoverDcpFromEntryAssembly(
        out string? dcpCliPath,
        out string? dcpExtensionsPath,
        out string? dcpBinPath)
    {
        dcpCliPath = null;
        dcpExtensionsPath = null;
        dcpBinPath = null;

        var baseDir = GetEntryAssemblyDirectory();
        if (baseDir is null)
        {
            return false;
        }

        return TryDiscoverDcpFromDirectory(baseDir, out dcpCliPath, out dcpExtensionsPath, out dcpBinPath);
    }

    /// <summary>
    /// Attempts to discover Dashboard relative to the entry assembly.
    /// This is used by Aspire.Hosting when no environment variables are set.
    /// </summary>
    public static bool TryDiscoverDashboardFromEntryAssembly(out string? dashboardPath)
    {
        dashboardPath = null;

        var baseDir = GetEntryAssemblyDirectory();
        if (baseDir is null)
        {
            return false;
        }

        return TryDiscoverDashboardFromDirectory(baseDir, out dashboardPath);
    }

    /// <summary>
    /// Attempts to discover .NET runtime from a base directory.
    /// Checks for the expected bundle layout structure with dotnet executable.
    /// </summary>
    /// <param name="baseDirectory">The base directory to search from.</param>
    /// <param name="runtimePath">The full path to the runtime directory if found.</param>
    /// <returns>True if runtime was found, false otherwise.</returns>
    public static bool TryDiscoverRuntimeFromDirectory(string baseDirectory, out string? runtimePath)
    {
        runtimePath = null;

        if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return false;
        }

        var runtimeDir = Path.Combine(baseDirectory, RuntimeDirectoryName);
        var dotnetPath = Path.Combine(runtimeDir, GetDotNetExecutableName());

        if (File.Exists(dotnetPath))
        {
            runtimePath = runtimeDir;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to discover .NET runtime relative to the entry assembly.
    /// This is used by Aspire.Hosting when no environment variables are set.
    /// </summary>
    public static bool TryDiscoverRuntimeFromEntryAssembly(out string? runtimePath)
    {
        runtimePath = null;

        var baseDir = GetEntryAssemblyDirectory();
        if (baseDir is null)
        {
            return false;
        }

        return TryDiscoverRuntimeFromDirectory(baseDir, out runtimePath);
    }

    /// <summary>
    /// Attempts to discover DCP relative to the current process.
    /// This is used by CLI to find DCP in the bundle layout.
    /// </summary>
    public static bool TryDiscoverDcpFromProcessPath(
        out string? dcpCliPath,
        out string? dcpExtensionsPath,
        out string? dcpBinPath)
    {
        dcpCliPath = null;
        dcpExtensionsPath = null;
        dcpBinPath = null;

        var baseDir = GetProcessDirectory();
        if (baseDir is null)
        {
            return false;
        }

        return TryDiscoverDcpFromDirectory(baseDir, out dcpCliPath, out dcpExtensionsPath, out dcpBinPath);
    }

    /// <summary>
    /// Attempts to discover Dashboard relative to the current process.
    /// </summary>
    public static bool TryDiscoverDashboardFromProcessPath(out string? dashboardPath)
    {
        dashboardPath = null;

        var baseDir = GetProcessDirectory();
        if (baseDir is null)
        {
            return false;
        }

        return TryDiscoverDashboardFromDirectory(baseDir, out dashboardPath);
    }

    /// <summary>
    /// Attempts to discover .NET runtime relative to the current process.
    /// </summary>
    public static bool TryDiscoverRuntimeFromProcessPath(out string? runtimePath)
    {
        runtimePath = null;

        var baseDir = GetProcessDirectory();
        if (baseDir is null)
        {
            return false;
        }

        return TryDiscoverRuntimeFromDirectory(baseDir, out runtimePath);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the full path to the DCP executable given a DCP directory.
    /// </summary>
    public static string GetDcpExecutablePath(string dcpDirectory)
    {
        var exeName = GetDcpExecutableName();
        return Path.Combine(dcpDirectory, exeName);
    }

    /// <summary>
    /// Gets the platform-specific DCP executable name.
    /// </summary>
    public static string GetDcpExecutableName()
    {
        return OperatingSystem.IsWindows() ? "dcp.exe" : "dcp";
    }

    /// <summary>
    /// Gets the platform-specific dotnet executable name.
    /// </summary>
    public static string GetDotNetExecutableName()
    {
        return OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
    }

    /// <summary>
    /// Gets the platform-specific executable name with extension.
    /// </summary>
    /// <param name="baseName">The base executable name without extension (e.g., "aspire-server").</param>
    /// <returns>The executable name with platform-appropriate extension.</returns>
    public static string GetExecutableFileName(string baseName)
    {
        return OperatingSystem.IsWindows() ? $"{baseName}.exe" : baseName;
    }

    /// <summary>
    /// Gets the platform-specific DLL name.
    /// </summary>
    /// <param name="baseName">The base name without extension (e.g., "aspire-server").</param>
    /// <returns>The DLL name (e.g., "aspire-server.dll").</returns>
    public static string GetDllFileName(string baseName)
    {
        return $"{baseName}.dll";
    }

    /// <summary>
    /// Gets the full path to the dotnet executable from the bundled runtime, or "dotnet" if not available.
    /// Resolution order: environment variable → disk discovery → PATH fallback.
    /// </summary>
    /// <returns>Full path to bundled dotnet executable, or "dotnet" to use PATH resolution.</returns>
    public static string GetDotNetExecutablePath()
    {
        // 1. Check environment variable (set by CLI for guest apphosts)
        var runtimePath = Environment.GetEnvironmentVariable(RuntimePathEnvVar);
        if (!string.IsNullOrEmpty(runtimePath))
        {
            var dotnetPath = Path.Combine(runtimePath, GetDotNetExecutableName());
            if (File.Exists(dotnetPath))
            {
                return dotnetPath;
            }
        }

        // 2. Try disk discovery (for future installed bundle scenario)
        if (TryDiscoverRuntimeFromEntryAssembly(out var discoveredRuntimePath) && discoveredRuntimePath is not null)
        {
            var dotnetPath = Path.Combine(discoveredRuntimePath, GetDotNetExecutableName());
            if (File.Exists(dotnetPath))
            {
                return dotnetPath;
            }
        }

        // 3. Fall back to PATH-based resolution
        return "dotnet";
    }

    /// <summary>
    /// Gets the DOTNET_ROOT path for the bundled runtime.
    /// This is the directory containing the dotnet executable and shared frameworks.
    /// </summary>
    /// <returns>The DOTNET_ROOT path if available, otherwise null.</returns>
    public static string? GetDotNetRoot()
    {
        // 1. Check environment variable (set by CLI for guest apphosts)
        var runtimePath = Environment.GetEnvironmentVariable(RuntimePathEnvVar);
        if (!string.IsNullOrEmpty(runtimePath) && Directory.Exists(runtimePath))
        {
            return runtimePath;
        }

        // 2. Try disk discovery (for future installed bundle scenario)
        if (TryDiscoverRuntimeFromEntryAssembly(out var discoveredRuntimePath) && discoveredRuntimePath is not null)
        {
            return discoveredRuntimePath;
        }

        return null;
    }

    /// <summary>
    /// Gets the current platform's runtime identifier.
    /// </summary>
    public static string GetCurrentRuntimeIdentifier()
    {
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64"
        };

        if (OperatingSystem.IsWindows())
        {
            return $"win-{arch}";
        }

        if (OperatingSystem.IsMacOS())
        {
            return $"osx-{arch}";
        }

        if (OperatingSystem.IsLinux())
        {
            return $"linux-{arch}";
        }

        return $"unknown-{arch}";
    }

    /// <summary>
    /// Gets the archive extension for the current platform.
    /// </summary>
    public static string GetArchiveExtension()
    {
        return OperatingSystem.IsWindows() ? ".zip" : ".tar.gz";
    }

    /// <summary>
    /// Gets the directory containing the entry assembly, if available.
    /// For native AOT or single-file apps, uses AppContext.BaseDirectory or ProcessPath fallback.
    /// </summary>
    private static string? GetEntryAssemblyDirectory()
    {
        // For native AOT and single-file apps, Assembly.Location returns empty
        // Use AppContext.BaseDirectory as the primary fallback
        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir) && Directory.Exists(baseDir))
        {
            // Remove trailing separator if present
            return baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        // Final fallback: try process path
        return GetProcessDirectory();
    }

    /// <summary>
    /// Gets the directory containing the current process executable.
    /// </summary>
    private static string? GetProcessDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return null;
        }

        return Path.GetDirectoryName(processPath);
    }
}
