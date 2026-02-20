// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
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
    public void FormatDateTime_WithMilliseconds_NewZealandCulture(string expected, MillisecondsDisplay includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        // macOS formats with uppercase AM/PM, so ignore case
        Assert.Equal(expected, FormatHelpers.FormatDateTime(CreateTimeProvider(), date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("en-NZ")), ignoreWhiteSpaceDifferences: true, ignoreCase: true);
    }

    [Theory]
    [InlineData(null, 5, "")]
    [InlineData("", 5, "")]
    [InlineData("abcdef", 5, "abcd" + FormatHelpers.Ellipsis)]
    [InlineData("abcdef", 10, "abcdef")]
    public void TruncateText(string? initialText, int maxLength, string expected)
    {
        Assert.Equal(expected, FormatHelpers.TruncateText(initialText, maxLength: maxLength));
    }

    private static DateTime GetLocalDateTime(string value)
    {
        Assert.True(DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date));
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        return date;
    }

    [Theory]
    [InlineData("2009-06-15T13:45:30.0000000Z", TimeFormat.TwelveHour, "1:45:30 PM")]
    [InlineData("2009-06-15T13:45:30.0000000Z", TimeFormat.TwentyFourHour, "13:45:30")]
    public void FormatTime_WithTimeFormatPreference(string value, TimeFormat format, string expected)
    {
        var date = GetLocalDateTime(value);
        var provider = CreateTimeProvider();
        provider.SetTimeFormat(format);

        // Use a culture that would normally be opposite
        var culture = format == TimeFormat.TwelveHour ? CultureInfo.GetCultureInfo("de-DE") : CultureInfo.GetCultureInfo("en-US");

        Assert.Equal(expected, FormatHelpers.FormatTime(provider, date, MillisecondsDisplay.None, culture));
    }

    [Theory]
    [InlineData("2009-06-15T13:45:30.0000000Z", TimeFormat.TwelveHour, "6/15/2009 1:45:30 PM")] // en-US date pattern + 12h time
    [InlineData("2009-06-15T13:45:30.0000000Z", TimeFormat.TwentyFourHour, "6/15/2009 13:45:30")] // en-US date pattern + 24h time
    public void FormatDateTime_WithTimeFormatPreference_EnUS(string value, TimeFormat format, string expected)
    {
        var date = GetLocalDateTime(value);
        var provider = CreateTimeProvider();
        provider.SetTimeFormat(format);

        Assert.Equal(expected, FormatHelpers.FormatDateTime(provider, date, MillisecondsDisplay.None, CultureInfo.GetCultureInfo("en-US")));
    }

    [Theory]
    [InlineData("fi-FI", TimeFormat.TwentyFourHour, MillisecondsDisplay.None, "15.6.2009 13.45.30")]
    [InlineData("fi-FI", TimeFormat.TwentyFourHour, MillisecondsDisplay.Truncated, "15.6.2009 13.45.30,123")]
    [InlineData("de-DE", TimeFormat.TwentyFourHour, MillisecondsDisplay.Truncated, "15.06.2009 13:45:30,123")]
    [InlineData("en-US", TimeFormat.TwelveHour, MillisecondsDisplay.Truncated, "6/15/2009 1:45:30.123 PM")]
    public void FormatDateTime_WithTimeFormatPreference_UsesCultureSeparators(string cultureName, TimeFormat format, MillisecondsDisplay includeMilliseconds, string expected)
    {
        var date = GetLocalDateTime("2009-06-15T13:45:30.1234567Z");
        var provider = CreateTimeProvider();
        provider.SetTimeFormat(format);

        Assert.Equal(expected, FormatHelpers.FormatDateTime(provider, date, includeMilliseconds, CultureInfo.GetCultureInfo(cultureName)));
    }

    [Fact]
    public void FormatTime_TwelveHour_UsesAmPm_EvenIfCultureIs24Hour()
    {
        // en-GB is typically 24-hour.
        var date = GetLocalDateTime("2009-06-15T13:45:30.0000000Z");
        var provider = CreateTimeProvider();
        provider.SetTimeFormat(TimeFormat.TwelveHour);

        // en-GB has AM/PM designators "am"/"pm" even if standard pattern is 24h.
        var result = FormatHelpers.FormatTime(provider, date, MillisecondsDisplay.None, CultureInfo.GetCultureInfo("en-GB"));
        Assert.Contains("pm", result.ToLowerInvariant());
        Assert.DoesNotContain("13", result);
    }

    [Fact]
    public void FormatTime_TwentyFourHour_Uses13_EvenIfCultureIs12Hour()
    {
        // en-US is typically 12-hour.
        var date = GetLocalDateTime("2009-06-15T13:45:30.0000000Z");
        var provider = CreateTimeProvider();
        provider.SetTimeFormat(TimeFormat.TwentyFourHour);

        var result = FormatHelpers.FormatTime(provider, date, MillisecondsDisplay.None, CultureInfo.GetCultureInfo("en-US"));
        Assert.Contains("13", result);
        Assert.DoesNotContain("PM", result);
    }

    private static BrowserTimeProvider CreateTimeProvider()
    {
        return new BrowserTimeProvider(NullLoggerFactory.Instance);
    }
}
