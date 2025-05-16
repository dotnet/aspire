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
    [InlineData(".", "_")]
    [InlineData(
        "0123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790123456787901234567879", "0123456787901234567879012345678790123456787901234567879012345678790123456787901234567879012345678790")]
    public void IsProjectNameValid_ReturnsExpectedResult(string projectName, string expectedSanitized)
    {
        // Valid names are recognized as valid
        Assert.Equal(projectName == expectedSanitized, ProjectNameValidator.IsProjectNameValid(projectName));

        // Sanitized names are recognized as valid
        Assert.True(ProjectNameValidator.IsProjectNameValid(ProjectNameValidator.SanitizeProjectName(projectName)));

        // Sanitization should produce expected result
        Assert.Equal(expectedSanitized, ProjectNameValidator.SanitizeProjectName(projectName));
    }
}
