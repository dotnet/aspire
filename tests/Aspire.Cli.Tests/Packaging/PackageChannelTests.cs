// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Tests.Packaging;

public class PackageChannelTests
{
    private sealed class FakeNuGetPackageCache : INuGetPackageCache
    {
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetTemplatePackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetIntegrationPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetCliPackagesAsync(DirectoryInfo workingDirectory, bool prerelease, FileInfo? nugetConfigFile, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
        public Task<IEnumerable<Aspire.Shared.NuGetPackageCli>> GetPackagesAsync(DirectoryInfo workingDirectory, string packageId, Func<string, bool>? filter, bool prerelease, FileInfo? nugetConfigFile, bool useCache, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<Aspire.Shared.NuGetPackageCli>>([]);
    }

    [Fact]
    public void SourceDetails_ImplicitChannel_ReturnsBasedOnNuGetConfig()
    {
        // Arrange
        var cache = new FakeNuGetPackageCache();

        // Act
        var channel = PackageChannel.CreateImplicitChannel(cache);

        // Assert
        Assert.Equal(PackagingStrings.BasedOnNuGetConfig, channel.SourceDetails);
        Assert.Equal(PackageChannelType.Implicit, channel.Type);
    }

    [Fact]
    public void SourceDetails_ExplicitChannelWithAspireMapping_ReturnsSourceFromMapping()
    {
        // Arrange
        var cache = new FakeNuGetPackageCache();
        var aspireSource = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json";
        var mappings = new[]
        {
            new PackageMapping("Aspire*", aspireSource, MappingType.Primary),
            new PackageMapping("*", "https://api.nuget.org/v3/index.json", MappingType.Supporting)
        };

        // Act
        var channel = PackageChannel.CreateExplicitChannel("daily", PackageChannelQuality.Prerelease, mappings, cache);

        // Assert
        Assert.Equal(aspireSource, channel.SourceDetails);
        Assert.Equal(PackageChannelType.Explicit, channel.Type);
    }

    [Fact]
    public void SourceDetails_ExplicitChannelWithPrHivePath_ReturnsLocalPath()
    {
        // Arrange
        var cache = new FakeNuGetPackageCache();
        var prHivePath = "/Users/davidfowler/.aspire/hives/pr-10981";
        var mappings = new[]
        {
            new PackageMapping("Aspire*", prHivePath, MappingType.Primary),
            new PackageMapping("*", "https://api.nuget.org/v3/index.json", MappingType.Supporting)
        };

        // Act
        var channel = PackageChannel.CreateExplicitChannel("pr-10981", PackageChannelQuality.Prerelease, mappings, cache);

        // Assert
        Assert.Equal(prHivePath, channel.SourceDetails);
        Assert.Equal(PackageChannelType.Explicit, channel.Type);
    }

    [Fact]
    public void SourceDetails_ExplicitChannelWithStagingUrl_ReturnsStagingUrl()
    {
        // Arrange
        var cache = new FakeNuGetPackageCache();
        var stagingUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/darc-pub-dotnet-aspire-48a11dae/nuget/v3/index.json";
        var mappings = new[]
        {
            new PackageMapping("Aspire*", stagingUrl, MappingType.Primary),
            new PackageMapping("*", "https://api.nuget.org/v3/index.json", MappingType.Supporting)
        };

        // Act
        var channel = PackageChannel.CreateExplicitChannel("staging", PackageChannelQuality.Stable, mappings, cache, configureGlobalPackagesFolder: true);

        // Assert
        Assert.Equal(stagingUrl, channel.SourceDetails);
        Assert.Equal(PackageChannelType.Explicit, channel.Type);
        Assert.True(channel.ConfigureGlobalPackagesFolder);
    }

    [Fact]
    public void SourceDetails_EmptyMappingsArray_ReturnsBasedOnNuGetConfig()
    {
        // Arrange
        var cache = new FakeNuGetPackageCache();
        var mappings = Array.Empty<PackageMapping>();

        // Act
        var channel = PackageChannel.CreateExplicitChannel("empty", PackageChannelQuality.Stable, mappings, cache);

        // Assert
        Assert.Equal(PackagingStrings.BasedOnNuGetConfig, channel.SourceDetails);
        Assert.Equal(PackageChannelType.Explicit, channel.Type);
    }

    [Fact]
    public void PackageMapping_HasTypeProperty_WithPrimaryValue()
    {
        // Arrange & Act
        var mapping = new PackageMapping("Aspire*", "https://example.com/feed", MappingType.Primary);

        // Assert
        Assert.Equal("Aspire*", mapping.PackageFilter);
        Assert.Equal("https://example.com/feed", mapping.Source);
        Assert.Equal(MappingType.Primary, mapping.Type);
    }

    [Fact]
    public void PackageMapping_HasTypeProperty_WithSupportingValue()
    {
        // Arrange & Act
        var mapping = new PackageMapping("*", "https://api.nuget.org/v3/index.json", MappingType.Supporting);

        // Assert
        Assert.Equal("*", mapping.PackageFilter);
        Assert.Equal("https://api.nuget.org/v3/index.json", mapping.Source);
        Assert.Equal(MappingType.Supporting, mapping.Type);
    }

    [Fact]
    public void PackageChannel_MappingsHaveCorrectTypes()
    {
        // Arrange
        var cache = new FakeNuGetPackageCache();
        var mappings = new[]
        {
            new PackageMapping("Aspire*", "https://example.com/aspire", MappingType.Primary),
            new PackageMapping("*", "https://api.nuget.org/v3/index.json", MappingType.Supporting)
        };

        // Act
        var channel = PackageChannel.CreateExplicitChannel("test", PackageChannelQuality.Stable, mappings, cache);

        // Assert
        Assert.NotNull(channel.Mappings);
        Assert.Equal(2, channel.Mappings.Length);
        Assert.Equal(MappingType.Primary, channel.Mappings[0].Type);
        Assert.Equal(MappingType.Supporting, channel.Mappings[1].Type);
    }
}
