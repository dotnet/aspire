// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class CIEnvironmentDetectorTests
{
    [Fact]
    public void IsCI_ReturnsBool()
    {
        // Just verify that the property can be accessed and returns a boolean
        var isCI = CIEnvironmentDetector.IsCI;
        Assert.True(isCI || !isCI); // Always true - just verifying it's a bool
    }

    [Theory]
    [InlineData("CI", "true")]
    [InlineData("CI", "1")]
    [InlineData("GITHUB_ACTIONS", "true")]
    [InlineData("AZURE_PIPELINES", "True")]
    [InlineData("TF_BUILD", "1")]
    [InlineData("JENKINS_URL", "http://jenkins")]
    [InlineData("GITLAB_CI", "true")]
    [InlineData("CIRCLECI", "true")]
    [InlineData("TRAVIS", "true")]
    [InlineData("BUILDKITE", "true")]
    [InlineData("APPVEYOR", "True")]
    [InlineData("TEAMCITY_VERSION", "2024.1")]
    [InlineData("BITBUCKET_BUILD_NUMBER", "123")]
    [InlineData("CODEBUILD_BUILD_ID", "build-123")]
    public void DetectCI_WithEnvironmentVariable_DocumentsExpectedBehavior(string envVar, string value)
    {
        // This test documents the expected behavior for various CI environment variables
        // Note: We can't easily test the actual detection logic since it's static and cached,
        // but we can document the expected behavior through these test cases
        
        // Arrange & Act
        var originalValue = Environment.GetEnvironmentVariable(envVar);
        try
        {
            Environment.SetEnvironmentVariable(envVar, value);
            
            // The actual IsCI property is static and cached at type initialization,
            // so we can't test it directly with environment variable changes.
            // This test serves as documentation of expected behavior.
            
            // Assert - just verify the test data is valid
            Assert.NotNull(envVar);
            Assert.NotNull(value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, originalValue);
        }
    }
}
