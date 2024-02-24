// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Utils;
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
        Assert.Equal(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("9", 9d)]
    [InlineData("9,9", 9.9d)]
    [InlineData("0,9", 0.9d)]
    [InlineData("12.345.678,9", 12345678.9d)]
    [InlineData("1,234568", 1.23456789d)]
    public void FormatNumberWithOptionalDecimalPlaces_GermanCulture(string expected, double value)
    {
        Assert.Equal(expected, FormatHelpers.FormatNumberWithOptionalDecimalPlaces(value, CultureInfo.GetCultureInfo("de-DE")));
    }

    [Theory]
    [InlineData("06/15/2009 13:45:30.000", true, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("06/15/2009 13:45:30.123", true, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("06/15/2009 13:45:30", false, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("06/15/2009 13:45:30", false, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_InvariantCulture(string expected, bool includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(date, includeMilliseconds, cultureInfo: CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("15.06.2009 13:45:30,000", true, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15.06.2009 13:45:30,123", true, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15.06.2009 13:45:30", false, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15.06.2009 13:45:30", false, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_GermanCulture(string expected, bool includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("de-DE")));
    }

    [Theory]
    [InlineData("15/06/2009 1:45:30.000 pm", true, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15/06/2009 1:45:30.123 pm", true, "2009-06-15T13:45:30.1234567Z")]
    [InlineData("15/06/2009 1:45:30 pm", false, "2009-06-15T13:45:30.0000000Z")]
    [InlineData("15/06/2009 1:45:30 pm", false, "2009-06-15T13:45:30.1234567Z")]
    public void FormatDateTime_WithMilliseconds_NewZealandCulture(string expected, bool includeMilliseconds, string value)
    {
        var date = GetLocalDateTime(value);
        Assert.Equal(expected, FormatHelpers.FormatDateTime(date, includeMilliseconds, cultureInfo: CultureInfo.GetCultureInfo("en-NZ")));
    }

    private static DateTime GetLocalDateTime(string value)
    {
        Assert.True(DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date));
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        return date;
    }
}
