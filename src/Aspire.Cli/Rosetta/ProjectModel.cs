// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Aspire.Cli.Interaction;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Rosetta;

/// <summary>
/// Represents the dotnet project that is used to generate the GenericAppHost.
/// </summary>
internal sealed class ProjectModel
{
    private const string ProjectHashFileName = ".projecthash";
    private const string FolderPrefix = ".aspire";
    private const string AppsFolder = "hosts";
    public const string ProjectFileName = "GenericAppHost.csproj";
    private const string ProjectDllName = "GenericAppHost.dll";
    private const string LaunchSettingsJsonFileName = "./Properties/launchSettings.json";
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

    public const string BuildFolder = "build";
    private const string AssemblyName = "GenericAppHost";
    private readonly string _projectModelPath;
    private readonly string _appPath;

    /// <summary>
    /// Initializes a new instance of the ProjectModel class.
    /// </summary>
    /// <param name="appPath">Specifies the application path for the custom language.</param>
    public ProjectModel(string appPath)
    {
        _appPath = Path.GetFullPath(appPath);
        _appPath = new Uri(_appPath).LocalPath;
        _appPath = OperatingSystem.IsWindows() ? _appPath.ToLowerInvariant() : _appPath;

        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(_appPath));
        var pathDir = Convert.ToHexString(pathHash)[..12].ToLowerInvariant();
        _projectModelPath = Path.Combine(Path.GetTempPath(), FolderPrefix, AppsFolder, pathDir);

        Directory.CreateDirectory(_projectModelPath);
    }

    public string ProjectModelPath => _projectModelPath;
    public string AppPath => _appPath;
    public string BuildPath => Path.Combine(_projectModelPath, BuildFolder);

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
    /// <returns>The full path to the project file.</returns>
    public string CreateProjectFiles(IEnumerable<(string Name, string Version)> packages)
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var csFile in new[] { "InstructionModels.cs", "InstructionProcessor.cs", "JsonRpcServer.cs", "Program.cs", "OrphanDetector.cs" })
        {
            // Extract embedded resource
            using var stream = assembly.GetManifestResourceStream($"Aspire.Cli.Rosetta.Shared.RemoteAppHost.{csFile}")
                ?? throw new InvalidProgramException($"A resource stream was not found: {csFile}");

            using var reader = new StreamReader(stream);
            File.WriteAllText(Path.Combine(_projectModelPath, csFile), reader.ReadToEnd());
        }

        // Create appsettings.json
        var appSettingsJson = """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning",
                  "Aspire.Hosting.Dcp": "Warning"
                }
              }
            }
            """;

        var appSettingsJsonPath = Path.Combine(_projectModelPath, "appsettings.json");
        File.WriteAllText(appSettingsJsonPath, appSettingsJson);

        // Create NuGet.config
        var localPackageSource = LocalPackagePath is not null ? $"""
                    <add key="local" value="{LocalPackagePath.Replace("\\", "/")}" />
                """ : string.Empty;

        var nugetConfig = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
                <packageSources>
                    <clear />{{localPackageSource}}
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """;

        var nugetConfigPath = Path.Combine(_projectModelPath, "NuGet.config");
        File.WriteAllText(nugetConfigPath, nugetConfig);

        // Create launchSettings.json
        var launchSettingsJson = """
        {
            "$schema": "https://json.schemastore.org/launchsettings.json",
            "profiles": {
                "https": {
                    "commandName": "Project",
                    "dotnetRunMessages": true,
                    "launchBrowser": true,
                    "applicationUrl": "https://localhost:17292;http://localhost:15013",
                    "environmentVariables": {
                        "ASPNETCORE_ENVIRONMENT": "Development",
                        "DOTNET_ENVIRONMENT": "Development",
                        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:21227",
                        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:22169"
                    }
                },
                "http": {
                    "commandName": "Project",
                    "dotnetRunMessages": true,
                    "launchBrowser": true,
                    "applicationUrl": "http://localhost:15013",
                    "environmentVariables": {
                        "ASPNETCORE_ENVIRONMENT": "Development",
                        "DOTNET_ENVIRONMENT": "Development",
                        "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL": "http://localhost:19101",
                        "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL": "http://localhost:20025"
                    }
                }
            }
        }
        """;

        var launchSettingsJsonPath = Path.Combine(_projectModelPath, LaunchSettingsJsonFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(launchSettingsJsonPath)!);
        File.WriteAllText(launchSettingsJsonPath, launchSettingsJson);

        // Create the project file
        var template = $"""
            <Project Sdk="Microsoft.NET.Sdk">

                <Sdk Name="Aspire.AppHost.Sdk" Version="{AspireHostVersion}" />

                <PropertyGroup>
                    <OutputType>exe</OutputType>
                    <TargetFramework>{TargetFramework}</TargetFramework>
                    <AssemblyName>{AssemblyName}</AssemblyName>
                    <OutDir>{BuildFolder}</OutDir>
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
            </Project>
            """;

        var doc = XDocument.Parse(template);

        // Add package references
        doc.Root!.Add(new XElement("ItemGroup",
            packages.Select(p => new XElement("PackageReference",
                new XAttribute("Include", p.Name),
                new XAttribute("Version", p.Version)))));

        var projectFileName = Path.Combine(_projectModelPath, ProjectFileName);
        doc.Save(projectFileName);

        return projectFileName;
    }

    /// <summary>
    /// Restores and builds the project dependencies.
    /// </summary>
    public async Task<bool> BuildAsync(IInteractionService interactionService)
    {
        var dotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        var startInfo = new ProcessStartInfo(dotnetExe)
        {
            WorkingDirectory = _projectModelPath,
            WindowStyle = ProcessWindowStyle.Minimized,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(ProjectFileName);

        return await interactionService.ShowStatusAsync(
            "Restoring external packages...",
            () =>
            {
                using var process = Process.Start(startInfo);
                process!.WaitForExit();

                return Task.FromResult(process.ExitCode == 0);
            });
    }

    /// <summary>
    /// Runs the GenericAppHost.
    /// </summary>
    /// <param name="socketPath">The Unix domain socket path for JSON-RPC communication.</param>
    /// <param name="hostPid">The PID of the host process for orphan detection.</param>
    /// <param name="launchSettingsEnvVars">Optional environment variables from apphost.run.json or launchSettings.json.</param>
    public Process Run(string socketPath, int hostPid, IReadOnlyDictionary<string, string>? launchSettingsEnvVars = null)
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

        // Pass environment variables for socket path and parent PID
        startInfo.Environment["REMOTE_APP_HOST_SOCKET_PATH"] = socketPath;
        startInfo.Environment["REMOTE_APP_HOST_PID"] = hostPid.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Apply environment variables from apphost.run.json / launchSettings.json if available
        if (launchSettingsEnvVars != null)
        {
            foreach (var (key, value) in launchSettingsEnvVars)
            {
                startInfo.Environment[key] = value;
            }
        }
        else
        {
            // Default environment variables when no launchSettings.json is present
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        var process = Process.Start(startInfo)!;

        // Forward GenericAppHost output to console
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                Console.WriteLine(e.Data);
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
            {
                Console.Error.WriteLine(e.Data);
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    /// <summary>
    /// Gets the socket path for the GenericAppHost based on the app path.
    /// </summary>
    public string GetSocketPath()
    {
        var pathHash = SHA256.HashData(Encoding.UTF8.GetBytes(_appPath));
        var socketName = Convert.ToHexString(pathHash)[..12].ToLowerInvariant() + ".sock";

        var socketDir = Path.Combine(Path.GetTempPath(), FolderPrefix, "sockets");
        Directory.CreateDirectory(socketDir);

        return Path.Combine(socketDir, socketName);
    }
}
