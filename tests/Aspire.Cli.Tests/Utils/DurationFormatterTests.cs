// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Shared;

namespace Aspire.Cli.Tests.Utils;

/// <summary>
/// Tests for the DurationFormatter used by ConsoleActivityLogger.
/// These tests ensure the formatter handles CLI scenarios correctly, including
/// the original issue where very quick operations displayed "0.0s".
/// </summary>
public class DurationFormatterTests
{
    [Theory]
    [InlineData(0.001, "1.00ms")]   // 1 millisecond
    [InlineData(0.025, "25.00ms")]  // 25 milliseconds - example from original issue
    [InlineData(0.049, "49.00ms")]  // Just under 50ms
    [InlineData(0.050, "50.00ms")]  // Exactly 50ms
    [InlineData(0.099, "99.00ms")]  // Just under 0.1 seconds
    [InlineData(0.0999, "99.90ms")] // 99.9ms (DurationFormatter uses decimals)
    [InlineData(0.1, "0.10s")]    // Exactly 0.1 seconds should show seconds
    [InlineData(0.15, "0.15s")]  // 0.15 seconds
    [InlineData(0.5, "0.50s")]    // Half second
    [InlineData(1.0, "1.00s")]      // One second
    [InlineData(1.5, "1.50s")]    // 1.5 seconds
    [InlineData(1.49, "1.49s")]  // 1.49 seconds
    [InlineData(10.25, "10.25s")] // Larger duration
    [InlineData(0.0001, "0.10ms")] // Very small value
    public void FormatDuration_FormatsSmallDurationsCorrectly(double seconds, string expected)
    {
        // Act - CLI tests use DecimalDurationDisplay.Fixed for alignment
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(seconds), System.Globalization.CultureInfo.InvariantCulture, DecimalDurationDisplay.Fixed);
        
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
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(seconds), System.Globalization.CultureInfo.InvariantCulture);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0, "0.00μs")]      // Zero should not display as "0.0s"
    [InlineData(0.01, "10.00ms")]    // 10 milliseconds
    [InlineData(0.05, "50.00ms")]    // 50 milliseconds
    [InlineData(0.099, "99.00ms")]   // 99 milliseconds
    public void FormatDuration_NeverReturnsZeroPointZeroSeconds(double seconds, string expected)
    {
        // This test verifies the core issue: we should never see "0.0s" for small durations
        // Act - CLI tests use DecimalDurationDisplay.Fixed for alignment
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(seconds), System.Globalization.CultureInfo.InvariantCulture, DecimalDurationDisplay.Fixed);
        
        // Assert - should never be "0.0s" and should match expected format
        Assert.NotEqual("0.0s", result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDuration_UsesInvariantCulture()
    {
        // Verify that the formatting uses InvariantCulture (dot as decimal separator)
        // regardless of current culture
        
        // Act - CLI tests use DecimalDurationDisplay.Fixed for alignment
        var result = DurationFormatter.FormatDuration(TimeSpan.FromSeconds(1.5), null, DecimalDurationDisplay.Fixed);
        
        // Assert - should use dot, not comma (with fixed decimal places)
        Assert.Equal("1.50s", result);
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

    [Theory]
    [InlineData(0.001, "1.00ms")]   // 1 millisecond with fixed decimals
    [InlineData(0.025, "25.00ms")]  // 25 milliseconds with fixed decimals
    [InlineData(0.1, "0.10s")]      // 0.1 seconds with fixed decimals
    [InlineData(1.0, "1.00s")]      // 1 second with fixed decimals
    [InlineData(1.5, "1.50s")]      // 1.5 seconds with fixed decimals
    [InlineData(0.0, "0.00μs")]     // Zero with fixed decimals
    public void FormatDuration_WithFixedDisplay_AlwaysShowsTwoDecimalPlaces(double seconds, string expected)
    {
        // Act - explicitly use DecimalDurationDisplay.Fixed
        var result = DurationFormatter.FormatDuration(
            TimeSpan.FromSeconds(seconds), 
            System.Globalization.CultureInfo.InvariantCulture, 
            DecimalDurationDisplay.Fixed);
        
        // Assert - should always have .00 format
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.001, "1ms")]      // 1 millisecond without trailing zeros
    [InlineData(0.025, "25ms")]     // 25 milliseconds without trailing zeros
    [InlineData(0.1, "0.1s")]       // 0.1 seconds without trailing zeros
    [InlineData(1.0, "1s")]         // 1 second without trailing zeros
    [InlineData(1.5, "1.5s")]       // 1.5 seconds shows one decimal
    [InlineData(0.0, "0μs")]        // Zero without trailing zeros
    [InlineData(1.25, "1.25s")]     // 1.25 seconds shows two decimals
    public void FormatDuration_WithOptionalDisplay_HidesTrailingZeros(double seconds, string expected)
    {
        // Act - explicitly use DecimalDurationDisplay.Optional (also the default)
        var result = DurationFormatter.FormatDuration(
            TimeSpan.FromSeconds(seconds), 
            System.Globalization.CultureInfo.InvariantCulture, 
            DecimalDurationDisplay.Optional);
        
        // Assert - should use 0.## format, hiding trailing zeros
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDuration_DefaultBehavior_UsesOptionalDisplay()
    {
        // Verify that the default parameter value is Optional
        var duration = TimeSpan.FromSeconds(1.0);
        
        // Act - call without specifying the parameter
        var defaultResult = DurationFormatter.FormatDuration(duration, System.Globalization.CultureInfo.InvariantCulture);
        var explicitOptionalResult = DurationFormatter.FormatDuration(duration, System.Globalization.CultureInfo.InvariantCulture, DecimalDurationDisplay.Optional);
        
        // Assert - both should produce the same result
        Assert.Equal(explicitOptionalResult, defaultResult);
        Assert.Equal("1s", defaultResult); // Should not have trailing zeros
    }
}

