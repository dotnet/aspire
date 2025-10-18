// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class DockerImageTagHelpersTests
{
    [Theory]
    [InlineData("valid-tag", "valid-tag")]
    [InlineData("ValidTag", "validtag")]
    [InlineData("Valid_Tag", "valid_tag")]
    [InlineData("Valid.Tag", "valid.tag")]
    [InlineData("Valid-Tag-123", "valid-tag-123")]
    [InlineData("aspire-deploy-20231215120000", "aspire-deploy-20231215120000")]
    public void SanitizeTag_ValidInputs_ReturnsExpectedTag(string input, string expected)
    {
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Invalid@Tag", "invalid-tag")]
    [InlineData("Invalid#Tag", "invalid-tag")]
    [InlineData("Invalid$Tag", "invalid-tag")]
    [InlineData("Invalid Tag", "invalid-tag")]
    [InlineData("Invalid/Tag", "invalid-tag")]
    [InlineData("Invalid:Tag", "invalid-tag")]
    [InlineData("MyEnv@123", "myenv-123")]
    public void SanitizeTag_InvalidCharacters_ReplacesWithHyphens(string input, string expected)
    {
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".invalid", "invalid")]
    [InlineData("-invalid", "invalid")]
    [InlineData(".-invalid", "invalid")]
    [InlineData("-.invalid", "invalid")]
    [InlineData("...invalid", "invalid")]
    [InlineData("---invalid", "invalid")]
    public void SanitizeTag_StartsWithPeriodOrHyphen_TrimsLeadingCharacters(string input, string expected)
    {
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "aspire-deploy")]
    [InlineData("", "aspire-deploy")]
    [InlineData("   ", "aspire-deploy")]
    [InlineData("@#$%", "aspire-deploy")]
    [InlineData(".-.-.-", "aspire-deploy")]
    public void SanitizeTag_NullEmptyOrAllInvalid_ReturnsDefault(string? input, string expected)
    {
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeTag_UppercaseEnvironmentName_ConvertsToLowercase()
    {
        var input = "MyEnvironment-20231215120000";
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal("myenvironment-20231215120000", result);
    }

    [Fact]
    public void SanitizeTag_AzureContainerRegistryName_WorksCorrectly()
    {
        // Azure Container Registry names can contain uppercase letters
        var input = "octoPetsACRL5romtyajh3t2-20231215120000";
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal("octopetsacrl5romtyajh3t2-20231215120000", result);
    }

    [Fact]
    public void SanitizeTag_LongInput_TruncatesTo128Characters()
    {
        var longInput = new string('a', 150);
        var result = DockerImageTagHelpers.SanitizeTag(longInput);
        Assert.Equal(128, result.Length);
        Assert.Equal(new string('a', 128), result);
    }

    [Fact]
    public void SanitizeTag_LongInputWithInvalidPrefix_TruncatesAfterTrimming()
    {
        var longInput = ".-.-" + new string('a', 150);
        var result = DockerImageTagHelpers.SanitizeTag(longInput);
        Assert.Equal(128, result.Length);
        Assert.True(result.All(c => c == 'a'));
    }

    [Theory]
    [InlineData("env_with_underscores", "env_with_underscores")]
    [InlineData("env.with.dots", "env.with.dots")]
    [InlineData("env-with-dashes", "env-with-dashes")]
    [InlineData("env123", "env123")]
    public void SanitizeTag_ValidDockerCharacters_PreservesInput(string input, string expected)
    {
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeTag_MixedCase_ConvertedToLowerCase()
    {
        var input = "MyApp-DeployTag-ABC123";
        var result = DockerImageTagHelpers.SanitizeTag(input);
        Assert.Equal("myapp-deploytag-abc123", result);
    }
}
