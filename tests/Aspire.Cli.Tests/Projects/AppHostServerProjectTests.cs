// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using System.Xml.Linq;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.Mcp;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class AppHostServerProjectTests(ITestOutputHelper outputHelper) : IDisposable
{
    private readonly TemporaryWorkspace _workspace = TemporaryWorkspace.Create(outputHelper);

    public void Dispose()
    {
        _workspace.Dispose();
        GC.SuppressFinalize(this);
    }

    private AppHostServerProject CreateProject(string? appPath = null)
    {
        appPath ??= _workspace.WorkspaceRoot.FullName;
        var runner = new TestDotNetCliRunner();
        var packagingService = new MockPackagingService();
        var configurationService = new TrackingConfigurationService();
        var logger = NullLogger<AppHostServerProject>.Instance;

        return new AppHostServerProject(appPath, runner, packagingService, configurationService, logger);
    }

    /// <summary>
    /// Normalizes a generated csproj for snapshot comparison by replacing dynamic values.
    /// </summary>
    private static string NormalizeCsprojForSnapshot(string csprojContent, AppHostServerProject project)
    {
        // Replace dynamic UserSecretsId with placeholder
        return csprojContent.Replace(project.UserSecretsId, "{USER_SECRETS_ID}");
    }

    [Fact]
    public async Task CreateProjectFiles_ProductionCsproj_MatchesSnapshot()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.AppHost", "13.1.0"),
            ("Aspire.Hosting.Redis", "13.1.0"),
            ("Aspire.Hosting.PostgreSQL", "13.1.0"),
            ("Aspire.Hosting.CodeGeneration.TypeScript", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var csprojContent = await File.ReadAllTextAsync(projectPath);
        var normalized = NormalizeCsprojForSnapshot(csprojContent, project);

        await Verify(normalized, extension: "xml")
            .UseFileName("AppHostServerProject_ProductionCsproj");
    }

    [Fact]
    public async Task CreateProjectFiles_AppSettingsJson_MatchesSnapshot()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.Redis", "13.1.0"),
            ("Aspire.Hosting.PostgreSQL", "13.1.0"),
            ("Aspire.Hosting.CodeGeneration.TypeScript", "13.1.0")
        };

        // Act
        await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var appSettingsPath = Path.Combine(project.ProjectModelPath, "appsettings.json");
        var content = await File.ReadAllTextAsync(appSettingsPath);

        await Verify(content, extension: "json")
            .UseFileName("AppHostServerProject_AppSettingsJson");
    }

    [Fact]
    public async Task CreateProjectFiles_ProgramCs_MatchesSnapshot()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0")
        };

        // Act
        await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var programCsPath = Path.Combine(project.ProjectModelPath, "Program.cs");
        var content = await File.ReadAllTextAsync(programCsPath);

        // Use .txt extension to avoid compilation of snapshot file
        await Verify(content, extension: "txt")
            .UseFileName("AppHostServerProject_ProgramCs");
    }

    [Fact]
    public async Task CreateProjectFiles_GeneratesProductionCsproj_WithAspireSdk()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.AppHost", "13.1.0"),
            ("Aspire.Hosting.Redis", "13.1.0"),
            ("Aspire.Hosting.CodeGeneration.TypeScript", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        Assert.True(File.Exists(projectPath));
        var doc = XDocument.Load(projectPath);

        // Verify SDK attribute
        var sdkAttr = doc.Root?.Attribute("Sdk")?.Value;
        Assert.Equal("Aspire.AppHost.Sdk/13.1.0", sdkAttr);
    }

    [Fact]
    public async Task CreateProjectFiles_ProductionMode_FiltersOutImplicitPackages()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.AppHost", "13.1.0"),
            ("Aspire.Hosting.Redis", "13.1.0"),
            ("Aspire.Hosting.PostgreSQL", "13.1.0"),
            ("Aspire.Hosting.CodeGeneration.TypeScript", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);
        var packageRefs = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .ToList();

        // Aspire.Hosting and Aspire.Hosting.AppHost should NOT be in package references (SDK provides them)
        Assert.DoesNotContain("Aspire.Hosting", packageRefs);
        Assert.DoesNotContain("Aspire.Hosting.AppHost", packageRefs);

        // Integration packages and code gen should be present
        Assert.Contains("Aspire.Hosting.Redis", packageRefs);
        Assert.Contains("Aspire.Hosting.PostgreSQL", packageRefs);
        Assert.Contains("Aspire.Hosting.CodeGeneration.TypeScript", packageRefs);

        // RemoteHost should always be added
        Assert.Contains("Aspire.Hosting.RemoteHost", packageRefs);
    }

    [Theory]
    [InlineData("Aspire.Hosting", false)]
    [InlineData("Aspire.Hosting.AppHost", false)]
    [InlineData("Aspire.Hosting.Redis", true)]
    [InlineData("Aspire.Hosting.PostgreSQL", true)]
    [InlineData("Aspire.Hosting.RemoteHost", true)]
    [InlineData("Aspire.Hosting.CodeGeneration.TypeScript", true)]
    [InlineData("Aspire.Hosting.CodeGeneration.Python", true)]
    public async Task CreateProjectFiles_ProductionMode_CorrectlyFiltersPackages(string packageName, bool shouldBeIncluded)
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.AppHost", "13.1.0"),
            (packageName, "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);
        var packageRefs = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .ToList();

        if (shouldBeIncluded)
        {
            Assert.Contains(packageName, packageRefs);
        }
        else
        {
            Assert.DoesNotContain(packageName, packageRefs);
        }
    }

    [Fact]
    public async Task CreateProjectFiles_ProductionMode_AlwaysAddsRemoteHost()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.AppHost", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);
        var packageRefs = doc.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => v is not null)
            .ToList();

        // RemoteHost should always be present even if not in input packages
        Assert.Contains("Aspire.Hosting.RemoteHost", packageRefs);
    }

    [Fact]
    public async Task CreateProjectFiles_GeneratesProgramCs()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0")
        };

        // Act
        await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var programCs = Path.Combine(project.ProjectModelPath, "Program.cs");
        Assert.True(File.Exists(programCs));

        var content = await File.ReadAllTextAsync(programCs);
        Assert.Contains("RemoteHostServer.RunAsync", content);
    }

    [Fact]
    public async Task CreateProjectFiles_GeneratesAppSettingsJson_WithAtsAssemblies()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.Redis", "13.1.0"),
            ("Aspire.Hosting.CodeGeneration.TypeScript", "13.1.0")
        };

        // Act
        await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var appSettingsPath = Path.Combine(project.ProjectModelPath, "appsettings.json");
        Assert.True(File.Exists(appSettingsPath));

        var content = await File.ReadAllTextAsync(appSettingsPath);
        Assert.Contains("AtsAssemblies", content);
        Assert.Contains("Aspire.Hosting", content);
        Assert.Contains("Aspire.Hosting.Redis", content);
        Assert.Contains("Aspire.Hosting.CodeGeneration.TypeScript", content);
    }

    [Fact]
    public async Task CreateProjectFiles_ProductionMode_HasMinimalProperties()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);

        // Should have minimal property group
        var propertyGroup = doc.Descendants("PropertyGroup").First();

        Assert.NotNull(propertyGroup.Element("OutputType"));
        Assert.NotNull(propertyGroup.Element("TargetFramework"));
        Assert.NotNull(propertyGroup.Element("AssemblyName"));
        Assert.NotNull(propertyGroup.Element("OutDir"));
        Assert.NotNull(propertyGroup.Element("UserSecretsId"));
        Assert.NotNull(propertyGroup.Element("IsAspireHost"));

        // Should NOT have dev-mode only properties
        Assert.Null(propertyGroup.Element("IsPublishable"));
        Assert.Null(propertyGroup.Element("SelfContained"));
        Assert.Null(propertyGroup.Element("NoWarn"));
        Assert.Null(propertyGroup.Element("RepoRoot"));
    }

    [Fact]
    public async Task CreateProjectFiles_ProductionMode_DisablesCodeGeneration()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);

        // Should have empty targets to disable code generation
        var targets = doc.Descendants("Target").ToList();
        Assert.Contains(targets, t => t.Attribute("Name")?.Value == "_CSharpWriteHostProjectMetadataSources");
        Assert.Contains(targets, t => t.Attribute("Name")?.Value == "_CSharpWriteProjectMetadataSources");
    }

    [Fact]
    public async Task CreateProjectFiles_CopiesAppSettingsToOutput()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);

        var noneElement = doc.Descendants("None")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "appsettings.json");

        Assert.NotNull(noneElement);
        Assert.Equal("PreserveNewest", noneElement.Attribute("CopyToOutputDirectory")?.Value);
    }

    [Fact]
    public void DefaultSdkVersion_ReturnsValidVersion()
    {
        // Act
        var version = AppHostServerProject.DefaultSdkVersion;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        // Should not contain '+' (commit hash should be stripped)
        Assert.DoesNotContain("+", version);
    }

    [Fact]
    public void ProjectModelPath_IsStableForSameAppPath()
    {
        // Arrange
        var appPath = _workspace.WorkspaceRoot.FullName;

        // Act
        var project1 = CreateProject(appPath);
        var project2 = CreateProject(appPath);

        // Assert - same app path should result in same project model path
        Assert.Equal(project1.ProjectModelPath, project2.ProjectModelPath);
    }

    [Fact]
    public void UserSecretsId_IsStableForSameAppPath()
    {
        // Arrange
        var appPath = _workspace.WorkspaceRoot.FullName;

        // Act
        var project1 = CreateProject(appPath);
        var project2 = CreateProject(appPath);

        // Assert - same app path should result in same user secrets ID
        Assert.Equal(project1.UserSecretsId, project2.UserSecretsId);
    }

    [Fact]
    public async Task CreateProjectFiles_UsesSdkVersionInPackageAttribute()
    {
        // Arrange
        var project = CreateProject();
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.2.0")
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.2.0", packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);
        var sdkAttr = doc.Root?.Attribute("Sdk")?.Value;
        Assert.Equal("Aspire.AppHost.Sdk/13.2.0", sdkAttr);
    }

    [Fact]
    public async Task CreateProjectFiles_PackageVersionsMatchSdkVersion()
    {
        // Arrange
        var project = CreateProject();
        var sdkVersion = "13.3.0";
        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", sdkVersion),
            ("Aspire.Hosting.Redis", sdkVersion)
        };

        // Act
        var (projectPath, _) = await project.CreateProjectFilesAsync(sdkVersion, packages).DefaultTimeout();

        // Assert
        var doc = XDocument.Load(projectPath);

        // RemoteHost should use SDK version
        var remoteHostRef = doc.Descendants("PackageReference")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "Aspire.Hosting.RemoteHost");

        Assert.NotNull(remoteHostRef);
        Assert.Equal(sdkVersion, remoteHostRef.Attribute("Version")?.Value);
    }

    /// <summary>
    /// Regression test for channel switching bug.
    /// When a project has a channel configured in .aspire/settings.json (project-local),
    /// the NuGet.config should use that channel's hive path, NOT the global config channel.
    /// 
    /// Bug scenario:
    /// 1. User runs `aspire update` and selects "pr-new" channel
    /// 2. UpdatePackagesAsync saves channel="pr-new" to project-local .aspire/settings.json
    /// 3. BuildAndGenerateSdkAsync calls CreateProjectFilesAsync
    /// 4. BUG: CreateProjectFilesAsync reads channel from GLOBAL config (returns "pr-old")
    /// 5. NuGet.config is generated with pr-old hive path instead of pr-new
    /// 6. Build fails because packages are in pr-new hive but NuGet.config points to pr-old
    /// </summary>
    [Fact]
    public async Task CreateProjectFiles_NuGetConfig_UsesProjectLocalChannel_NotGlobalChannel_MatchesSnapshot()
    {
        // Arrange
        var appPath = _workspace.WorkspaceRoot.FullName;

        // Create two PR hive directories to simulate having multiple PR builds
        var hivesDir = _workspace.WorkspaceRoot.CreateSubdirectory("hives");
        var prOldHive = hivesDir.CreateSubdirectory("pr-old");
        var prNewHive = hivesDir.CreateSubdirectory("pr-new");

        // Create project-local .aspire/settings.json with channel="pr-new"
        // This simulates what happens after `aspire update` saves the selected channel
        var aspireDir = _workspace.WorkspaceRoot.CreateSubdirectory(".aspire");
        var settingsJson = Path.Combine(aspireDir.FullName, "settings.json");
        await File.WriteAllTextAsync(settingsJson, """
            {
                "channel": "pr-new",
                "sdkVersion": "13.1.0"
            }
            """);

        // Configure global config to return "pr-old" (the WRONG channel)
        // This simulates a stale global config that hasn't been updated
        var configurationService = new TrackingConfigurationService
        {
            OnGetConfiguration = key => key == "channel" ? "pr-old" : null
        };

        // Create a packaging service that returns explicit channels for both PR hives
        var packagingService = new MockPackagingServiceWithExplicitChannels(
            prOldHive.FullName,
            prNewHive.FullName);

        var runner = new TestDotNetCliRunner();
        
        // Use a real logger to capture debug output for diagnostics
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddXunit(outputHelper);
        });
        var logger = loggerFactory.CreateLogger<AppHostServerProject>();

        // Use a workspace-local ProjectModelPath for test isolation
        var projectModelPath = Path.Combine(appPath, ".aspire_server");
        var project = new AppHostServerProject(appPath, runner, packagingService, configurationService, logger, projectModelPath);

        var packages = new List<(string Name, string Version)>
        {
            ("Aspire.Hosting", "13.1.0"),
            ("Aspire.Hosting.AppHost", "13.1.0"),
            ("Aspire.Hosting.Redis", "13.1.0")
        };

        // Act
        await project.CreateProjectFilesAsync("13.1.0", packages).DefaultTimeout();

        // Dump workspace directory tree for debugging
        outputHelper.WriteLine("=== Workspace Directory Tree ===");
        DumpDirectoryTree(appPath, outputHelper);
        outputHelper.WriteLine("================================");

        // Also dump ProjectModelPath content
        outputHelper.WriteLine($"=== ProjectModelPath ({project.ProjectModelPath}) ===");
        if (Directory.Exists(project.ProjectModelPath))
        {
            DumpDirectoryTree(project.ProjectModelPath, outputHelper);
        }
        else
        {
            outputHelper.WriteLine("  (directory does not exist)");
        }
        outputHelper.WriteLine("================================");

        // Assert - verify nuget.config uses the correct channel
        // Note: NuGetConfigMerger creates the file as "nuget.config" (lowercase)
        var nugetConfigPath = Path.Combine(project.ProjectModelPath, "nuget.config");
        
        // Build diagnostic info for assertion failure
        var diagnosticInfo = new System.Text.StringBuilder();
        diagnosticInfo.AppendLine($"appPath: {appPath}");
        diagnosticInfo.AppendLine($"settingsJson path: {settingsJson}");
        diagnosticInfo.AppendLine($"settingsJson exists: {File.Exists(settingsJson)}");
        if (File.Exists(settingsJson))
        {
            diagnosticInfo.AppendLine($"settingsJson content: {File.ReadAllText(settingsJson)}");
        }
        diagnosticInfo.AppendLine($"project.ProjectModelPath: {project.ProjectModelPath}");
        diagnosticInfo.AppendLine($"nugetConfigPath: {nugetConfigPath}");
        diagnosticInfo.AppendLine($"nugetConfigPath exists: {File.Exists(nugetConfigPath)}");
        
        // List all files for debugging case sensitivity issues
        if (Directory.Exists(project.ProjectModelPath))
        {
            diagnosticInfo.AppendLine("Files in ProjectModelPath:");
            foreach (var file in Directory.GetFiles(project.ProjectModelPath))
            {
                diagnosticInfo.AppendLine($"  - {Path.GetFileName(file)}");
            }
        }
        
        // The nuget.config should exist
        Assert.True(File.Exists(nugetConfigPath), $"nuget.config should be created\n\nDiagnostics:\n{diagnosticInfo}");
        
        var nugetConfigContent = await File.ReadAllTextAsync(nugetConfigPath);

        // Normalize paths for snapshot (replace machine-specific paths)
        var normalizedContent = nugetConfigContent
            .Replace(prNewHive.FullName, "{PR_NEW_HIVE}")
            .Replace(prOldHive.FullName, "{PR_OLD_HIVE}");

        // Snapshot verification - this will fail if the bug exists
        // Expected: Contains {PR_NEW_HIVE} (project-local channel)
        // Bug behavior: Contains {PR_OLD_HIVE} (global config channel)
        await Verify(normalizedContent, extension: "xml")
            .UseFileName("AppHostServerProject_NuGetConfig_UsesProjectLocalChannel");
    }

    /// <summary>
    /// Mock packaging service that returns explicit PR channels with specific hive paths.
    /// Used to test that the correct channel is selected based on project-local settings.
    /// </summary>
    private sealed class MockPackagingServiceWithExplicitChannels : IPackagingService
    {
        private readonly string _prOldHivePath;
        private readonly string _prNewHivePath;

        public MockPackagingServiceWithExplicitChannels(string prOldHivePath, string prNewHivePath)
        {
            _prOldHivePath = prOldHivePath;
            _prNewHivePath = prNewHivePath;
        }

        public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
        {
            var nugetCache = new FakeNuGetPackageCache();

            // Create explicit channels for both PR hives
            var prOldChannel = PackageChannel.CreateExplicitChannel("pr-old", PackageChannelQuality.Prerelease, new[]
            {
                new PackageMapping("Aspire*", _prOldHivePath),
                new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
            }, nugetCache);

            var prNewChannel = PackageChannel.CreateExplicitChannel("pr-new", PackageChannelQuality.Prerelease, new[]
            {
                new PackageMapping("Aspire*", _prNewHivePath),
                new PackageMapping(PackageMapping.AllPackages, "https://api.nuget.org/v3/index.json")
            }, nugetCache);

            var implicitChannel = PackageChannel.CreateImplicitChannel(nugetCache);

            return Task.FromResult<IEnumerable<PackageChannel>>(new[] { implicitChannel, prOldChannel, prNewChannel });
        }
    }

    private sealed class FakeNuGetPackageCache : INuGetPackageCache
    {
        public Task<IEnumerable<NuGetPackageCli>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);

        public Task<IEnumerable<NuGetPackageCli>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);

        public Task<IEnumerable<NuGetPackageCli>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);

        public Task<IEnumerable<NuGetPackageCli>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, bool useCache, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<NuGetPackageCli>>([]);
    }

    private static void DumpDirectoryTree(string path, ITestOutputHelper output, string indent = "")
    {
        var dirInfo = new DirectoryInfo(path);
        output.WriteLine($"{indent}{dirInfo.Name}/");
        
        foreach (var file in dirInfo.GetFiles())
        {
            output.WriteLine($"{indent}  {file.Name}");
        }
        
        foreach (var dir in dirInfo.GetDirectories())
        {
            DumpDirectoryTree(dir.FullName, output, indent + "  ");
        }
    }
}
