// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class LogLevelParserTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("This is some text without any log level", false)]
    [InlineData("This is some text that does not start with a log level info", false)]
    [InlineData("crit:", true)]
    [InlineData("dbug:", true)]
    [InlineData("fail:", true)]
    [InlineData("info:", true)]
    [InlineData("trce:", true)]
    [InlineData("warn:", true)]
    [InlineData("\x1B[32minfo\x1B[39m:", true)]
    public void StartsWithLogLevel_ReturnsCorrectResult(string input, bool expectedResult)
    {
        var result = LogLevelParser.StartsWithLogLevelHeader(input);

        Assert.Equal(expectedResult, result);
    }
}
