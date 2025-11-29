// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Configuration;

public class FeaturesTests
{
    [Fact]
    public void IsFeatureEnabled_ReturnsDefaultValue_WhenNotConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled("someFeature", defaultValue: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalse_WhenConfiguredFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:someFeature"] = "false"
            })
            .Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled("someFeature", defaultValue: true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsTrue_WhenConfiguredTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:someFeature"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled("someFeature", defaultValue: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFeatureEnabled_NonInteractiveSdkInstall_ReturnsTrue_WhenEnvVarSet()
    {
        // Arrange - Test the dedicated environment variable ASPIRE_NON_INTERACTIVE_SDK_INSTALL
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_NON_INTERACTIVE_SDK_INSTALL"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled(KnownFeatures.NonInteractiveSdkInstall, defaultValue: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFeatureEnabled_NonInteractiveSdkInstall_ReturnsFalse_WhenEnvVarSetToFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_NON_INTERACTIVE_SDK_INSTALL"] = "false"
            })
            .Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled(KnownFeatures.NonInteractiveSdkInstall, defaultValue: true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFeatureEnabled_NonInteractiveSdkInstall_PrefersFeatureConfig_OverEnvVar()
    {
        // Arrange - When both are set, the features: config should take precedence
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["features:nonInteractiveSdkInstall"] = "false",
                ["ASPIRE_NON_INTERACTIVE_SDK_INSTALL"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled(KnownFeatures.NonInteractiveSdkInstall, defaultValue: true);

        // Assert - features: config should win
        Assert.False(result);
    }

    [Fact]
    public void IsFeatureEnabled_NonInteractiveSdkInstall_FallsBackToEnvVar_WhenFeatureNotSet()
    {
        // Arrange - Only environment variable is set
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_NON_INTERACTIVE_SDK_INSTALL"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act
        var result = features.IsFeatureEnabled(KnownFeatures.NonInteractiveSdkInstall, defaultValue: false);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFeatureEnabled_OtherFeatures_DoNotCheckEnvVar()
    {
        // Arrange - Verify other features don't check the SDK install env var
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_NON_INTERACTIVE_SDK_INSTALL"] = "true"
            })
            .Build();
        var features = new Features(configuration);

        // Act - Check a different feature
        var result = features.IsFeatureEnabled(KnownFeatures.DotNetSdkInstallationEnabled, defaultValue: false);

        // Assert - Should return default value, not be affected by the env var
        Assert.False(result);
    }
}
