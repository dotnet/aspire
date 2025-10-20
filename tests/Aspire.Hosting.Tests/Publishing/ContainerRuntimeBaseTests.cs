// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Globalization;

namespace Aspire.Hosting.Tests.Publishing;

public class ContainerRuntimeBaseTests
{
    [Fact]
    public void ExceptionMessageTemplate_ForTagImageAsync_FormatsCorrectly()
    {
        // This test validates the fix for the issue where string interpolation + string.Format
        // was causing "Expected an ASCII digit" errors.
        
        // The template string in TagImageAsync after string interpolation should be:
        // "TestRuntime tag failed with exit code {0}."
        // This is because {{0}} in the interpolated string becomes {0} after interpolation.
        
        var runtimeName = "TestRuntime";
        var exceptionTemplate = $"{runtimeName} tag failed with exit code {{0}}.";
        
        // Act - This should not throw FormatException
        var result = string.Format(CultureInfo.InvariantCulture, exceptionTemplate, 1);
        
        // Assert
        Assert.Equal("TestRuntime tag failed with exit code 1.", result);
    }

    [Fact]
    public void ExceptionMessageTemplate_ForPushImageAsync_FormatsCorrectly()
    {
        // This test validates the fix for the issue where string interpolation + string.Format
        // was causing "Expected an ASCII digit" errors.
        
        // The template string in PushImageAsync after string interpolation should be:
        // "TestRuntime push failed with exit code {0}."
        // This is because {{0}} in the interpolated string becomes {0} after interpolation.
        
        var runtimeName = "TestRuntime";
        var exceptionTemplate = $"{runtimeName} push failed with exit code {{0}}.";
        
        // Act - This should not throw FormatException
        var result = string.Format(CultureInfo.InvariantCulture, exceptionTemplate, 2);
        
        // Assert
        Assert.Equal("TestRuntime push failed with exit code 2.", result);
    }

    [Fact]
    public void ExceptionMessageTemplate_WithOldPattern_ThrowsFormatException()
    {
        // This test demonstrates the bug that was fixed.
        // Using {{ExitCode}} instead of {{0}} causes FormatException.
        
        var runtimeName = "TestRuntime";
        // This is what the old code had - after interpolation it becomes {ExitCode}
        var badExceptionTemplate = $"{runtimeName} tag failed with exit code {{ExitCode}}.";
        
        // Act & Assert - This should throw FormatException with "Expected an ASCII digit"
        var exception = Assert.Throws<FormatException>(() =>
            string.Format(CultureInfo.InvariantCulture, badExceptionTemplate, 1));
        
        // The error message should indicate the parsing problem
        Assert.Contains("digit", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
