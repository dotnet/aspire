// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class FormatHelpersTests
{
    [Theory]
    [InlineData("9", 9d)]
    [InlineData("9.9", 9.9d)]
    [InlineData("0.9", 0.9d)]
    [InlineData("12,345,678.9", 12345678.9d)]
    [InlineData("1.234568", 1.23456789d)]
    public void FormatNumberWithOptionalDecimalPlaces_InvariantCulture(string expected, double value)
    {
        Assert.Equal(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, maxDecimalPlaces: 6, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("9", 9d)]
    [InlineData("9,9", 9.9d)]
    [InlineData("0,9", 0.9d)]
    [InlineData("12.345.678,9", 12345678.9d)]
    [InlineData("1,234568", 1.23456789d)]
    public void FormatNumberWithOptionalDecimalPlaces_GermanCulture(string expected, double value)
    {
        Assert.Equal(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, maxDecimalPlaces: 6, CultureInfo.GetCultureInfo("de-DE")));
    }

    [Theory]
    [InlineData("06/15/2009 13:45:30.000", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("06/15/2009 13:45:30.123", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("06/15/2009 13:45:30.1234567", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("06/15/2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("06/15/2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_InvariantCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("15.06.2009 13:45:30,000", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15.06.2009 13:45:30,123", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15.06.2009 13:45:30,1234567", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15.06.2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15.06.2009 13:45:30", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_GermanCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("de-DE")));
    }

    [Theory]
    [InlineData("15.6.2009 13.45.30,000", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15.6.2009 13.45.30,123", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15.6.2009 13.45.30,1234567", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15.6.2009 13.45.30", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15.6.2009 13.45.30", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_FinnishCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("fi-FI")));
    }

    [Theory]
    [InlineData("15/06/2009 1:45:30.000 pm", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15/06/2009 1:45:30.123 pm", MillisecondsDisplay.Truncated, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15/06/2009 1:45:30.1234567 pm", MillisecondsDisplay.Full, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15/06/2009 1:45:30 pm", MillisecondsDisplay.None, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15/06/2009 1:45:30 pm", MillisecondsDisplay.None, "2009-06-15T13:45:30.1234567Z")]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/9151", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOS))]
    public void FormatDateTime_WithMilliseconds_NewZealandCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("en-NZ")), ignoreWhiteSpaceDifferences: true);
    }

    private static DateTime GetLocalDateTime(string value)
    {
        Assert.True(DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date));
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        return date;
    }

    private static BrowserTimeProvider CreateTimeProvider()
    {
        return new BrowserTimeProvider(NullLoggerFactory.Instance);
    }
}
