// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Xunit;

namespace Aspire.Cli.Tests.Utils;

public class ProjectNameValidatorTests
{
    [Theory]
    [InlineData("validName", "validName")]
    [InlineData("valid_name", "valid_name")]
    [InlineData("valid.name", "valid.name")]
    [InlineData("valid_name_1", "valid_name_1")]
    [InlineData("invalid@name", "invalid_name")]
    [InlineData("invalid$name", "invalid_name")]
    [InlineData("invalid-name", "invalid_name")]
    [InlineData("invalid+name", "invalid_name")]
    [InlineData("invalid name", "invalid_name")]
    public void SanitizeProjectName_ConvertInvalidChars(string input, string expectedOutput)
    {
        // Act
        var result = ProjectNameValidator.SanitizeProjectName(input);

        // Assert
        Assert.Equal(expectedOutput, result);
    }

    [Theory]
    [InlineData("validName", true)]
    [InlineData("valid_name", true)]
    [InlineData("valid.name", true)]
    [InlineData("valid_name_1", true)]
    [InlineData("invalid@name", false)]
    [InlineData("invalid$name", false)]
    [InlineData("invalid-name", false)]
    [InlineData("invalid+name", false)]
    [InlineData("invalid name", false)]
    [InlineData("-invalidName", false)]
    [InlineData("invalidName-", false)]
    [InlineData("@invalidName", false)]
    [InlineData("invalidName@", false)]
    public void IsProjectNameValid_ReturnsExpectedResult(string projectName, bool expectedResult)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}