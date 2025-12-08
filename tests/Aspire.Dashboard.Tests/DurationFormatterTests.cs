// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class DurationFormatterTests
{
    [Theory]
    [InlineData(0, "μs")]
    [InlineData(1, "μs")]
    [InlineData(10, "μs")]
    [InlineData(100, "ms")]
    [InlineData(1_000, "ms")]
    [InlineData(100_000, "ms")]
    [InlineData(1_000_000, "s")]
    [InlineData(1_000_000_000, "s")]
    [InlineData(1_000_000_000_000, "h")]
    [InlineData(1_000_000_000_000_000, "h")]
    [InlineData(1_000_000_000_000_000_000, "h")]
    public void GetUnit(long ticks, string unit)
    {
        Assert.Equal(unit, global::Aspire.Shared.DurationFormatter.GetUnit(TimeSpan.FromTicks(ticks)));
    }

    [Fact]
    public void KeepsMicrosecondsTheSame()
    {
        Assert.Equal("1μs", global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(1 * TimeSpan.TicksPerMicrosecond), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysMaximumOf2UnitsAndRoundsLastOne()
    {
        var input = 10 * TimeSpan.TicksPerDay + 13 * TimeSpan.TicksPerHour + 30 * TimeSpan.TicksPerMinute;
        Assert.Equal("10d 14h", global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void SkipsUnitsThatAreEmpty()
    {
        var input = 2 * TimeSpan.TicksPerDay + 5 * TimeSpan.TicksPerMinute;
        Assert.Equal("2d", global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysMillisecondsInDecimals()
    {
        var input = 2 * TimeSpan.TicksPerMillisecond + 357 * TimeSpan.TicksPerMicrosecond;
        Assert.Equal(2.36m.ToString("0.##ms", CultureInfo.CurrentCulture), global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysSecondsInDecimals()
    {
        var input = 2 * TimeSpan.TicksPerSecond + 357 * TimeSpan.TicksPerMillisecond;
        Assert.Equal(2.36m.ToString("0.##s", CultureInfo.CurrentCulture), global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysMinutesInSplitUnits()
    {
        var input = 2 * TimeSpan.TicksPerMinute + 30 * TimeSpan.TicksPerSecond + 555 * TimeSpan.TicksPerMillisecond;
        Assert.Equal("2m 31s", global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysHoursInSplitUnits()
    {
        var input = 2 * TimeSpan.TicksPerHour + 30 * TimeSpan.TicksPerMinute + 30 * TimeSpan.TicksPerSecond;
        Assert.Equal("2h 31m", global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysLargeFractionalMillisecondAsMilliseconds()
    {
        var input = 9155;
        Assert.Equal(0.92m.ToString("0.##ms", CultureInfo.CurrentCulture), global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysLargeFractionalSecondsAsSeconds()
    {
        var input = 915 * TimeSpan.TicksPerMillisecond;
        Assert.Equal(0.92m.ToString("0.##s", CultureInfo.CurrentCulture), global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysTimesLessThanMicroseconds()
    {
        var input = (double)TimeSpan.TicksPerMicrosecond / 10;
        Assert.Equal(0.1m.ToString("0.##μs", CultureInfo.CurrentCulture), global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks((long)input), CultureInfo.CurrentCulture));
    }

    [Fact]
    public void DisplaysTimesOf0()
    {
        var input = 0;
        Assert.Equal("0μs", global::Aspire.Shared.DurationFormatter.FormatDuration(TimeSpan.FromTicks(input), CultureInfo.CurrentCulture));
    }
}
