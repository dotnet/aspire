// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;

namespace Aspire.Cli.Tests.Commands;

public class ProjectNameValidatorTests
{
    [Theory]
    [InlineData("项目1", true)]  // Chinese
    [InlineData("Проект1", true)]  // Cyrillic
    [InlineData("プロジェクト1", true)]  // Japanese
    [InlineData("مشروع1", true)]  // Arabic
    [InlineData("Project_1", true)]  // Latin with underscore
    [InlineData("Project-1", true)]  // Latin with dash
    [InlineData("Project.1", true)]  // Latin with dot
    [InlineData("MyApp", true)]  // Simple ASCII
    [InlineData("A", true)]  // Single character
    [InlineData("1", true)]  // Single number
    [InlineData("プ", true)]  // Single Unicode character
    [InlineData("Test123", true)]  // Mixed letters and numbers
    [InlineData("My_Cool-Project.v2", true)]  // Complex valid name
    [InlineData("Project:1", true)]  // Colon (now allowed)
    [InlineData("Project*1", true)]  // Asterisk (now allowed)
    [InlineData("Project?1", true)]  // Question mark (now allowed)
    [InlineData("Project\"1", true)]  // Quote (now allowed)
    [InlineData("Project<1", true)]  // Less than (now allowed)
    [InlineData("Project>1", true)]  // Greater than (now allowed)
    [InlineData("Project|1", true)]  // Pipe (now allowed)
    [InlineData("Project ", true)]  // Ends with space (now allowed)
    [InlineData(" Project", true)]  // Starts with space (now allowed)
    [InlineData("Pro ject", true)]  // Space in middle (now allowed)
    [InlineData("-Project", true)]  // Starts with dash (now allowed)
    [InlineData("Project-", true)]  // Ends with dash (now allowed)
    [InlineData(".Project", true)]  // Starts with dot (now allowed)
    [InlineData("Project.", true)]  // Ends with dot (now allowed)
    [InlineData("_Project", true)]  // Starts with underscore (now allowed)
    [InlineData("Project_", true)]  // Ends with underscore (now allowed)
    public void IsProjectNameValid_ValidNames_ReturnsTrue(string projectName, bool expected)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Project/1", false)]  // Forward slash (path separator)
    [InlineData("Project\\1", false)]  // Backslash (path separator)
    [InlineData("", false)]  // Empty string
    [InlineData(" ", false)]  // Space only
    [InlineData("   ", false)]  // Multiple spaces only
    [InlineData("\t", false)]  // Tab only
    [InlineData("\n", false)]  // Newline only
    public void IsProjectNameValid_InvalidNames_ReturnsFalse(string projectName, bool expected)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsProjectNameValid_MaxLength254_ReturnsTrue()
    {
        // Arrange
        var projectName = new string('A', 254);

        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsProjectNameValid_Length255_ReturnsFalse()
    {
        // Arrange
        var projectName = new string('A', 255);

        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("项目测试名称很长的中文项目名称")]  // Long Chinese name
    [InlineData("очень_длинное_русское_имя_проекта")]  // Long Russian name
    [InlineData("とても長い日本語のプロジェクト名")]  // Long Japanese name
    [InlineData("اسم_مشروع_طويل_جدا_بالعربية")]  // Long Arabic name
    public void IsProjectNameValid_LongUnicodeNames_ReturnsTrue(string projectName)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.True(result, $"Unicode project name should be valid: {projectName}");
    }

    [Theory]
    [InlineData("Ελληνικά", true)]  // Greek
    [InlineData("עברית", true)]  // Hebrew
    [InlineData("हिन्दी", true)]  // Hindi
    [InlineData("ไทย", true)]  // Thai
    [InlineData("한국어", true)]  // Korean
    [InlineData("Türkçe", true)]  // Turkish
    [InlineData("Português", true)]  // Portuguese with accent
    [InlineData("Français", true)]  // French with accent
    [InlineData("Español", true)]  // Spanish with accent
    [InlineData("Deutsch", true)]  // German
    public void IsProjectNameValid_VariousLanguages_ReturnsTrue(string projectName, bool expected)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Test123-Project_Name.v2")]  // Complex valid with all allowed characters
    [InlineData("A1-B2_C3.D4")]  // Mixed with separators
    [InlineData("项目-测试_版本.1")]  // Unicode with separators
    public void IsProjectNameValid_ComplexValidNames_ReturnsTrue(string projectName)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.True(result, $"Complex valid project name should be valid: {projectName}");
    }

    [Theory]
    [InlineData("Test..Name")]  // Double dot
    [InlineData("Test--Name")]  // Double dash
    [InlineData("Test__Name")]  // Double underscore
    public void IsProjectNameValid_ConsecutiveSpecialChars_ReturnsTrue(string projectName)
    {
        // These should be valid as the spec doesn't prohibit consecutive allowed characters
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.True(result, $"Consecutive allowed characters should be valid: {projectName}");
    }

    [Theory]
    [InlineData("My/Project")]  // Forward slash in middle
    [InlineData("/MyProject")]  // Forward slash at start
    [InlineData("MyProject/")]  // Forward slash at end
    [InlineData("My\\Project")]  // Backslash in middle
    [InlineData("\\MyProject")]  // Backslash at start
    [InlineData("MyProject\\")]  // Backslash at end
    [InlineData("My/Project/Name")]  // Multiple forward slashes
    [InlineData("My\\Project\\Name")]  // Multiple backslashes
    [InlineData("My/Project\\Name")]  // Mixed path separators
    public void IsProjectNameValid_PathSeparators_ReturnsFalse(string projectName)
    {
        // Act
        var result = ProjectNameValidator.IsProjectNameValid(projectName);

        // Assert
        Assert.False(result, $"Project name with path separators should be invalid: {projectName}");
    }
}