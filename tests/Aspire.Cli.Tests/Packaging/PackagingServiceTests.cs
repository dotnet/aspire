// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace Aspire.Cli.Tests.Packaging;

public class PackagingServiceTests(ITestOutputHelper outputHelper)
{

    private sealed class FakeNuGetPackageCache : INuGetPackageCache
    {
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, bool useCache, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
    }

    private sealed class TestFeatures : IFeatures
    {
        private readonly Dictionary<string, bool> _features = new();

        public bool IsFeatureEnabled(string featureFlag, bool defaultValue)
        {
            return _features.TryGetValue(featureFlag, out var value) ? value : defaultValue;
        }

        public void SetFeature(string featureFlag, bool enabled)
        {
            _features[featureFlag] = enabled;
        }
    }

    [Fact]
    public async Task GetChannelsAsync_WhenStagingChannelDisabled_DoesNotIncludeStagingChannel()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var tempDir = workspace.WorkspaceRoot;
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "cache"));
        var executionContext = new CliExecutionContext(tempDir, hivesDir, cacheDir, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")));
        
        var features = new TestFeatures();
        var configuration = new ConfigurationBuilder().Build();
        var packagingService = new PackagingService(executionContext, new FakeNuGetPackageCache(), features, configuration);

        // Act
        var channels = await packagingService.GetChannelsAsync();

        // Assert
        var channelNames = channels.Select(c => c.Name).ToList();
        Assert.DoesNotContain("staging", channelNames);
        Assert.Contains("default", channelNames);
        Assert.Contains("stable", channelNames);
        Assert.Contains("daily", channelNames);

        // Verify that non-staging channels have ConfigureGlobalPackagesFolder = false
        var defaultChannel = channels.First(c => c.Name == "default");
        Assert.False(defaultChannel.ConfigureGlobalPackagesFolder);
        
        var stableChannel = channels.First(c => c.Name == "stable");
        Assert.False(stableChannel.ConfigureGlobalPackagesFolder);
        
        var dailyChannel = channels.First(c => c.Name == "daily");
        Assert.False(dailyChannel.ConfigureGlobalPackagesFolder);
    }

    [Fact]
    public async Task GetChannelsAsync_WhenStagingChannelEnabled_IncludesStagingChannelWithOverrideHash()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var tempDir = workspace.WorkspaceRoot;
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "cache"));
        var executionContext = new CliExecutionContext(tempDir, hivesDir, cacheDir, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")));
        
        var features = new TestFeatures();
        features.SetFeature(KnownFeatures.StagingChannelEnabled, true);
        
        var testHash = "12345678";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["overrideStagingHash"] = testHash
            })
            .Build();

        var packagingService = new PackagingService(executionContext, new FakeNuGetPackageCache(), features, configuration);

        // Act
        var channels = await packagingService.GetChannelsAsync();

        // Assert
        var channelNames = channels.Select(c => c.Name).ToList();
        Assert.Contains("staging", channelNames);
        
        var stagingChannel = channels.First(c => c.Name == "staging");
        Assert.Equal(PackageChannelQuality.Stable, stagingChannel.Quality);
        Assert.True(stagingChannel.ConfigureGlobalPackagesFolder);
        Assert.NotNull(stagingChannel.Mappings);
        
        var aspireMapping = stagingChannel.Mappings!.FirstOrDefault(m => m.PackageFilter == "Aspire*");
        Assert.NotNull(aspireMapping);
        Assert.Equal($"https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-{testHash}/nuget/v3/index.json", aspireMapping.Source);
        
        var nugetMapping = stagingChannel.Mappings!.FirstOrDefault(m => m.PackageFilter == "*");
        Assert.NotNull(nugetMapping);
        Assert.Equal("https://api.nuget.org/v3/index.json", nugetMapping.Source);
    }

    [Fact]
    public async Task GetChannelsAsync_WhenStagingChannelEnabledWithLongOverrideHash_TruncatesTo8Characters()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var tempDir = workspace.WorkspaceRoot;
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "cache"));
        var executionContext = new CliExecutionContext(tempDir, hivesDir, cacheDir, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")));
        
        var features = new TestFeatures();
        features.SetFeature(KnownFeatures.StagingChannelEnabled, true);
        
        var longHash = "48a11dae4f2a514b4a933956701f9ae45b2c9a25";
        var expectedHash = "48a11dae";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["overrideStagingHash"] = longHash
            })
            .Build();

        var packagingService = new PackagingService(executionContext, new FakeNuGetPackageCache(), features, configuration);

        // Act
        var channels = await packagingService.GetChannelsAsync();

        // Assert
        var stagingChannel = channels.First(c => c.Name == "staging");
        var aspireMapping = stagingChannel.Mappings!.FirstOrDefault(m => m.PackageFilter == "Aspire*");
        Assert.NotNull(aspireMapping);
        Assert.Equal($"https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-{expectedHash}/nuget/v3/index.json", aspireMapping.Source);
    }

    [Fact]
    public async Task GetChannelsAsync_WhenStagingChannelEnabledWithShortOverrideHash_UsesFull()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var tempDir = workspace.WorkspaceRoot;
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir.FullName, ".aspire", "cache"));
        var executionContext = new CliExecutionContext(tempDir, hivesDir, cacheDir, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes")));
        
        var features = new TestFeatures();
        features.SetFeature(KnownFeatures.StagingChannelEnabled, true);
        
        var shortHash = "abc123";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["overrideStagingHash"] = shortHash
            })
            .Build();

        var packagingService = new PackagingService(executionContext, new FakeNuGetPackageCache(), features, configuration);

        // Act
        var channels = await packagingService.GetChannelsAsync();

        // Assert
        var stagingChannel = channels.First(c => c.Name == "staging");
        var aspireMapping = stagingChannel.Mappings!.FirstOrDefault(m => m.PackageFilter == "Aspire*");
        Assert.NotNull(aspireMapping);
        Assert.Equal($"https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-{shortHash}/nuget/v3/index.json", aspireMapping.Source);
    }

    [Fact]
    public async Task NuGetConfigMerger_WhenChannelRequiresGlobalPackagesFolder_AddsGlobalPackagesFolderConfiguration()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var tempDir = workspace.WorkspaceRoot;
        
        var features = new TestFeatures();
        features.SetFeature(KnownFeatures.StagingChannelEnabled, true);
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["overrideStagingHash"] = "48a11dae"
            })
            .Build();

        var packagingService = new PackagingService(
            new CliExecutionContext(tempDir, tempDir, tempDir, new DirectoryInfo(Path.Combine(Path.GetTempPath(), "aspire-test-runtimes"))), 
            new FakeNuGetPackageCache(), 
            features, 
            configuration);

        var channels = await packagingService.GetChannelsAsync();
        var stagingChannel = channels.First(c => c.Name == "staging");

        // Act
        await NuGetConfigMerger.CreateOrUpdateAsync(tempDir, stagingChannel);

        // Assert
        var nugetConfigPath = Path.Combine(tempDir.FullName, "NuGet.config");
        Assert.True(File.Exists(nugetConfigPath));
        
        var configContent = await File.ReadAllTextAsync(nugetConfigPath);
        Assert.Contains("globalPackagesFolder", configContent);
        Assert.Contains(".nugetpackages", configContent);

        // Verify the XML structure
        var doc = XDocument.Load(nugetConfigPath);
        var configSection = doc.Root?.Element("config");
        Assert.NotNull(configSection);
        
        var globalPackagesFolderAdd = configSection.Elements("add")
            .FirstOrDefault(add => string.Equals((string?)add.Attribute("key"), "globalPackagesFolder", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(globalPackagesFolderAdd);
        Assert.Equal(".nugetpackages", (string?)globalPackagesFolderAdd.Attribute("value"));
    }
}
