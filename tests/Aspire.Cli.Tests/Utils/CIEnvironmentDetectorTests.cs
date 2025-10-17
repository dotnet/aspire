// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Utils;

public class CIEnvironmentDetectorTests
{
    [Fact]
    public void IsCI_ReturnsFalse_WhenNoCIVariablesSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        
        // Act
        var detector = new CIEnvironmentDetector(configuration);
        
        // Assert
        Assert.False(detector.IsCI);
    }

    [Theory]
    [InlineData("CI", "true", true)]
    [InlineData("CI", "1", true)]
    [InlineData("CI", "false", false)]
    [InlineData("CI", "0", false)]
    [InlineData("CI", "something", false)]
    [InlineData("GITHUB_ACTIONS", "true", true)]
    [InlineData("GITHUB_ACTIONS", "false", true)] // Any value for non-CI var means CI
    [InlineData("AZURE_PIPELINES", "True", true)]
    [InlineData("TF_BUILD", "1", true)]
    [InlineData("JENKINS_URL", "http://jenkins", true)]
    [InlineData("GITLAB_CI", "true", true)]
    [InlineData("CIRCLECI", "true", true)]
    [InlineData("TRAVIS", "true", true)]
    [InlineData("BUILDKITE", "true", true)]
    [InlineData("APPVEYOR", "True", true)]
    [InlineData("TEAMCITY_VERSION", "2024.1", true)]
    [InlineData("BITBUCKET_BUILD_NUMBER", "123", true)]
    [InlineData("CODEBUILD_BUILD_ID", "build-123", true)]
    public void IsCI_DetectsCorrectly_BasedOnConfiguration(string envVar, string value, bool expectedIsCI)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [envVar] = value
            })
            .Build();
        
        // Act
        var detector = new CIEnvironmentDetector(configuration);
        
        // Assert
        Assert.Equal(expectedIsCI, detector.IsCI);
    }

    [Fact]
    public void IsCI_ReturnsTrue_WhenMultipleCIVariablesSet()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GITHUB_ACTIONS"] = "true",
                ["CI"] = "true"
            })
            .Build();
        
        // Act
        var detector = new CIEnvironmentDetector(configuration);
        
        // Assert
        Assert.True(detector.IsCI);
    }
}
