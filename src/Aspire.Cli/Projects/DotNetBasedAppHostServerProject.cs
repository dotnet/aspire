// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

/// <summary>
/// Base class for AppHost server projects that use the .NET SDK to build.
/// Provides common functionality for project generation, building, and running.
/// </summary>
internal abstract class DotNetBasedAppHostServerProject : IAppHostServerProject
{
    private const string ProjectHashFileName = ".projecthash";
    private const string FolderPrefix = ".aspire";
    private const string AppsFolder = "hosts";
    public const string ProjectFileName = "AppHostServer.csproj";
    private const string ProjectDllName = "AppHostServer.dll";
    protected const string TargetFramework = "net10.0";
    public const string BuildFolder = "build";
    protected const string AssemblyName = "AppHostServer";

    /// <summary>
    /// Gets the default Aspire SDK version based on the CLI version.
    /// </summary>
    public static string DefaultSdkVersion => GetEffectiveVersion();

    private static string GetEffectiveVersion()
    {
        var version = VersionHelper.GetDefaultTemplateVersion();

        // Strip the commit SHA suffix (e.g., "9.2.0+abc123" -> "9.2.0")
        var plusIndex = version.IndexOf('+');
        if (plusIndex > 0)
        {
            version = version[..plusIndex];
        }

        // Dev versions (e.g., "13.2.0-dev") don't exist on NuGet, fall back to latest stable
        if (version.EndsWith("-dev", StringComparison.OrdinalIgnoreCase))
        {
            return "13.1.0";
        }
        return version;
    }

    protected readonly string _projectModelPath;
    protected readonly string _appPath;
    protected readonly string _socketPath;
    protected readonly string _userSecretsId;
    protected readonly IDotNetCliRunner _dotNetCliRunner;
    protected readonly IPackagingService _packagingService;
    protected readonly IConfigurationService _configurationService;
    protected readonly ILogger _logger;

    protected DotNetBasedAppHostServerProject(
        string appPath,
        string socketPath,
        IDotNetCliRunner dotNetCliRunner,
        IPackagingService packagingService,
        IConfigurationService configurationService,
        ILogger logger,
        string? projectModelPath = null)
    {
        _appPath = Path.GetFullPath(appPath);
        _appPath = new Uri(_appPath).LocalPath;
        _appPath = OperatingSystem.IsWindows() ? _appPath.ToLowerInvariant() : _appPath;
        _socketPath = socketPath;
        _dotNetCliRunner = dotNetCliRunner;
        _packagingService = packagingService;
        _configurationService = configurationService;
        _logger = logger;

        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(_appPath));

        if (projectModelPath is not null)
        {
            _projectModelPath = projectModelPath;
        }
        else
        {
            var pathDir = Convert.ToHexString(pathHash)[..12].ToLowerInvariant();
            _projectModelPath = Path.Combine(Path.GetTempPath(), FolderPrefix, AppsFolder, pathDir);
        }

        // Create a stable UserSecretsId based on the app path hash
        _userSecretsId = new Guid(pathHash[..16]).ToString();

        Directory.CreateDirectory(_projectModelPath);
    }

    /// <inheritdoc />
    public string AppPath => _appPath;

    public string ProjectModelPath => _projectModelPath;
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
    /// Creates the project-specific .csproj content.
    /// </summary>
    protected abstract XDocument CreateProjectFile(string sdkVersion, IEnumerable<(string Name, string Version)> packages);

    /// <summary>
    /// Scaffolds the project files.
    /// </summary>
    public async Task<(string ProjectPath, string? ChannelName)> CreateProjectFilesAsync(
        string sdkVersion,
        IEnumerable<(string Name, string Version)> packages,
        CancellationToken cancellationToken = default,
        IEnumerable<string>? additionalProjectReferences = null)
    {
        // Clean obj folder to ensure fresh NuGet restore
        var objPath = Path.Combine(_projectModelPath, "obj");
        if (Directory.Exists(objPath))
        {
            try
            {
                Directory.Delete(objPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to delete obj folder at {ObjPath}", objPath);
            }
        }

        // Create Program.cs
        var programCs = """
            await Aspire.Hosting.RemoteHost.RemoteHostServer.RunAsync(args);
            """;
        File.WriteAllText(Path.Combine(_projectModelPath, "Program.cs"), programCs);

        // Create appsettings.json with ATS assemblies
        var atsAssemblies = new List<string> { "Aspire.Hosting" };
        foreach (var pkg in packages)
        {
            if (!atsAssemblies.Contains(pkg.Name, StringComparer.OrdinalIgnoreCase))
            {
                atsAssemblies.Add(pkg.Name);
            }
        }

        if (additionalProjectReferences is not null)
        {
            foreach (var projectPath in additionalProjectReferences)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(projectPath);
                if (!atsAssemblies.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
                {
                    atsAssemblies.Add(assemblyName);
                }
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
        File.WriteAllText(Path.Combine(_projectModelPath, "appsettings.json"), appSettingsJson);

        // Handle NuGet config and channel resolution
        string? channelName = null;
        var nugetConfigPath = Path.Combine(_projectModelPath, "nuget.config");

        var userNugetConfig = FindNuGetConfig(_appPath);
        if (userNugetConfig is not null)
        {
            File.Copy(userNugetConfig, nugetConfigPath, overwrite: true);
        }

        var channels = await _packagingService.GetChannelsAsync(cancellationToken);
        var localConfig = AspireJsonConfiguration.Load(_appPath);
        var configuredChannelName = localConfig?.Channel;

        if (string.IsNullOrEmpty(configuredChannelName))
        {
            configuredChannelName = await _configurationService.GetConfigurationAsync("channel", cancellationToken);
        }

        PackageChannel? channel;
        if (!string.IsNullOrEmpty(configuredChannelName))
        {
            channel = channels.FirstOrDefault(c => string.Equals(c.Name, configuredChannelName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            channel = channels.FirstOrDefault(c => c.Type == PackageChannelType.Explicit);
        }

        if (channel is not null)
        {
            await NuGetConfigMerger.CreateOrUpdateAsync(
                new DirectoryInfo(_projectModelPath),
                channel,
                cancellationToken: cancellationToken);
            channelName = channel.Name;
        }

        // Create the project file
        var doc = CreateProjectFile(sdkVersion, packages);

        // Add additional project references
        if (additionalProjectReferences is not null)
        {
            var additionalProjectRefs = additionalProjectReferences
                .Select(path => new XElement("ProjectReference",
                    new XAttribute("Include", path),
                    new XElement("IsAspireProjectResource", "false")))
                .ToList();

            if (additionalProjectRefs.Count > 0)
            {
                doc.Root!.Add(new XElement("ItemGroup", additionalProjectRefs));
            }
        }

        // Add appsettings.json to output
        doc.Root!.Add(new XElement("ItemGroup",
            new XElement("None",
                new XAttribute("Include", "appsettings.json"),
                new XAttribute("CopyToOutputDirectory", "PreserveNewest"))));

        var projectFileName = Path.Combine(_projectModelPath, ProjectFileName);

        // Log the full project XML for debugging
        _logger.LogDebug("Generated AppHostServer project file:\n{ProjectXml}", doc.ToString());

        doc.Save(projectFileName);

        return (projectFileName, channelName);
    }

    /// <summary>
    /// Restores and builds the project.
    /// </summary>
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

    /// <inheritdoc />
    public async Task<AppHostServerPrepareResult> PrepareAsync(
        string sdkVersion,
        IEnumerable<(string Name, string Version)> packages,
        CancellationToken cancellationToken = default)
    {
        var (_, channelName) = await CreateProjectFilesAsync(sdkVersion, packages, cancellationToken);
        var (buildSuccess, buildOutput) = await BuildAsync(cancellationToken);

        if (!buildSuccess)
        {
            return new AppHostServerPrepareResult(
                Success: false,
                Output: buildOutput,
                ChannelName: channelName,
                NeedsCodeGeneration: false);
        }

        return new AppHostServerPrepareResult(
            Success: true,
            Output: buildOutput,
            ChannelName: channelName,
            NeedsCodeGeneration: true);
    }

    /// <inheritdoc />
    public string GetInstanceIdentifier() => GetProjectFilePath();

    /// <inheritdoc />
    public (string SocketPath, Process Process, OutputCollector OutputCollector) Run(
        int hostPid,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        string[]? additionalArgs = null,
        bool debug = false)
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

        if (additionalArgs is { Length: > 0 })
        {
            startInfo.ArgumentList.Add("--");
            foreach (var arg in additionalArgs)
            {
                startInfo.ArgumentList.Add(arg);
            }
        }

        startInfo.Environment["REMOTE_APP_HOST_SOCKET_PATH"] = _socketPath;
        startInfo.Environment["REMOTE_APP_HOST_PID"] = hostPid.ToString(System.Globalization.CultureInfo.InvariantCulture);
        startInfo.Environment[KnownConfigNames.CliProcessId] = hostPid.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
            _logger.LogDebug("Enabling debug logging for AppHostServer");
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        var process = Process.Start(startInfo)!;

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

        return (_socketPath, process, outputCollector);
    }

    protected static string? FindNuGetConfig(string workingDirectory)
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

            var configPaths = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var workingDirFullPath = Path.GetFullPath(workingDirectory);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var globalNuGetPath = Path.Combine(userProfile, ".nuget");

            foreach (var configPath in configPaths)
            {
                if (File.Exists(configPath))
                {
                    var configFullPath = Path.GetFullPath(configPath);
                    var configDir = Path.GetDirectoryName(configFullPath);

                    if (configDir is not null &&
                        !configDir.StartsWith(globalNuGetPath, StringComparison.OrdinalIgnoreCase) &&
                        (workingDirFullPath.StartsWith(configDir, StringComparison.OrdinalIgnoreCase) ||
                         configDir.StartsWith(workingDirFullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        return configFullPath;
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
}
