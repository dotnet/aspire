// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.Mcp;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        await project.CreateProjectFilesAsync("13.1.0", packages);

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
        await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        await project.CreateProjectFilesAsync("13.1.0", packages);

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
        await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.1.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync("13.2.0", packages);

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
        var (projectPath, _) = await project.CreateProjectFilesAsync(sdkVersion, packages);

        // Assert
        var doc = XDocument.Load(projectPath);

        // RemoteHost should use SDK version
        var remoteHostRef = doc.Descendants("PackageReference")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "Aspire.Hosting.RemoteHost");

        Assert.NotNull(remoteHostRef);
        Assert.Equal(sdkVersion, remoteHostRef.Attribute("Version")?.Value);
    }
}
