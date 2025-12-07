// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

/// <summary>
/// Tests for the ConsoleActivityLogger's duration formatting logic.
/// Since the FormatDuration method is private, we test it via reflection.
/// </summary>
public class ConsoleActivityLoggerTests
{
    [Theory]
    [InlineData(0.001, "1ms")]   // 1 millisecond
    [InlineData(0.025, "25ms")]  // 25 milliseconds - example from issue
    [InlineData(0.049, "49ms")]  // Just under 50ms
    [InlineData(0.050, "50ms")]  // Exactly 50ms
    [InlineData(0.099, "99ms")]  // Just under 0.1 seconds
    [InlineData(0.0999, "100ms")] // Rounds to 100ms
    [InlineData(0.1, "0.1s")]    // Exactly 0.1 seconds should show seconds
    [InlineData(0.15, "0.2s")]   // Rounds to 0.2s
    [InlineData(0.5, "0.5s")]    // Half second
    [InlineData(1.0, "1.0s")]    // One second
    [InlineData(1.5, "1.5s")]    // 1.5 seconds
    [InlineData(1.49, "1.5s")]   // Rounds to 1.5s
    [InlineData(10.25, "10.3s")] // Larger duration (rounds to 1 decimal)
    [InlineData(0.0001, "0ms")]  // Very small value rounds to 0ms
    public void FormatDuration_FormatsCorrectly(double seconds, string expected)
    {
        // Use reflection to test the private FormatDuration method
        var type = typeof(ConsoleActivityLogger);
        var method = type.GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        
        // Act
        var result = method.Invoke(null, [seconds]) as string;
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDuration_UsesInvariantCulture()
    {
        // Verify that the formatting uses InvariantCulture (dot as decimal separator)
        // regardless of current culture
        var type = typeof(ConsoleActivityLogger);
        var method = type.GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        
        // Act - test with a value that would format differently in some cultures
        var result = method.Invoke(null, [1.5]) as string;
        
        // Assert - should use dot, not comma
        Assert.Equal("1.5s", result);
        Assert.DoesNotContain(",", result);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.01)]
    [InlineData(0.05)]
    [InlineData(0.099)]
    public void FormatDuration_NeverReturnsZeroPointZeroSeconds(double seconds)
    {
        // This test verifies the core issue: we should never see "0.0s" for small durations
        var type = typeof(ConsoleActivityLogger);
        var method = type.GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        
        // Act
        var result = method.Invoke(null, [seconds]) as string;
        
        // Assert - should never be "0.0s"
        Assert.NotEqual("0.0s", result);
    }
}
