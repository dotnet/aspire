// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Otlp.Model;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class DurationFormatterTests
{
    [Fact]
    public void KeepsMicrosecondsTheSame()
    {
        Assert.Equal("1μs", DurationFormatter.FormatDuration(TimeSpan.FromTicks(1 * TimeSpan.TicksPerMicrosecond)));
    }

    [Fact]
    public void DisplaysMaximumOf2UnitsAndRoundsLastOne()
    {
        var input = 10 * TimeSpan.TicksPerDay + 13 * TimeSpan.TicksPerHour + 30 * TimeSpan.TicksPerMinute;
        Assert.Equal("10d 14h", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [Fact]
    public void SkipsUnitsThatAreEmpty()
    {
        var input = 2 * TimeSpan.TicksPerDay + 5 * TimeSpan.TicksPerMinute;
        Assert.Equal("2d", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [Fact]
    public void DisplaysMillisecondsInDecimals()
    {
        var input = 2 * TimeSpan.TicksPerMillisecond + 357 * TimeSpan.TicksPerMicrosecond;
        Assert.Equal(2.36m.ToString("0.##ms", CultureInfo.CurrentCulture), DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [Fact]
    public void DisplaysSecondsInDecimals()
    {
        var input = 2 * TimeSpan.TicksPerSecond + 357 * TimeSpan.TicksPerMillisecond;
        Assert.Equal(2.36m.ToString("0.##s", CultureInfo.CurrentCulture), DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [Fact]
    public void DisplaysMinutesInSplitUnits()
    {
        var input = 2 * TimeSpan.TicksPerMinute + 30 * TimeSpan.TicksPerSecond + 555 * TimeSpan.TicksPerMillisecond;
        Assert.Equal("2m 31s", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [Fact]
    public void DisplaysHoursInSplitUnits()
    {
        var input = 2 * TimeSpan.TicksPerHour + 30 * TimeSpan.TicksPerMinute + 30 * TimeSpan.TicksPerSecond;
        Assert.Equal("2h 31m", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }

    [Fact]
    public void DisplaysTimesLessThanMicroseconds()
    {
        var input = (double)TimeSpan.TicksPerMicrosecond / 10;
        Assert.Equal(0.1m.ToString("0.##μs", CultureInfo.CurrentCulture), DurationFormatter.FormatDuration(TimeSpan.FromTicks((long)input)));
    }

    [Fact]
    public void DisplaysTimesOf0()
    {
        var input = 0;
        Assert.Equal("0μs", DurationFormatter.FormatDuration(TimeSpan.FromTicks(input)));
    }
}
