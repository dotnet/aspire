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
        var packageRefs = integrationList.Where(r => r.IsPackageReference).Select(r => (r.Name, r.Version!)).ToList();
        var projectRefs = integrationList.Where(r => r.IsProjectReference).ToList();

        try
        {
            // Generate appsettings.json with ATS assemblies for the server to scan
            await GenerateAppSettingsAsync(integrationList, cancellationToken);

            // Resolve the configured channel (local settings.json → global config fallback)
            var channelName = await ResolveChannelNameAsync(cancellationToken);

            // Restore NuGet integration packages
            if (packageRefs.Count > 0)
            {
                _logger.LogDebug("Restoring {Count} integration packages", packageRefs.Count);

                // Get NuGet sources filtered to the resolved channel
                var sources = await GetNuGetSourcesAsync(channelName, cancellationToken);

                // Pass apphost directory for nuget.config discovery
                var appHostDirectory = Path.GetDirectoryName(_appPath);

                _integrationLibsPath = await _nugetService.RestorePackagesAsync(
                    packageRefs,
                    "net10.0",
                    sources: sources,
                    workingDirectory: appHostDirectory,
                    ct: cancellationToken);
            }

            // Build and publish project references
            if (projectRefs.Count > 0)
            {
                _logger.LogDebug("Building {Count} project references", projectRefs.Count);

                // Ensure we have a libs directory to copy into
                if (_integrationLibsPath is null)
                {
                    _integrationLibsPath = Path.Combine(Path.GetTempPath(), ".aspire", "project-refs", Guid.NewGuid().ToString("N")[..12]);
                    Directory.CreateDirectory(_integrationLibsPath);
                }

                foreach (var projectRef in projectRefs)
                {
                    var projectFile = new FileInfo(projectRef.ProjectPath!);
                    if (!projectFile.Exists)
                    {
                        _logger.LogError("Project reference not found: {Path}", projectRef.ProjectPath);
                        throw new FileNotFoundException($"Project reference not found: {projectRef.ProjectPath}");
                    }

                    // Publish to a temp directory to get the full transitive closure
                    var publishDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), ".aspire", "publish", Guid.NewGuid().ToString("N")[..12]));
                    Directory.CreateDirectory(publishDir.FullName);

                    _logger.LogDebug("Publishing project reference {Name} from {Path} to {OutputDir}", projectRef.Name, projectRef.ProjectPath, publishDir.FullName);

                    var exitCode = await _dotNetCliRunner.PublishProjectAsync(
                        projectFile,
                        publishDir,
                        new DotNetCliRunnerInvocationOptions(),
                        cancellationToken);

                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException($"Failed to publish project reference '{projectRef.Name}' at {projectRef.ProjectPath}. Exit code: {exitCode}");
                    }

                    // Copy all DLLs from publish output into the integration libs path
                    foreach (var dll in Directory.GetFiles(publishDir.FullName, "*.dll"))
                    {
                        var destPath = Path.Combine(_integrationLibsPath, Path.GetFileName(dll));
                        File.Copy(dll, destPath, overwrite: true);
                    }
                }
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
    /// Gets NuGet sources from the resolved channel, or all explicit channels if no channel is configured.
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
                // Filter to the configured channel
                var matchingChannel = channels.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));
                explicitChannels = matchingChannel is not null ? [matchingChannel] : channels.Where(c => c.Type == PackageChannelType.Explicit);
            }
            else
            {
                // No channel configured, use all explicit channels
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
