// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Layout;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Manages a pre-built AppHost server from the Aspire bundle layout.
/// This is used when running in bundle mode (without .NET SDK) to avoid
/// dynamic project generation and building.
/// </summary>
internal sealed class PrebuiltAppHostServer : IAppHostServerProject
{
    private readonly string _appPath;
    private readonly string _socketPath;
    private readonly LayoutConfiguration _layout;
    private readonly BundleNuGetService _nugetService;
    private readonly IPackagingService _packagingService;
    private readonly ILogger _logger;
    private readonly string _workingDirectory;

    // Path to restored integration libraries (set during PrepareAsync)
    private string? _integrationLibsPath;

    /// <summary>
    /// Initializes a new instance of the PrebuiltAppHostServer class.
    /// </summary>
    /// <param name="appPath">The path to the user's polyglot app host.</param>
    /// <param name="socketPath">The socket path for JSON-RPC communication.</param>
    /// <param name="layout">The bundle layout configuration.</param>
    /// <param name="nugetService">The NuGet service for restoring integration packages.</param>
    /// <param name="packagingService">The packaging service for channel resolution.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public PrebuiltAppHostServer(
        string appPath,
        string socketPath,
        LayoutConfiguration layout,
        BundleNuGetService nugetService,
        IPackagingService packagingService,
        ILogger logger)
    {
        _appPath = Path.GetFullPath(appPath);
        _socketPath = socketPath;
        _layout = layout;
        _nugetService = nugetService;
        _packagingService = packagingService;
        _logger = logger;

        // Create a working directory for this app host session
        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(_appPath));
        var pathDir = Convert.ToHexString(pathHash)[..12].ToLowerInvariant();
        _workingDirectory = Path.Combine(Path.GetTempPath(), ".aspire", "bundle-hosts", pathDir);
        Directory.CreateDirectory(_workingDirectory);
    }

    /// <inheritdoc />
    public string AppPath => _appPath;

    /// <summary>
    /// Gets the path to the pre-built AppHost server (exe or DLL).
    /// </summary>
    public string GetServerPath()
    {
        var serverPath = _layout.GetAppHostServerPath();
        if (serverPath is null || !File.Exists(serverPath))
        {
            throw new InvalidOperationException("Pre-built AppHost server not found in layout.");
        }

        return serverPath;
    }

    /// <inheritdoc />
    public async Task<AppHostServerPrepareResult> PrepareAsync(
        string sdkVersion,
        IEnumerable<(string Name, string Version)> packages,
        CancellationToken cancellationToken = default)
    {
        var packageList = packages.ToList();

        try
        {
            // Generate appsettings.json with ATS assemblies for the server to scan
            await GenerateAppSettingsAsync(packageList, cancellationToken);

            // Restore integration packages
            if (packageList.Count > 0)
            {
                _logger.LogDebug("Restoring {Count} integration packages", packageList.Count);

                // Get NuGet sources from environment and channels (same as SDK mode)
                var sources = await GetNuGetSourcesAsync(cancellationToken);

                // Pass apphost directory for nuget.config discovery
                var appHostDirectory = Path.GetDirectoryName(_appPath);

                _integrationLibsPath = await _nugetService.RestorePackagesAsync(
                    packageList,
                    "net10.0",
                    sources: sources,
                    workingDirectory: appHostDirectory,
                    ct: cancellationToken);
            }

            return new AppHostServerPrepareResult(
                Success: true,
                Output: null,
                ChannelName: null,
                NeedsCodeGeneration: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare prebuilt AppHost server");
            var output = new OutputCollector();
            output.AppendError($"Failed to prepare: {ex.Message}");
            return new AppHostServerPrepareResult(
                Success: false,
                Output: output,
                ChannelName: null,
                NeedsCodeGeneration: false);
        }
    }

    /// <summary>
    /// Gets NuGet sources from package channels.
    /// This mirrors the channel resolution logic in DotNetBasedAppHostServerProject.
    /// </summary>
    private async Task<IEnumerable<string>?> GetNuGetSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = new List<string>();

        try
        {
            var channels = await _packagingService.GetChannelsAsync(cancellationToken);

            // Look for explicit channels (staging, daily, PR hives)
            foreach (var channel in channels.Where(c => c.Type == PackageChannelType.Explicit))
            {
                if (channel.Mappings is null)
                {
                    continue;
                }

                foreach (var mapping in channel.Mappings)
                {
                    if (!sources.Contains(mapping.Source, StringComparer.OrdinalIgnoreCase))
                    {
                        sources.Add(mapping.Source);
                        _logger.LogDebug("Using channel '{Channel}' NuGet source: {Source}", channel.Name, mapping.Source);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get package channels, relying on nuget.config and nuget.org fallback");
        }

        return sources.Count > 0 ? sources : null;
    }

    /// <inheritdoc />
    public (string SocketPath, Process Process, OutputCollector OutputCollector) Run(
        int hostPid,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        string[]? additionalArgs = null,
        bool debug = false)
    {
        var serverPath = GetServerPath();

        // Get runtime path for DOTNET_ROOT
        var runtimePath = _layout.GetDotNetExePath();
        var runtimeDir = runtimePath is not null ? Path.GetDirectoryName(runtimePath) : null;

        // Bundle always uses single-file executables - run directly
        var startInfo = new ProcessStartInfo(serverPath)
        {
            WorkingDirectory = _workingDirectory,
            WindowStyle = ProcessWindowStyle.Minimized,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set DOTNET_ROOT so the executable can find the runtime
        if (runtimeDir is not null)
        {
            startInfo.Environment["DOTNET_ROOT"] = runtimeDir;
            startInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        }

        // Add arguments to point to our appsettings.json
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add("--contentRoot");
        startInfo.ArgumentList.Add(_workingDirectory);

        // Add any additional arguments
        if (additionalArgs is { Length: > 0 })
        {
            foreach (var arg in additionalArgs)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        // Configure environment
        startInfo.Environment["REMOTE_APP_HOST_SOCKET_PATH"] = _socketPath;
        startInfo.Environment["REMOTE_APP_HOST_PID"] = hostPid.ToString(System.Globalization.CultureInfo.InvariantCulture);
        startInfo.Environment[KnownConfigNames.CliProcessId] = hostPid.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Also set ASPIRE_RUNTIME_PATH so DashboardEventHandlers knows which dotnet to use
        if (runtimeDir is not null)
        {
            startInfo.Environment[BundleDiscovery.RuntimePathEnvVar] = runtimeDir;
        }

        // Pass the integration libs path so the server can resolve assemblies via AssemblyLoader
        if (_integrationLibsPath is not null)
        {
            _logger.LogDebug("Setting ASPIRE_INTEGRATION_LIBS_PATH to {Path}", _integrationLibsPath);
            startInfo.Environment["ASPIRE_INTEGRATION_LIBS_PATH"] = _integrationLibsPath;
        }
        else
        {
            _logger.LogWarning("Integration libs path is null - assemblies may not resolve correctly");
        }

        // Set DCP and Dashboard paths from the layout
        var dcpPath = _layout.GetDcpPath();
        if (dcpPath is not null)
        {
            startInfo.Environment[BundleDiscovery.DcpPathEnvVar] = dcpPath;
        }

        var dashboardPath = _layout.GetDashboardPath();
        if (dashboardPath is not null)
        {
            // Bundle uses single-file executables
            var dashboardExe = Path.Combine(dashboardPath, BundleDiscovery.GetExecutableFileName(BundleDiscovery.DashboardExecutableName));
            startInfo.Environment[BundleDiscovery.DashboardPathEnvVar] = dashboardExe;
        }

        // Apply environment variables from apphost.run.json
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.Environment[key] = value;
            }
        }

        if (debug)
        {
            startInfo.Environment["Logging__LogLevel__Default"] = "Debug";
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        var process = Process.Start(startInfo)!;

        var outputCollector = new OutputCollector();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("PrebuiltAppHostServer({ProcessId}) stdout: {Line}", process.Id, e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("PrebuiltAppHostServer({ProcessId}) stderr: {Line}", process.Id, e.Data);
                outputCollector.AppendError(e.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return (_socketPath, process, outputCollector);
    }

    /// <inheritdoc />
    public string GetInstanceIdentifier() => _appPath;

    private async Task GenerateAppSettingsAsync(
        List<(string Name, string Version)> packages,
        CancellationToken cancellationToken)
    {
        // Build the list of ATS assemblies (for [AspireExport] scanning)
        // Skip SDK-only packages that don't have runtime DLLs
        var atsAssemblies = new List<string> { "Aspire.Hosting" };
        foreach (var (name, _) in packages)
        {
            // Skip SDK packages that don't produce runtime assemblies
            if (name.Equals("Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Aspire.AppHost.Sdk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!atsAssemblies.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                atsAssemblies.Add(name);
            }
        }

        var assembliesJson = string.Join(",\n      ", atsAssemblies.Select(a => $"\"{a}\""));
        var appSettingsJson = $$"""
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning",
                  "Aspire.Hosting.Dcp": "Warning"
                }
              },
              "AtsAssemblies": [
                {{assembliesJson}}
              ]
            }
            """;

        await File.WriteAllTextAsync(
            Path.Combine(_workingDirectory, "appsettings.json"),
            appSettingsJson,
            cancellationToken);
    }
}
