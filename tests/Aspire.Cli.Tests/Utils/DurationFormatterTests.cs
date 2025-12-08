// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

/// <summary>
/// Tests for the DurationFormatter used by ConsoleActivityLogger.
/// These tests ensure the formatter handles CLI scenarios correctly, including
/// the original issue where very quick operations displayed "0.0s".
/// </summary>
public class DurationFormatterTests
{
    [Theory]
    [InlineData(0.001, "1ms")]   // 1 millisecond
    [InlineData(0.025, "25ms")]  // 25 milliseconds - example from original issue
    [InlineData(0.049, "49ms")]  // Just under 50ms
    [InlineData(0.050, "50ms")]  // Exactly 50ms
    [InlineData(0.099, "99ms")]  // Just under 0.1 seconds
    [InlineData(0.0999, "99.9ms")] // 99.9ms (DurationFormatter uses decimals)
    [InlineData(0.1, "0.1s")]    // Exactly 0.1 seconds should show seconds
    [InlineData(0.15, "0.15s")]  // 0.15 seconds
    [InlineData(0.5, "0.5s")]    // Half second
    [InlineData(1.0, "1s")]      // One second
    [InlineData(1.5, "1.5s")]    // 1.5 seconds
    [InlineData(1.49, "1.49s")]  // 1.49 seconds
    [InlineData(10.25, "10.25s")] // Larger duration
    [InlineData(0.0001, "0.1ms")] // Very small value
    public void FormatDuration_FormatsSmallDurationsCorrectly(double seconds, string expected)
    {
        // Act
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(seconds));
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(60, "1m")]        // 1 minute (no seconds displayed when 0)
    [InlineData(90, "1m 30s")]    // 1 minute 30 seconds
    [InlineData(150, "2m 30s")]   // 2 minutes 30 seconds
    [InlineData(3600, "1h")]      // 1 hour (no minutes when 0)
    [InlineData(3661, "1h 1m")]   // 1 hour 1 minute 1 second (rounds to 1m)
    [InlineData(7200, "2h")]      // 2 hours (no minutes when 0)
    public void FormatDuration_FormatsLongerDurationsWithMultipleUnits(double seconds, string expected)
    {
        // Act
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(seconds));
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0, "0μs")]      // Zero should not display as "0.0s"
    [InlineData(0.01, "10ms")]    // 10 milliseconds
    [InlineData(0.05, "50ms")]    // 50 milliseconds
    [InlineData(0.099, "99ms")]   // 99 milliseconds
    public void FormatDuration_NeverReturnsZeroPointZeroSeconds(double seconds, string expected)
    {
        // This test verifies the core issue: we should never see "0.0s" for small durations
        // Act
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(seconds));
        
        // Assert - should never be "0.0s" and should match expected format
        Assert.NotEqual("0.0s", result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDuration_UsesInvariantCulture()
    {
        // Verify that the formatting uses InvariantCulture (dot as decimal separator)
        // regardless of current culture
        
        // Act - test with a value that would format differently in some cultures
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(1.5));
        
        // Assert - should use dot, not comma
        Assert.Equal("1.5s", result);
        Assert.DoesNotContain(",", result);
    }

    [Fact]
    public void FormatDuration_HandlesVeryLargeDurations()
    {
        // Test that large durations are formatted with appropriate units
        var oneDayInSeconds = 24 * 60 * 60;
        
        // Act
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(oneDayInSeconds));
        
        // Assert - should show days (no hours when 0)
        Assert.Equal("1d", result);
    }

    [Fact]
    public void FormatDuration_HandlesMixedUnitsCorrectly()
    {
        // Test the example from the original DurationFormatter tests
        var duration = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(30) + TimeSpan.FromMilliseconds(555);
        
        // Act
        var result = DurationFormatter.FormatDuration(duration);
        
        // Assert - should round seconds appropriately
        Assert.Equal("2m 31s", result);
    }
}
