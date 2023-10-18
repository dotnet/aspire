// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class TimestampParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("This is some text without any timestamp")]
    public void TryColorizeTimestamp_DoesNotStartWithTimestamp_ReturnsFalse(string input)
    {
        var result = TimestampParser.TryColorizeTimestamp(input, convertTimestampsFromUtc: false, out var _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("2023-10-10T15:05:30.123456789Z", true, "<span class=\"timestamp\">2023-10-10T15:05:30.123456789Z</span>", "2023-10-10T15:05:30.123456789Z")]
    [InlineData("2023-10-10T15:05:30.123456789Z ", true, "<span class=\"timestamp\">2023-10-10T15:05:30.123456789Z</span> ", "2023-10-10T15:05:30.123456789Z")]
    [InlineData("2023-10-10T15:05:30.123456789Z with some text after it", true, "<span class=\"timestamp\">2023-10-10T15:05:30.123456789Z</span> with some text after it", "2023-10-10T15:05:30.123456789Z")]
    [InlineData("With some text before it 2023-10-10T15:05:30.123456789Z", false, null, null)]
    public void TryColorizeTimestamp_ReturnsCorrectResult(string input, bool expectedResult, string? expectedOutput, string? expectedTimestamp)
    {
        var result = TimestampParser.TryColorizeTimestamp(input, convertTimestampsFromUtc: false, out var parseResult);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedOutput, parseResult.ModifiedText);
        Assert.Equal(expectedTimestamp, parseResult.Timestamp);
    }

    [Theory]
    [InlineData("2023-10-10T15:05:30.123456789Z")]
    [InlineData("2023-10-10T15:05:30.12345678Z")]
    [InlineData("2023-10-10T15:05:30.1234567Z")]
    [InlineData("2023-10-10T15:05:30.123456Z")]
    [InlineData("2023-10-10T15:05:30.12345Z")]
    [InlineData("2023-10-10T15:05:30.1234Z")]
    [InlineData("2023-10-10T15:05:30.123Z")]
    [InlineData("2023-10-10T15:05:30.12Z")]
    [InlineData("2023-10-10T15:05:30.1Z")]
    [InlineData("2023-10-10T15:05:30Z")]
    [InlineData("2023-10-10T15:05:30.123456789+12:59")]
    [InlineData("2023-10-10T15:05:30.123456789-12:59")]
    [InlineData("2023-10-10T15:05:30.123456789")]
    public void TryColorizeTimestamp_SupportedTimestampFormats(string input)
    {
        var result = TimestampParser.TryColorizeTimestamp(input, convertTimestampsFromUtc: false, out var _);

        Assert.True(result);
    }
}
