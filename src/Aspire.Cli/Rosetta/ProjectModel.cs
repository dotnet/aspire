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

        // Handle NuGet.config:
        // 1. If local package source is specified (dev scenario), create a config that includes it
        // 2. Otherwise, copy user's NuGet.config if found (to respect their feeds/auth)
        var nugetConfigPath = Path.Combine(_projectModelPath, "NuGet.config");
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
            // Search for NuGet.config starting from user's project directory and walking up
            var userNugetConfig = FindNuGetConfig(_appPath);
            if (userNugetConfig is not null)
            {
                File.Copy(userNugetConfig, nugetConfigPath, overwrite: true);
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

            template = $"""
                <Project Sdk="Microsoft.NET.Sdk">

                    <PropertyGroup>
                        <OutputType>exe</OutputType>
                        <TargetFramework>{TargetFramework}</TargetFramework>
                        <AssemblyName>{AssemblyName}</AssemblyName>
                        <OutDir>{BuildFolder}</OutDir>
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
                    Console.WriteLine($"Warning: Could not find local project for {pkg.Name}, falling back to NuGet");
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
        }
        else
        {
            // Add package references (standard NuGet flow)
            doc.Root!.Add(new XElement("ItemGroup",
                packages.Select(p => new XElement("PackageReference",
                    new XAttribute("Include", p.Name),
                    new XAttribute("Version", p.Version)))));
        }

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

            // Find a config that's within the project directory tree (not global user config).
            // Global configs (e.g., ~/.nuget/NuGet/NuGet.Config) will be found by dotnet anyway.
            var configPaths = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var workingDirFullPath = Path.GetFullPath(workingDirectory);

            foreach (var configPath in configPaths)
            {
                if (File.Exists(configPath))
                {
                    var configFullPath = Path.GetFullPath(configPath);
                    // Check if this config is within the project directory tree
                    if (configFullPath.StartsWith(workingDirFullPath, StringComparison.OrdinalIgnoreCase))
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
}
