// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Configuration;

public class FeaturesTests
{
    [Fact]
    public void Enabled_WithNoConfiguration_ReturnsDefaultValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var features = new Features(configuration);

        // Act & Assert - Test various feature flags with different defaults
        Assert.True(features.Enabled<UpdateNotificationsEnabledFeature>());
        Assert.True(features.Enabled<MinimumSdkCheckEnabledFeature>());
        Assert.False(features.Enabled<ExecCommandEnabledFeature>());
        Assert.True(features.Enabled<OrphanDetectionWithTimestampEnabledFeature>());
        Assert.False(features.Enabled<ShowDeprecatedPackagesFeature>());
        Assert.True(features.Enabled<PackageSearchDiskCachingEnabledFeature>());
        Assert.False(features.Enabled<StagingChannelEnabledFeature>());
        Assert.False(features.Enabled<DefaultWatchEnabledFeature>());
        Assert.False(features.Enabled<ShowAllTemplatesFeature>());
        Assert.True(features.Enabled<DotNetSdkInstallationEnabledFeature>());
    }

    [Fact]
    public void Enabled_WithConfigurationSetToTrue_ReturnsTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:execCommandEnabled"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert
        Assert.True(features.Enabled<ExecCommandEnabledFeature>());
    }

    [Fact]
    public void Enabled_WithConfigurationSetToFalse_ReturnsFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:updateNotificationsEnabled"] = "false"
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert
        Assert.False(features.Enabled<UpdateNotificationsEnabledFeature>());
    }

    [Fact]
    public void Enabled_WithInvalidConfigurationValue_ReturnsDefaultValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:updateNotificationsEnabled"] = "invalid"
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert - Should return default value when configuration is invalid
        Assert.True(features.Enabled<UpdateNotificationsEnabledFeature>());
    }

    [Fact]
    public void Enabled_WithEmptyConfigurationValue_ReturnsDefaultValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:updateNotificationsEnabled"] = ""
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert - Should return default value when configuration is empty
        Assert.True(features.Enabled<UpdateNotificationsEnabledFeature>());
    }

    [Fact]
    public void Enabled_MultipleFeatureFlags_EachUsesItsOwnConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:updateNotificationsEnabled"] = "false",
                ["features:execCommandEnabled"] = "true",
                ["features:showAllTemplates"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert - Each feature should use its own configuration
        Assert.False(features.Enabled<UpdateNotificationsEnabledFeature>());
        Assert.True(features.Enabled<ExecCommandEnabledFeature>());
        Assert.True(features.Enabled<ShowAllTemplatesFeature>());
        // Features not in configuration should use their defaults
        Assert.True(features.Enabled<MinimumSdkCheckEnabledFeature>());
        Assert.False(features.Enabled<ShowDeprecatedPackagesFeature>());
    }

    [Theory]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("true")]
    public void Enabled_WithVariousTrueValues_ReturnsTrue(string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:execCommandEnabled"] = value
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert
        Assert.True(features.Enabled<ExecCommandEnabledFeature>());
    }

    [Theory]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("false")]
    public void Enabled_WithVariousFalseValues_ReturnsFalse(string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:updateNotificationsEnabled"] = value
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert
        Assert.False(features.Enabled<UpdateNotificationsEnabledFeature>());
    }

    [Fact]
    public void IsFeatureEnabled_LegacyMethod_StillWorks()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:customFeature"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act & Assert - Legacy method should still work for backward compatibility
        Assert.True(features.IsFeatureEnabled("customFeature", defaultValue: false));
        Assert.False(features.IsFeatureEnabled("nonExistentFeature", defaultValue: false));
        Assert.True(features.IsFeatureEnabled("nonExistentFeature", defaultValue: true));
    }

    [Fact]
    public void FeatureFlags_HaveCorrectConfigurationKeys()
    {
        // Assert - Verify that each feature flag has the expected configuration key
        Assert.Equal("updateNotificationsEnabled", new UpdateNotificationsEnabledFeature().ConfigurationKey);
        Assert.Equal("minimumSdkCheckEnabled", new MinimumSdkCheckEnabledFeature().ConfigurationKey);
        Assert.Equal("execCommandEnabled", new ExecCommandEnabledFeature().ConfigurationKey);
        Assert.Equal("orphanDetectionWithTimestampEnabled", new OrphanDetectionWithTimestampEnabledFeature().ConfigurationKey);
        Assert.Equal("showDeprecatedPackages", new ShowDeprecatedPackagesFeature().ConfigurationKey);
        Assert.Equal("packageSearchDiskCachingEnabled", new PackageSearchDiskCachingEnabledFeature().ConfigurationKey);
        Assert.Equal("stagingChannelEnabled", new StagingChannelEnabledFeature().ConfigurationKey);
        Assert.Equal("defaultWatchEnabled", new DefaultWatchEnabledFeature().ConfigurationKey);
        Assert.Equal("showAllTemplates", new ShowAllTemplatesFeature().ConfigurationKey);
        Assert.Equal("dotnetSdkInstallationEnabled", new DotNetSdkInstallationEnabledFeature().ConfigurationKey);
    }

    [Fact]
    public void FeatureFlags_HaveCorrectDefaultValues()
    {
        // Assert - Verify that each feature flag has the expected default value
        Assert.True(new UpdateNotificationsEnabledFeature().DefaultValue);
        Assert.True(new MinimumSdkCheckEnabledFeature().DefaultValue);
        Assert.False(new ExecCommandEnabledFeature().DefaultValue);
        Assert.True(new OrphanDetectionWithTimestampEnabledFeature().DefaultValue);
        Assert.False(new ShowDeprecatedPackagesFeature().DefaultValue);
        Assert.True(new PackageSearchDiskCachingEnabledFeature().DefaultValue);
        Assert.False(new StagingChannelEnabledFeature().DefaultValue);
        Assert.False(new DefaultWatchEnabledFeature().DefaultValue);
        Assert.False(new ShowAllTemplatesFeature().DefaultValue);
        Assert.True(new DotNetSdkInstallationEnabledFeature().DefaultValue);
    }
}
