// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Aspire.Cli.Interaction;

namespace Aspire.Cli.Rosetta;

/// <summary>
/// Represents the dotnet project that is used to generate the AppHost.
/// </summary>

internal sealed class ProjectModel
{
    const string ProjectHashFileName = ".projecthash";
    const string FolderPrefix = ".aspire";
    const string AppsFolder = "hosts";
    public const string ProjectFileName = "GenericAppHost.csproj";
    const string ProjectDllName = "GenericAppHost.dll";
    const string LaunchSettingsJsonFileName = "./Properties/launchSettings.json";
    const string TargetFramework = "net9.0";
    public static string AspireHostVersion = Environment.GetEnvironmentVariable("ASPIRE_POLYGLOT_PACKAGE_VERSION") ?? "9.5.0";
    public static string? LocalPakcagePath = Environment.GetEnvironmentVariable("ASPIRE_POLYGLOT_PACKAGE_SOURCE");
    public const string BuildFolder = "build";
    const string AssemblyName = "GenericAppHost";
    private readonly string _projectModelPath;
    private readonly string _appPath;

    /// <summary>
    /// Initializes a new instance of the ProjectModel class.
    /// </summary>
    /// <param name="appPath">Specifies the application path in for the custom language.</param>
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
    public string CreateProjectFiles(IEnumerable<(string, string)> packages)
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var csFile in new string[] { "InstructionModels.cs", "InstructionProcessor.cs", "JsonRpcServer.cs", "Program.cs", "OrphanDetector.cs" })
        {
            // Extract InstructionModels.cs
            using var stream = assembly.GetManifestResourceStream($"Aspire.Cli.Rosetta.Shared.RemoteAppHost.{csFile}")
                ?? throw new InvalidProgramException($"A resource stream was not found: {csFile}");

            using var reader = new StreamReader(stream);
            File.WriteAllText(Path.Combine(_projectModelPath, csFile), reader.ReadToEnd());
        }

        // language=json
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

        var localPackageSource = LocalPakcagePath is not null ? $"""
                    <add key="local" value="{LocalPakcagePath.Replace("\\", "/")}" />
                """ : string.Empty;

        // Add NuGet.config to the project model path
        var nugetConfig = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
                <packageSources>
                    <clear />{{localPackageSource}}
                    <add key="dotnet9" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """;

        var nugetConfigPath = Path.Combine(_projectModelPath, "NuGet.config");
        File.WriteAllText(nugetConfigPath, nugetConfig);

        // language=json
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

        // language=xml
        string template = $"""
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
                    <PackageReference Include="StreamJsonRpc" Version="2.22.11" />
                </ItemGroup>
            </Project>
            """;

        // TODO: Uncomment to hide build logs

        // Create the msbuild.rsp file in the project model path
        // File.WriteAllText(Path.Combine(_projectModelPath, "msbuild.rsp"),
        // """
        // -noconsolelogger
        // -tl:off
        // -nologo
        // """);

        var doc = XDocument.Parse(template);

        doc.Root!.Add(new XElement("ItemGroup",
            packages.Select(CreatePackageReference))
            );

        var projectFileName = Path.Combine(_projectModelPath, ProjectFileName);
        doc.Save(projectFileName);

        return projectFileName;

        static XElement CreatePackageReference((string, string) package)
        {
            (string name, string version) = package;

            return new XElement("PackageReference",
                                new XAttribute("Include", name),
                                new XAttribute("Version", version)
                            );
        }
    }

    public string BuildPath => Path.Combine(_projectModelPath, BuildFolder);

    /// <summary>
    /// Restores the project dependencies.
    /// </summary>
    public async Task<bool> Restore(IInteractionService interactionService)
    {
        var dotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        var startInfo = new ProcessStartInfo(dotnetExe);
        startInfo.WorkingDirectory = _projectModelPath;
        startInfo.WindowStyle = ProcessWindowStyle.Minimized;
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(ProjectFileName);
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        return await interactionService.ShowStatusAsync(
            $":hammer_and_wrench:  Restoring external packages...",
            () =>
            {
                using var process = Process.Start(startInfo);
                process!.WaitForExit();

                return Task.FromResult(process.ExitCode == 0);
            });
    }

    /// <summary>
    /// Creates a dependency context from the restored artifacts.
    /// </summary>
    /// <returns></returns>
    public IDependencyContext CreateDependencyContext()
    {
        return new DepsFileDependencyContext(BuildPath);
    }

    public Process Run()
    {
        var assemblyPath = Path.Combine(BuildPath, ProjectDllName);

        var dotnetExe = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        var startInfo = new ProcessStartInfo(dotnetExe);
        startInfo.WorkingDirectory = _projectModelPath;
        startInfo.WindowStyle = ProcessWindowStyle.Minimized;
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true; // To keep the process stdout flowing

        var process = Process.Start(startInfo)!;

        return process;
    }

    public void CreateHelpersCs(string content)
    {
        var helpersCsPath = Path.Combine(_projectModelPath, "Helpers.cs");
        File.WriteAllText(helpersCsPath, content);
    }

    // New implementation of IDependencyContext over the deps file
    private class DepsFileDependencyContext : IDependencyContext
    {
        private readonly JsonObject _libraries; // parsed target libraries
        private readonly string _buildPath;

        public string ArtifactsPath => _buildPath;

        public DepsFileDependencyContext(string buildPath)
        {
            _buildPath = buildPath;
            var depsPath = Path.Combine(buildPath, $"{AssemblyName}.deps.json");
            var depsObj = JsonNode.Parse(File.ReadAllText(depsPath));

            if (depsObj == null)
            {
                throw new InvalidOperationException($"{depsPath} could not be parsed.");
            }

            // Use the first target as the default target.
            var targets = depsObj["targets"]?.AsObject();

            if (targets == null || targets.Count == 0)
            {
                throw new InvalidOperationException($"No targets found in {depsPath}.");
            }

            var runtimeTarget = depsObj["runtimeTarget"]?["name"]?.ToString() ?? throw new InvalidOperationException("Invalid deps file structure.");

            _libraries = targets[runtimeTarget]?.AsObject() ?? throw new InvalidOperationException("Invalid target structure.");
        }

        public IEnumerable<string> GetAssemblyPaths(string name, string version)
        {
            var key = $"{name}/{version}";
            var packageObj = _libraries[key]?.AsObject();

            if (packageObj == null)
            {
                yield break;
            }

            var runtime = packageObj["runtime"]?.AsObject() ?? [];

            foreach (var (assemblyPath, _) in runtime)
            {
                var assemblyName = Path.GetFileName(assemblyPath);
                var fullPath = Path.Combine(_buildPath, assemblyName);

                if (File.Exists(fullPath))
                {
                    yield return fullPath;
                }
                else
                {
                    Console.WriteLine($"Assembly {fullPath} not found.");
                }
            }
        }

        public IEnumerable<(string, string)> GetDependencies(string name, string version)
        {
            var key = $"{name}/{version}";
            var packageObj = _libraries[key]?.AsObject();

            if (packageObj == null)
            {
                yield break;
            }

            var dependencies = packageObj["dependencies"]?.AsObject() ?? [];

            foreach (var (depName, v) in dependencies)
            {
                string depVersion = v?.GetValue<string>()!;
                yield return (depName, depVersion);
            }
        }
    }
}
