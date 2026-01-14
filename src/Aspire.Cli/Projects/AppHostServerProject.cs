// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for creating AppHostServerProject instances with required dependencies.
/// </summary>
internal interface IAppHostServerProjectFactory
{
    AppHostServerProject Create(string appPath);
}

/// <summary>
/// Factory implementation that creates AppHostServerProject instances with IPackagingService and IConfigurationService.
/// </summary>
internal sealed class AppHostServerProjectFactory(
    IDotNetCliRunner dotNetCliRunner,
    IPackagingService packagingService,
    IConfigurationService configurationService,
    ILogger<AppHostServerProject> logger) : IAppHostServerProjectFactory
{
    public AppHostServerProject Create(string appPath) => new AppHostServerProject(appPath, dotNetCliRunner, packagingService, configurationService, logger);
}

/// <summary>
/// Manages the AppHost server project that hosts the Aspire.Hosting runtime for polyglot apphosts.
/// This project is dynamically generated and built to provide the .NET Aspire infrastructure
/// (distributed application builder, resource management, dashboard, etc.) that polyglot apphosts
/// (TypeScript, Python, etc.) connect to via JSON-RPC to define and manage their resources.
/// </summary>
internal sealed class AppHostServerProject
{
    private const string ProjectHashFileName = ".projecthash";
    private const string FolderPrefix = ".aspire";
    private const string AppsFolder = "hosts";
    public const string ProjectFileName = "AppHostServer.csproj";
    private const string ProjectDllName = "AppHostServer.dll";
    private const string TargetFramework = "net9.0";

    public static string AspireHostVersion = Environment.GetEnvironmentVariable("ASPIRE_POLYGLOT_PACKAGE_VERSION") ?? GetEffectiveVersion();

    private static string GetEffectiveVersion()
    {
        var version = VersionHelper.GetDefaultTemplateVersion();
        // Dev versions (e.g., "13.2.0-dev") don't exist on NuGet, fall back to latest stable
        if (version.EndsWith("-dev", StringComparison.OrdinalIgnoreCase))
        {
            // Use the latest stable version available on NuGet
            // This should be updated when new stable versions are released
            return "13.1.0";
        }
        return version;
    }
    public static string? LocalPackagePath = Environment.GetEnvironmentVariable("ASPIRE_POLYGLOT_PACKAGE_SOURCE");

    /// <summary>
    /// Path to local Aspire repo root (e.g., /path/to/aspire).
    /// When set, uses direct project references instead of NuGet packages.
    /// </summary>
    public static string? LocalAspirePath = Environment.GetEnvironmentVariable("ASPIRE_REPO_ROOT");

    public const string BuildFolder = "build";
    private const string AssemblyName = "AppHostServer";
    private readonly string _projectModelPath;
    private readonly string _appPath;
    private readonly string _userSecretsId;
    private readonly IDotNetCliRunner _dotNetCliRunner;
    private readonly IPackagingService _packagingService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<AppHostServerProject> _logger;

    /// <summary>
    /// Initializes a new instance of the AppHostServerProject class.
    /// </summary>
    /// <param name="appPath">Specifies the application path for the custom language.</param>
    /// <param name="dotNetCliRunner">The .NET CLI runner for executing dotnet commands.</param>
    /// <param name="packagingService">The packaging service for channel resolution.</param>
    /// <param name="configurationService">The configuration service for reading global settings.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public AppHostServerProject(string appPath, IDotNetCliRunner dotNetCliRunner, IPackagingService packagingService, IConfigurationService configurationService, ILogger<AppHostServerProject> logger)
    {
        _appPath = Path.GetFullPath(appPath);
        _appPath = new Uri(_appPath).LocalPath;
        _appPath = OperatingSystem.IsWindows() ? _appPath.ToLowerInvariant() : _appPath;
        _dotNetCliRunner = dotNetCliRunner;
        _packagingService = packagingService;
        _configurationService = configurationService;
        _logger = logger;

        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(_appPath));
        var pathDir = Convert.ToHexString(pathHash)[..12].ToLowerInvariant();
        _projectModelPath = Path.Combine(Path.GetTempPath(), FolderPrefix, AppsFolder, pathDir);

        // Create a stable UserSecretsId based on the app path hash
        _userSecretsId = new Guid(pathHash[..16]).ToString();

        Directory.CreateDirectory(_projectModelPath);
    }

    public string ProjectModelPath => _projectModelPath;
    public string AppPath => _appPath;
    public string UserSecretsId => _userSecretsId;
    public string BuildPath => Path.Combine(_projectModelPath, BuildFolder);

    /// <summary>
    /// Gets the full path to the AppHost server project file.
    /// </summary>
    public string GetProjectFilePath() => Path.Combine(_projectModelPath, ProjectFileName);

    public string GetProjectHash()
    {
        var hashFilePath = Path.Combine(_projectModelPath, ProjectHashFileName);

        if (File.Exists(hashFilePath))
        {
            return File.ReadAllText(hashFilePath);
        }

        return string.Empty;
    }

    public void SaveProjectHash(string hash)
    {
        var hashFilePath = Path.Combine(_projectModelPath, ProjectHashFileName);
        File.WriteAllText(hashFilePath, hash);
    }

    /// <summary>
    /// Scaffolds the project files.
    /// </summary>
    /// <param name="packages">The package references to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the full path to the project file and the channel name used (if any).</returns>
    public async Task<(string ProjectPath, string? ChannelName)> CreateProjectFilesAsync(IEnumerable<(string Name, string Version)> packages, CancellationToken cancellationToken = default)
    {
        // Create Program.cs that starts the RemoteHost server
        // The server reads AtsAssemblies from appsettings.json to load integration assemblies
        var programCs = """
            await Aspire.Hosting.RemoteHost.RemoteHostServer.RunAsync(args);
            """;

        File.WriteAllText(Path.Combine(_projectModelPath, "Program.cs"), programCs);

        // Create appsettings.json with the list of ATS assemblies
        // These are the assemblies that will be scanned for [AspireExport] capabilities
        // Include all packages since any package could contribute capabilities via [AspireExport]
        var atsAssemblies = new List<string> { "Aspire.Hosting" };
        foreach (var pkg in packages)
        {
            if (!atsAssemblies.Contains(pkg.Name, StringComparer.OrdinalIgnoreCase))
            {
                atsAssemblies.Add(pkg.Name);
            }
        }
        // Add the TypeScript code generator assembly for code generation support
        atsAssemblies.Add("Aspire.Hosting.CodeGeneration.TypeScript");

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

        var appSettingsJsonPath = Path.Combine(_projectModelPath, "appsettings.json");
        File.WriteAllText(appSettingsJsonPath, appSettingsJson);

        // Handle NuGet.config:
        // 1. If local package source is specified (dev scenario), create a config that includes it
        // 2. Otherwise, use NuGetConfigMerger to create/update config based on channel (same pattern as aspire new/init)
        var nugetConfigPath = Path.Combine(_projectModelPath, "NuGet.config");
        string? channelName = null;

        if (LocalPackagePath is not null)
        {
            var nugetConfig = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                    <packageSources>
                        <add key="local" value="{LocalPackagePath.Replace("\\", "/")}" />
                    </packageSources>
                </configuration>
                """;
            File.WriteAllText(nugetConfigPath, nugetConfig);
        }
        else
        {
            // First, copy user's NuGet.config if it exists (to preserve private feeds/auth)
            var userNugetConfig = FindNuGetConfig(_appPath);
            if (userNugetConfig is not null)
            {
                File.Copy(userNugetConfig, nugetConfigPath, overwrite: true);
            }

            // Get the appropriate channel from the packaging service (same logic as aspire new/init)
            var channels = await _packagingService.GetChannelsAsync(cancellationToken);

            // Check for global channel setting (same as aspire new/init)
            var configuredChannelName = await _configurationService.GetConfigurationAsync("channel", cancellationToken);

            PackageChannel? channel;
            if (!string.IsNullOrEmpty(configuredChannelName))
            {
                // Use the configured channel if specified
                channel = channels.FirstOrDefault(c => string.Equals(c.Name, configuredChannelName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Fall back to first explicit channel (staging/PR)
                channel = channels.FirstOrDefault(c => c.Type == PackageChannelType.Explicit);
            }

            // NuGetConfigMerger creates or updates the config with channel sources/mappings
            if (channel is not null)
            {
                await NuGetConfigMerger.CreateOrUpdateAsync(
                    new DirectoryInfo(_projectModelPath),
                    channel,
                    cancellationToken: cancellationToken);

                // Track the channel name to return to caller
                channelName = channel.Name;
            }
        }

        // Note: We don't create launchSettings.json here. Environment variables
        // (ports, OTLP endpoints, etc.) are read from the user's apphost.run.json
        // and passed directly to Run() at runtime.

        // Create the project file
        string template;

        if (LocalAspirePath is not null)
        {
            // Local build: use project references like the playground
            var repoRoot = Path.GetFullPath(LocalAspirePath) + Path.DirectorySeparatorChar;

            // Determine OS/architecture for DCP package name (matches Directory.Build.props logic)
            var (buildOs, buildArch) = GetBuildPlatform();
            var dcpPackageName = $"microsoft.developercontrolplane.{buildOs}-{buildArch}";

            // Read DCP version from eng/Versions.props in the repo
            var dcpVersion = GetDcpVersionFromRepo(repoRoot, buildOs, buildArch);

            template = $"""
                <Project Sdk="Microsoft.NET.Sdk">

                    <PropertyGroup>
                        <OutputType>exe</OutputType>
                        <TargetFramework>{TargetFramework}</TargetFramework>
                        <AssemblyName>{AssemblyName}</AssemblyName>
                        <OutDir>{BuildFolder}</OutDir>
                        <UserSecretsId>{_userSecretsId}</UserSecretsId>
                        <IsAspireHost>true</IsAspireHost>
                        <IsPublishable>false</IsPublishable>
                        <SelfContained>false</SelfContained>
                        <ImplicitUsings>enable</ImplicitUsings>
                        <Nullable>enable</Nullable>
                        <WarningLevel>0</WarningLevel>
                        <EnableNETAnalyzers>false</EnableNETAnalyzers>
                        <EnableRoslynAnalyzers>false</EnableRoslynAnalyzers>
                        <RunAnalyzers>false</RunAnalyzers>
                        <NoWarn>$(NoWarn);1701;1702;1591;CS8019;CS1591;CS1573;CS0168;CS0219;CS8618;CS8625;CS1998;CS1999</NoWarn>
                        <!-- Properties for in-repo building (from Aspire.RepoTesting.targets) -->
                        <RepoRoot>{repoRoot}</RepoRoot>
                        <SkipValidateAspireHostProjectResources>true</SkipValidateAspireHostProjectResources>
                        <SkipAddAspireDefaultReferences>true</SkipAddAspireDefaultReferences>
                        <AspireHostingSDKVersion>42.42.42</AspireHostingSDKVersion>
                        <!-- DCP and Dashboard paths for local development (same as Directory.Build.props) -->
                        <DcpDir>$(NuGetPackageRoot){dcpPackageName}/{dcpVersion}/tools/</DcpDir>
                        <AspireDashboardDir>{repoRoot}artifacts/bin/Aspire.Dashboard/Debug/net8.0/</AspireDashboardDir>
                    </PropertyGroup>
                    <ItemGroup>
                        <PackageReference Include="StreamJsonRpc" Version="2.22.23" />
                        <!-- Pin Google.Protobuf to match Aspire.Hosting's version to avoid conflicts -->
                        <PackageReference Include="Google.Protobuf" Version="3.33.0" />
                    </ItemGroup>
                </Project>
                """;
        }
        else
        {
            // Standard NuGet flow with Aspire.AppHost.Sdk
            template = $"""
                <Project Sdk="Microsoft.NET.Sdk">

                    <Sdk Name="Aspire.AppHost.Sdk" Version="{AspireHostVersion}" />

                    <PropertyGroup>
                        <OutputType>exe</OutputType>
                        <TargetFramework>{TargetFramework}</TargetFramework>
                        <AssemblyName>{AssemblyName}</AssemblyName>
                        <OutDir>{BuildFolder}</OutDir>
                        <UserSecretsId>{_userSecretsId}</UserSecretsId>
                        <IsAspireHost>true</IsAspireHost>
                        <IsPublishable>true</IsPublishable>
                        <SelfContained>true</SelfContained>
                        <ImplicitUsings>enable</ImplicitUsings>
                        <Nullable>enable</Nullable>
                        <WarningLevel>0</WarningLevel>
                        <EnableNETAnalyzers>false</EnableNETAnalyzers>
                        <EnableRoslynAnalyzers>false</EnableRoslynAnalyzers>
                        <RunAnalyzers>false</RunAnalyzers>
                        <NoWarn>$(NoWarn);1701;1702;1591;CS8019;CS1591;CS1573;CS0168;CS0219;CS8618;CS8625;CS1998;CS1999</NoWarn>
                    </PropertyGroup>
                    <ItemGroup>
                        <PackageReference Include="StreamJsonRpc" Version="2.22.23" />
                    </ItemGroup>
                    <!-- Disable Aspire SDK code generation - we don't need project metadata for the AppHost server -->
                    <Target Name="_CSharpWriteHostProjectMetadataSources" />
                    <Target Name="_CSharpWriteProjectMetadataSources" />
                </Project>
                """;
        }

        var doc = XDocument.Parse(template);

        // Check if using local build (project references for faster dev loop)
        if (LocalAspirePath is not null)
        {
            var repoRoot = Path.GetFullPath(LocalAspirePath);

            var projectRefGroup = new XElement("ItemGroup");
            var addedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var otherPackages = new List<(string Name, string Version)>();

            foreach (var pkg in packages)
            {
                if (!pkg.Name.StartsWith("Aspire.Hosting", StringComparison.OrdinalIgnoreCase))
                {
                    otherPackages.Add(pkg);
                    continue;
                }

                // Skip if already added
                if (addedProjects.Contains(pkg.Name))
                {
                    continue;
                }

                // Look for the project in src/ or src/Components/ (same as AspireProjectOrPackageReference)
                var candidatePaths = new[]
                {
                    Path.Combine(repoRoot, "src", "Components", pkg.Name, $"{pkg.Name}.csproj"),
                    Path.Combine(repoRoot, "src", pkg.Name, $"{pkg.Name}.csproj")
                };

                var projectPath = candidatePaths.FirstOrDefault(File.Exists);
                if (projectPath is not null)
                {
                    addedProjects.Add(pkg.Name);
                    projectRefGroup.Add(new XElement("ProjectReference",
                        new XAttribute("Include", projectPath),
                        new XElement("IsAspireProjectResource", "false")));
                }
                else
                {
                    // Fallback to NuGet package if project not found
                    _logger.LogWarning("Could not find local project for {PackageName}, falling back to NuGet", pkg.Name);
                    otherPackages.Add(pkg);
                }
            }

            if (projectRefGroup.HasElements)
            {
                doc.Root!.Add(projectRefGroup);
            }

            if (otherPackages.Count > 0)
            {
                doc.Root!.Add(new XElement("ItemGroup",
                    otherPackages.Select(p => new XElement("PackageReference",
                        new XAttribute("Include", p.Name),
                        new XAttribute("Version", p.Version)))));
            }

            // Add imports for in-repo AppHost building (from Aspire.RepoTesting.targets)
            var appHostInTargets = Path.Combine(repoRoot, "src", "Aspire.Hosting.AppHost", "build", "Aspire.Hosting.AppHost.in.targets");
            var sdkInTargets = Path.Combine(repoRoot, "src", "Aspire.AppHost.Sdk", "SDK", "Sdk.in.targets");

            if (File.Exists(appHostInTargets))
            {
                doc.Root!.Add(new XElement("Import", new XAttribute("Project", appHostInTargets)));
            }
            if (File.Exists(sdkInTargets))
            {
                doc.Root!.Add(new XElement("Import", new XAttribute("Project", sdkInTargets)));
            }

            // Add Dashboard project reference (like playground does)
            var dashboardProject = Path.Combine(repoRoot, "src", "Aspire.Dashboard", "Aspire.Dashboard.csproj");
            if (File.Exists(dashboardProject))
            {
                doc.Root!.Add(new XElement("ItemGroup",
                    new XElement("ProjectReference",
                        new XAttribute("Include", dashboardProject))));
            }

            // Add Aspire.Hosting.RemoteHost project reference
            var remoteHostProject = Path.Combine(repoRoot, "src", "Aspire.Hosting.RemoteHost", "Aspire.Hosting.RemoteHost.csproj");
            if (File.Exists(remoteHostProject))
            {
                doc.Root!.Add(new XElement("ItemGroup",
                    new XElement("ProjectReference",
                        new XAttribute("Include", remoteHostProject))));
            }

            // Add Aspire.Hosting.CodeGeneration.TypeScript project reference for code generation
            var typeScriptCodeGenProject = Path.Combine(repoRoot, "src", "Aspire.Hosting.CodeGeneration.TypeScript", "Aspire.Hosting.CodeGeneration.TypeScript.csproj");
            if (File.Exists(typeScriptCodeGenProject))
            {
                doc.Root!.Add(new XElement("ItemGroup",
                    new XElement("ProjectReference",
                        new XAttribute("Include", typeScriptCodeGenProject))));
            }

            // Disable Aspire SDK code generation - we don't need project metadata for the AppHost server
            // These must come after the imports to override the targets defined there
            doc.Root!.Add(new XElement("Target", new XAttribute("Name", "_CSharpWriteHostProjectMetadataSources")));
            doc.Root!.Add(new XElement("Target", new XAttribute("Name", "_CSharpWriteProjectMetadataSources")));
        }
        else
        {
            // Add package references (standard NuGet flow)
            var packageRefs = packages.Select(p => new XElement("PackageReference",
                new XAttribute("Include", p.Name),
                new XAttribute("Version", p.Version))).ToList();

            // Add Aspire.Hosting.RemoteHost package reference
            packageRefs.Add(new XElement("PackageReference",
                new XAttribute("Include", "Aspire.Hosting.RemoteHost"),
                new XAttribute("Version", AspireHostVersion)));

            // Add Aspire.Hosting.CodeGeneration.TypeScript package for code generation
            packageRefs.Add(new XElement("PackageReference",
                new XAttribute("Include", "Aspire.Hosting.CodeGeneration.TypeScript"),
                new XAttribute("Version", AspireHostVersion)));

            doc.Root!.Add(new XElement("ItemGroup", packageRefs));
        }

        // Add appsettings.json to be copied to output directory
        // This is required for the RemoteHostServer to find AtsAssemblies configuration
        doc.Root!.Add(new XElement("ItemGroup",
            new XElement("None",
                new XAttribute("Include", "appsettings.json"),
                new XAttribute("CopyToOutputDirectory", "PreserveNewest"))));

        var projectFileName = Path.Combine(_projectModelPath, ProjectFileName);
        doc.Save(projectFileName);

        return (projectFileName, channelName);
    }

    /// <summary>
    /// Restores and builds the project dependencies.
    /// </summary>
    /// <returns>A tuple containing the success status and an OutputCollector with build output.</returns>
    public async Task<(bool Success, OutputCollector Output)> BuildAsync(CancellationToken cancellationToken = default)
    {
        var outputCollector = new OutputCollector();
        var projectFile = new FileInfo(Path.Combine(_projectModelPath, ProjectFileName));

        var options = new DotNetCliRunnerInvocationOptions
        {
            StandardOutputCallback = outputCollector.AppendOutput,
            StandardErrorCallback = outputCollector.AppendError
        };

        var exitCode = await _dotNetCliRunner.BuildAsync(projectFile, options, cancellationToken);

        return (exitCode == 0, outputCollector);
    }

    /// <summary>
    /// Runs the AppHost server.
    /// </summary>
    /// <param name="socketPath">The Unix domain socket path for JSON-RPC communication.</param>
    /// <param name="hostPid">The PID of the host process for orphan detection.</param>
    /// <param name="launchSettingsEnvVars">Optional environment variables from apphost.run.json or launchSettings.json.</param>
    /// <param name="additionalArgs">Optional additional command-line arguments (e.g., for publish/deploy).</param>
    /// <param name="debug">Whether to enable debug logging in the AppHost server.</param>
    /// <returns>A tuple containing the started process and an OutputCollector for capturing output.</returns>
    public (Process Process, OutputCollector OutputCollector) Run(string socketPath, int hostPid, IReadOnlyDictionary<string, string>? launchSettingsEnvVars = null, string[]? additionalArgs = null, bool debug = false)
    {
        var assemblyPath = Path.Combine(BuildPath, ProjectDllName);
        var dotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        var startInfo = new ProcessStartInfo(dotnetExe)
        {
            WorkingDirectory = _projectModelPath,
            WindowStyle = ProcessWindowStyle.Minimized,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(assemblyPath);

        // Add the separator and any additional arguments (for publish/deploy)
        if (additionalArgs is { Length: > 0 })
        {
            startInfo.ArgumentList.Add("--");
            foreach (var arg in additionalArgs)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        // Pass environment variables for socket path and parent PID
        startInfo.Environment["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath;
        startInfo.Environment["REMOTE_APP_HOST_PID"] = hostPid.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // Pass the original apphost project directory so resources resolve paths correctly
        startInfo.Environment["ASPIRE_PROJECT_DIRECTORY"] = _appPath;

        // Apply environment variables from apphost.run.json / launchSettings.json
        if (launchSettingsEnvVars != null)
        {
            foreach (var (key, value) in launchSettingsEnvVars)
            {
                startInfo.Environment[key] = value;
            }
        }

        // Enable debug logging if requested
        if (debug)
        {
            startInfo.Environment["Logging__LogLevel__Default"] = "Debug";
            _logger.LogDebug("Enabling debug logging for AppHostServer");
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        var process = Process.Start(startInfo)!;

        // Collect output for error diagnostics and log at debug level
        var outputCollector = new OutputCollector();
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("AppHostServer({ProcessId}) stdout: {Line}", process.Id, e.Data);
                outputCollector.AppendOutput(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                _logger.LogDebug("AppHostServer({ProcessId}) stderr: {Line}", process.Id, e.Data);
                outputCollector.AppendError(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return (process, outputCollector);
    }

    /// <summary>
    /// Gets the socket path for the AppHost server based on the app path.
    /// </summary>
    public string GetSocketPath()
    {
        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(_appPath));
        var socketName = Convert.ToHexString(pathHash)[..12].ToLowerInvariant() + ".sock";

        var socketDir = Path.Combine(Path.GetTempPath(), FolderPrefix, "sockets");
        Directory.CreateDirectory(socketDir);

        return Path.Combine(socketDir, socketName);
    }

    /// <summary>
    /// Gets a project-level NuGet config path using dotnet nuget config paths command.
    /// Only returns configs that are within the project directory tree, not global user configs.
    /// </summary>
    private static string? FindNuGetConfig(string workingDirectory)
    {
        try
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                Arguments = "nuget config paths",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return null;
            }

            // Find a config that's in the project directory or a parent directory (not global user config).
            // Global configs (e.g., ~/.nuget/NuGet/NuGet.Config) will be found by dotnet anyway.
            var configPaths = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var workingDirFullPath = Path.GetFullPath(workingDirectory);

            // Get user profile path to exclude global NuGet configs
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var globalNuGetPath = Path.Combine(userProfile, ".nuget");

            foreach (var configPath in configPaths)
            {
                if (File.Exists(configPath))
                {
                    var configFullPath = Path.GetFullPath(configPath);
                    var configDir = Path.GetDirectoryName(configFullPath);

                    // Skip global NuGet configs (they're in ~/.nuget)
                    if (configFullPath.StartsWith(globalNuGetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Check if the working directory is within or below the config's directory
                    // (i.e., the config is in a parent directory of the project)
                    if (configDir is not null && workingDirFullPath.StartsWith(configDir, StringComparison.OrdinalIgnoreCase))
                    {
                        return configPath;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the OS and architecture identifiers for the DCP package name.
    /// </summary>
    private static (string Os, string Arch) GetBuildPlatform()
    {
        // OS mapping (matches MSBuild logic in Directory.Build.props)
        var os = OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsMacOS() ? "darwin"
            : "windows";

        // Architecture mapping
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => "386",
            Architecture.Arm64 => "arm64",
            _ => "amd64"
        };

        return (os, arch);
    }

    /// <summary>
    /// Reads the DCP version from eng/Versions.props in the repo.
    /// </summary>
    private static string GetDcpVersionFromRepo(string repoRoot, string buildOs, string buildArch)
    {
        const string fallbackVersion = "0.21.1";

        try
        {
            var versionsPropsPath = Path.Combine(repoRoot, "eng", "Versions.props");
            if (!File.Exists(versionsPropsPath))
            {
                return fallbackVersion;
            }

            var doc = XDocument.Load(versionsPropsPath);

            // Property name format: MicrosoftDeveloperControlPlane{os}{arch}Version
            // e.g., MicrosoftDeveloperControlPlanedarwinarm64Version
            var propertyName = $"MicrosoftDeveloperControlPlane{buildOs}{buildArch}Version";

            var version = doc.Descendants(propertyName).FirstOrDefault()?.Value;
            return version ?? fallbackVersion;
        }
        catch
        {
            return fallbackVersion;
        }
    }
}
