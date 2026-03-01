// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
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
    private readonly IDotNetCliRunner _dotNetCliRunner;
    private readonly IPackagingService _packagingService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger _logger;
    private readonly string _workingDirectory;

    // Path to restored integration libraries (set during PrepareAsync)
    private string? _integrationLibsPath;

    /// <summary>
    /// Initializes a new instance of the PrebuiltAppHostServer class.
    /// </summary>
    public PrebuiltAppHostServer(
        string appPath,
        string socketPath,
        LayoutConfiguration layout,
        BundleNuGetService nugetService,
        IDotNetCliRunner dotNetCliRunner,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        ILogger logger)
    {
        _appPath = Path.GetFullPath(appPath);
        _socketPath = socketPath;
        _layout = layout;
        _nugetService = nugetService;
        _dotNetCliRunner = dotNetCliRunner;
        _packagingService = packagingService;
        _configurationService = configurationService;
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
    /// Gets the path to the aspire-managed executable (used as the server).
    /// </summary>
    public string GetServerPath()
    {
        var managedPath = _layout.GetManagedPath();
        if (managedPath is null || !File.Exists(managedPath))
        {
            throw new InvalidOperationException("aspire-managed not found in layout.");
        }

        return managedPath;
    }

    /// <inheritdoc />
    public async Task<AppHostServerPrepareResult> PrepareAsync(
        string sdkVersion,
        IEnumerable<IntegrationReference> integrations,
        CancellationToken cancellationToken = default)
    {
        var integrationList = integrations.ToList();
        var packageRefs = integrationList.Where(r => r.IsPackageReference).ToList();
        var projectRefs = integrationList.Where(r => r.IsProjectReference).ToList();

        try
        {
            // Generate appsettings.json with ATS assemblies for the server to scan
            await GenerateAppSettingsAsync(integrationList, cancellationToken);

            // Resolve the configured channel (local settings.json → global config fallback)
            var channelName = await ResolveChannelNameAsync(cancellationToken);

            if (projectRefs.Count > 0)
            {
                // Project references require .NET SDK — create a single synthetic project
                // with all package and project references, then dotnet publish once.
                _integrationLibsPath = await PublishIntegrationProjectAsync(
                    packageRefs, projectRefs, channelName, cancellationToken);
            }
            else if (packageRefs.Count > 0)
            {
                // NuGet-only — use the bundled NuGet service (no SDK required)
                _integrationLibsPath = await RestoreNuGetPackagesAsync(
                    packageRefs, channelName, cancellationToken);
            }

            return new AppHostServerPrepareResult(
                Success: true,
                Output: null,
                ChannelName: channelName,
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
    /// Restores NuGet packages using the bundled NuGet service (no .NET SDK required).
    /// </summary>
    private async Task<string> RestoreNuGetPackagesAsync(
        List<IntegrationReference> packageRefs,
        string? channelName,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Restoring {Count} integration packages via bundled NuGet", packageRefs.Count);

        var packages = packageRefs.Select(r => (r.Name, r.Version!)).ToList();
        var sources = await GetNuGetSourcesAsync(channelName, cancellationToken);
        var appHostDirectory = Path.GetDirectoryName(_appPath);

        return await _nugetService.RestorePackagesAsync(
            packages,
            "net10.0",
            sources: sources,
            workingDirectory: appHostDirectory,
            ct: cancellationToken);
    }

    /// <summary>
    /// Creates a synthetic .csproj with all package and project references,
    /// then publishes it to get the full transitive DLL closure. Requires .NET SDK.
    /// </summary>
    private async Task<string> PublishIntegrationProjectAsync(
        List<IntegrationReference> packageRefs,
        List<IntegrationReference> projectRefs,
        string? channelName,
        CancellationToken cancellationToken)
    {
        var restoreDir = Path.Combine(_workingDirectory, "integration-restore");
        Directory.CreateDirectory(restoreDir);

        var projectContent = GenerateIntegrationProjectFile(packageRefs, projectRefs);
        var projectFilePath = Path.Combine(restoreDir, "IntegrationRestore.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent, cancellationToken);

        // Copy nuget.config from the user's apphost directory if present
        var appHostDirectory = Path.GetDirectoryName(_appPath)!;
        var userNugetConfig = Path.Combine(appHostDirectory, "nuget.config");
        if (File.Exists(userNugetConfig))
        {
            File.Copy(userNugetConfig, Path.Combine(restoreDir, "nuget.config"), overwrite: true);
        }

        // Merge channel-specific NuGet sources if a channel is configured
        if (channelName is not null)
        {
            try
            {
                var channels = await _packagingService.GetChannelsAsync(cancellationToken);
                var channel = channels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));
                if (channel is not null)
                {
                    await NuGetConfigMerger.CreateOrUpdateAsync(
                        new DirectoryInfo(restoreDir),
                        channel,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to merge channel NuGet sources, relying on user nuget.config");
            }
        }

        var publishDir = new DirectoryInfo(Path.Combine(restoreDir, "publish"));
        Directory.CreateDirectory(publishDir.FullName);

        _logger.LogDebug("Publishing integration project with {PackageCount} packages and {ProjectCount} project references to {OutputDir}",
            packageRefs.Count, projectRefs.Count, publishDir.FullName);

        var exitCode = await _dotNetCliRunner.PublishProjectAsync(
            new FileInfo(projectFilePath),
            publishDir,
            new DotNetCliRunnerInvocationOptions(),
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to publish integration project. Exit code: {exitCode}");
        }

        return publishDir.FullName;
    }

    /// <summary>
    /// Generates a synthetic .csproj file that references all integration packages and projects.
    /// Publishing this project produces the full transitive DLL closure.
    /// </summary>
    private static string GenerateIntegrationProjectFile(
        List<IntegrationReference> packageRefs,
        List<IntegrationReference> projectRefs)
    {
        var packageElements = string.Join("\n    ",
            packageRefs.Select(p => $"""<PackageReference Include="{p.Name}" Version="{p.Version}" />"""));

        var projectElements = string.Join("\n    ",
            projectRefs.Select(p => $"""<ProjectReference Include="{p.ProjectPath}" />"""));

        return $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <EnableDefaultItems>false</EnableDefaultItems>
              </PropertyGroup>
              <ItemGroup>
                {{packageElements}}
              </ItemGroup>
              <ItemGroup>
                {{projectElements}}
              </ItemGroup>
            </Project>
            """;
    }

    /// <summary>
    /// Resolves the configured channel name from local settings.json or global config.
    /// </summary>
    private async Task<string?> ResolveChannelNameAsync(CancellationToken cancellationToken)
    {
        // Check local settings.json first
        var localConfig = AspireJsonConfiguration.Load(Path.GetDirectoryName(_appPath)!);
        var channelName = localConfig?.Channel;

        // Fall back to global config
        if (string.IsNullOrEmpty(channelName))
        {
            channelName = await _configurationService.GetConfigurationAsync("channel", cancellationToken);
        }

        if (!string.IsNullOrEmpty(channelName))
        {
            _logger.LogDebug("Resolved channel: {Channel}", channelName);
        }

        return channelName;
    }

    /// <summary>
    /// Gets NuGet sources from the resolved channel for bundled restore.
    /// </summary>
    private async Task<IEnumerable<string>?> GetNuGetSourcesAsync(string? channelName, CancellationToken cancellationToken)
    {
        var sources = new List<string>();

        try
        {
            var channels = await _packagingService.GetChannelsAsync(cancellationToken);

            IEnumerable<PackageChannel> explicitChannels;
            if (!string.IsNullOrEmpty(channelName))
            {
                var matchingChannel = channels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));
                explicitChannels = matchingChannel is not null ? [matchingChannel] : channels.Where(c => c.Type == PackageChannelType.Explicit);
            }
            else
            {
                explicitChannels = channels.Where(c => c.Type == PackageChannelType.Explicit);
            }

            foreach (var channel in explicitChannels)
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

        // aspire-managed is self-contained - run directly
        var startInfo = new ProcessStartInfo(serverPath)
        {
            WorkingDirectory = _workingDirectory,
            WindowStyle = ProcessWindowStyle.Minimized,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Insert "server" subcommand, then remaining args
        startInfo.ArgumentList.Add("server");
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

        // Set the dashboard path so the AppHost can locate and launch the dashboard binary
        var managedPath = _layout.GetManagedPath();
        if (managedPath is not null)
        {
            startInfo.Environment[BundleDiscovery.DashboardPathEnvVar] = managedPath;
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
        List<IntegrationReference> integrations,
        CancellationToken cancellationToken)
    {
        // Build the list of ATS assemblies (for [AspireExport] scanning)
        // Skip SDK-only packages that don't have runtime DLLs
        var atsAssemblies = new List<string> { "Aspire.Hosting" };
        foreach (var integration in integrations)
        {
            // Skip SDK packages that don't produce runtime assemblies
            if (integration.Name.Equals("Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase) ||
                integration.Name.StartsWith("Aspire.AppHost.Sdk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!atsAssemblies.Contains(integration.Name, StringComparer.OrdinalIgnoreCase))
            {
                atsAssemblies.Add(integration.Name);
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
