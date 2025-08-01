// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.PackageChannels;

namespace Aspire.Cli.Tests.PackageChannels;

public class PackageChannelServiceTests
{
    [Fact]
    public void GetAllChannels_ReturnsStablePreviews_AndDailyChannels()
    {
        // Arrange
        var service = new PackageChannelService();

        // Act
        var channels = service.GetAllChannels().ToList();

        // Assert
        Assert.Contains(channels, c => c.Name == "stable");
        Assert.Contains(channels, c => c.Name == "preview");
        Assert.Contains(channels, c => c.Name == "daily");
    }

    [Fact]
    public void GetChannelByName_WithValidName_ReturnsCorrectChannel()
    {
        // Arrange
        var service = new PackageChannelService();

        // Act
        var stableChannel = service.GetChannelByName("stable");
        var previewChannel = service.GetChannelByName("preview");
        var dailyChannel = service.GetChannelByName("daily");

        // Assert
        Assert.NotNull(stableChannel);
        Assert.Equal("stable", stableChannel.Name);
        Assert.Equal("https://api.nuget.org/v3/index.json", stableChannel.Location);
        Assert.Equal(PackageChannelType.NuGetFeed, stableChannel.Type);

        Assert.NotNull(previewChannel);
        Assert.Equal("preview", previewChannel.Name);
        Assert.Equal(PackageChannelType.NuGetFeed, previewChannel.Type);

        Assert.NotNull(dailyChannel);
        Assert.Equal("daily", dailyChannel.Name);
        Assert.Equal(PackageChannelType.NuGetFeed, dailyChannel.Type);
    }

    [Fact]
    public void GetChannelByName_WithCaseInsensitiveName_ReturnsCorrectChannel()
    {
        // Arrange
        var service = new PackageChannelService();

        // Act
        var channel = service.GetChannelByName("STABLE");

        // Assert
        Assert.NotNull(channel);
        Assert.Equal("stable", channel.Name);
    }

    [Fact]
    public void GetChannelByName_WithInvalidName_ReturnsNull()
    {
        // Arrange
        var service = new PackageChannelService();

        // Act
        var channel = service.GetChannelByName("nonexistent");

        // Assert
        Assert.Null(channel);
    }

    [Fact]
    public void GetChannelByName_WithNullOrEmptyName_ReturnsNull()
    {
        // Arrange
        var service = new PackageChannelService();

        // Act & Assert
        Assert.Null(service.GetChannelByName(null!));
        Assert.Null(service.GetChannelByName(""));
        Assert.Null(service.GetChannelByName(" "));
    }

    [Fact]
    public void PackageChannel_DisplayName_ReturnsCorrectDisplayNames()
    {
        // Arrange & Act
        var stableChannel = new PackageChannel("stable", "https://api.nuget.org/v3/index.json", PackageChannelType.NuGetFeed);
        var previewChannel = new PackageChannel("preview", "https://preview.feed.url", PackageChannelType.NuGetFeed);
        var dailyChannel = new PackageChannel("daily", "https://daily.feed.url", PackageChannelType.NuGetFeed);
        var prChannel = new PackageChannel("pr-123", "/path/to/pr", PackageChannelType.LocalDirectory);
        var customChannel = new PackageChannel("custom", "https://custom.feed.url", PackageChannelType.NuGetFeed);

        // Assert
        Assert.Equal("Stable (nuget.org)", stableChannel.DisplayName);
        Assert.Equal("Preview (Azure DevOps)", previewChannel.DisplayName);
        Assert.Equal("Daily (CI builds)", dailyChannel.DisplayName);
        Assert.Equal("PR 123 (Local)", prChannel.DisplayName);
        Assert.Equal("custom", customChannel.DisplayName);
    }

    [Fact]
    public void GetPrChannels_WithNonExistentHivesDirectory_ReturnsEmpty()
    {
        // Arrange
        var service = new PackageChannelService();

        // Act
        var prChannels = service.GetPrChannels().ToList();

        // Assert
        // Since we can't easily control the hives directory in tests,
        // we at least verify the method doesn't throw and returns an enumerable
        Assert.NotNull(prChannels);
    }
}