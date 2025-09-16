// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Templating;

public class DotNetTemplateFactoryTests
{
    private readonly ITestOutputHelper _outputHelper;

    public DotNetTemplateFactoryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    private sealed class FakeNuGetPackageCache : INuGetPackageCache
    {
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        {
            _ = workingDirectory; _ = prerelease; _ = nugetConfigFile; _ = cancellationToken; return Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        }
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        {
            _ = workingDirectory; _ = prerelease; _ = nugetConfigFile; _ = cancellationToken; return Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        }
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken)
        {
            _ = workingDirectory; _ = prerelease; _ = nugetConfigFile; _ = cancellationToken; return Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        }
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, bool useCache, CancellationToken cancellationToken)
        {
            _ = workingDirectory; _ = packageId; _ = filter; _ = prerelease; _ = nugetConfigFile; _ = useCache; _ = cancellationToken; return Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        }
    }

    private static PackageChannel CreateExplicitChannel(PackageMapping[] mappings) => 
        PackageChannel.CreateExplicitChannel("test", PackageChannelQuality.Both, mappings, new FakeNuGetPackageCache());

    private static async Task WriteNuGetConfigAsync(DirectoryInfo dir, string content)
    {
        var path = Path.Combine(dir.FullName, "NuGet.config");
        await File.WriteAllTextAsync(path, content);
    }

    /// <summary>
    /// Test that simulates the path comparison logic by testing NuGetConfigMerger behavior
    /// directly, which is what PromptToCreateOrUpdateNuGetConfigAsync will ultimately call.
    /// </summary>
    [Fact]
    public async Task NuGetConfigMerger_InPlaceCreation_WithoutExistingConfig_CreatesInWorkingDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        
        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://test.feed.example.com")
        };
        var channel = CreateExplicitChannel(mappings);

        // Act - Simulate in-place creation: output directory same as working directory
        await NuGetConfigMerger.CreateOrUpdateAsync(workingDir, channel);

        // Assert
        var nugetConfigPath = Path.Combine(workingDir.FullName, "NuGet.config");
        Assert.True(File.Exists(nugetConfigPath), "NuGet.config should be created in working directory for in-place creation");
    }

    [Fact]
    public async Task NuGetConfigMerger_InPlaceCreation_WithExistingConfig_UpdatesWorkingDirectoryConfig()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        
        // Create existing NuGet.config in working directory without the required source
        await WriteNuGetConfigAsync(workingDir, 
            """
            <?xml version="1.0"?>
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """);

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://test.feed.example.com")
        };
        var channel = CreateExplicitChannel(mappings);

        // Act - Simulate in-place creation: output directory same as working directory
        await NuGetConfigMerger.CreateOrUpdateAsync(workingDir, channel);

        // Assert
        var nugetConfigPath = Path.Combine(workingDir.FullName, "NuGet.config");
        Assert.True(File.Exists(nugetConfigPath), "NuGet.config should exist in working directory");
        
        var content = await File.ReadAllTextAsync(nugetConfigPath);
        Assert.Contains("https://test.feed.example.com", content);
    }

    [Fact]
    public async Task NuGetConfigMerger_SubdirectoryCreation_WithParentConfig_IgnoresParentAndCreatesInOutputDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        var outputDir = Directory.CreateDirectory(Path.Combine(workingDir.FullName, "MyProject"));

        // Create existing NuGet.config in working directory (parent)
        var parentConfigContent = 
            """
            <?xml version="1.0"?>
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """;
        await WriteNuGetConfigAsync(workingDir, parentConfigContent);

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://test.feed.example.com")
        };
        var channel = CreateExplicitChannel(mappings);

        // Act - Simulate subdirectory creation: output directory different from working directory
        await NuGetConfigMerger.CreateOrUpdateAsync(outputDir, channel);

        // Assert
        // Parent NuGet.config should remain unchanged
        var parentConfigPath = Path.Combine(workingDir.FullName, "NuGet.config");
        var parentContent = await File.ReadAllTextAsync(parentConfigPath);
        Assert.Equal(parentConfigContent.ReplaceLineEndings(), parentContent.ReplaceLineEndings());
        Assert.DoesNotContain("https://test.feed.example.com", parentContent);

        // New NuGet.config should be created in output directory
        var outputConfigPath = Path.Combine(outputDir.FullName, "NuGet.config");
        Assert.True(File.Exists(outputConfigPath), "NuGet.config should be created in output directory");
        
        var outputContent = await File.ReadAllTextAsync(outputConfigPath);
        Assert.Contains("https://test.feed.example.com", outputContent);
    }

    [Fact]
    public async Task NuGetConfigMerger_SubdirectoryCreation_WithExistingConfigInOutputDirectory_MergesInOutputDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        var outputDir = Directory.CreateDirectory(Path.Combine(workingDir.FullName, "MyProject"));

        // Create existing NuGet.config in output directory
        await WriteNuGetConfigAsync(outputDir, 
            """
            <?xml version="1.0"?>
            <configuration>
                <packageSources>
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                </packageSources>
            </configuration>
            """);

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://test.feed.example.com")
        };
        var channel = CreateExplicitChannel(mappings);

        // Act - Simulate subdirectory creation: merge into existing config in output directory
        await NuGetConfigMerger.CreateOrUpdateAsync(outputDir, channel);

        // Assert
        var outputConfigPath = Path.Combine(outputDir.FullName, "NuGet.config");
        Assert.True(File.Exists(outputConfigPath), "NuGet.config should exist in output directory");
        
        var content = await File.ReadAllTextAsync(outputConfigPath);
        Assert.Contains("https://test.feed.example.com", content);
        Assert.Contains("https://api.nuget.org/v3/index.json", content);
    }

    [Fact]
    public async Task NuGetConfigMerger_SubdirectoryCreation_WithoutAnyConfig_CreatesInOutputDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        var outputDir = Directory.CreateDirectory(Path.Combine(workingDir.FullName, "MyProject"));

        var mappings = new[]
        {
            new PackageMapping("Aspire.*", "https://test.feed.example.com")
        };
        var channel = CreateExplicitChannel(mappings);

        // Act - Simulate subdirectory creation: create new config in output directory
        await NuGetConfigMerger.CreateOrUpdateAsync(outputDir, channel);

        // Assert
        // No NuGet.config should exist in working directory
        var workingConfigPath = Path.Combine(workingDir.FullName, "NuGet.config");
        Assert.False(File.Exists(workingConfigPath), "No NuGet.config should be created in working directory");

        // New NuGet.config should be created in output directory
        var outputConfigPath = Path.Combine(outputDir.FullName, "NuGet.config");
        Assert.True(File.Exists(outputConfigPath), "NuGet.config should be created in output directory");
        
        var content = await File.ReadAllTextAsync(outputConfigPath);
        Assert.Contains("https://test.feed.example.com", content);
    }

    [Fact]
    public async Task NuGetConfigMerger_ImplicitChannel_DoesNothing()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        var outputDir = Directory.CreateDirectory(Path.Combine(workingDir.FullName, "MyProject"));

        var channel = PackageChannel.CreateImplicitChannel(new FakeNuGetPackageCache());

        // Act
        await NuGetConfigMerger.CreateOrUpdateAsync(outputDir, channel);

        // Assert
        // No NuGet.config should be created anywhere
        var workingConfigPath = Path.Combine(workingDir.FullName, "NuGet.config");
        var outputConfigPath = Path.Combine(outputDir.FullName, "NuGet.config");
        Assert.False(File.Exists(workingConfigPath), "No NuGet.config should be created for implicit channel");
        Assert.False(File.Exists(outputConfigPath), "No NuGet.config should be created for implicit channel");
    }

    [Fact]
    public async Task NuGetConfigMerger_ExplicitChannelWithoutMappings_DoesNothing()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var workingDir = workspace.WorkspaceRoot;
        var outputDir = Directory.CreateDirectory(Path.Combine(workingDir.FullName, "MyProject"));

        var channel = CreateExplicitChannel([]); // No mappings

        // Act
        await NuGetConfigMerger.CreateOrUpdateAsync(outputDir, channel);

        // Assert
        // No NuGet.config should be created anywhere
        var workingConfigPath = Path.Combine(workingDir.FullName, "NuGet.config");
        var outputConfigPath = Path.Combine(outputDir.FullName, "NuGet.config");
        Assert.False(File.Exists(workingConfigPath), "No NuGet.config should be created when no mappings exist");
        Assert.False(File.Exists(outputConfigPath), "No NuGet.config should be created when no mappings exist");
    }
}