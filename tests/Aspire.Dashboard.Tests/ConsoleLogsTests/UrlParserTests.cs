// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Aspire.Dashboard.ConsoleLogs;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class UrlParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("This is some text without any urls")]
    public void TryParse_NoUrl_ReturnsFalse(string? input)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("This is some text with a URL at the end: https://bing.com/", true, "This is some text with a URL at the end: <a target=\"_blank\" href=\"https://bing.com/\">https://bing.com/</a>")]
    [InlineData("https://bing.com/ This is some text with a URL at the beginning", true, "<a target=\"_blank\" href=\"https://bing.com/\">https://bing.com/</a> This is some text with a URL at the beginning")]
    [InlineData("This is some text with a https://bing.com/ in the middle", true, "This is some text with a <a target=\"_blank\" href=\"https://bing.com/\">https://bing.com/</a> in the middle")]
    public void TryParse_ReturnsCorrectResult(string input, bool expectedResult, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedOutput, modifiedText);
    }

    [Theory]
    [InlineData("http://bing.com", "<a target=\"_blank\" href=\"http://bing.com\">http://bing.com</a>")]
    [InlineData("https://bing.com", "<a target=\"_blank\" href=\"https://bing.com\">https://bing.com</a>")]
    [InlineData("http://www.bing.com", "<a target=\"_blank\" href=\"http://www.bing.com\">http://www.bing.com</a>")]
    [InlineData("http://bing.com/", "<a target=\"_blank\" href=\"http://bing.com/\">http://bing.com/</a>")]
    [InlineData("http://bing.com/dir", "<a target=\"_blank\" href=\"http://bing.com/dir\">http://bing.com/dir</a>")]
    [InlineData("http://bing.com/index.aspx", "<a target=\"_blank\" href=\"http://bing.com/index.aspx\">http://bing.com/index.aspx</a>")]
    [InlineData("http://bing", "<a target=\"_blank\" href=\"http://bing\">http://bing</a>")]
    public void TryParse_SupportedUrlFormats(string input, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);

        Assert.True(result);
        Assert.Equal(expectedOutput, modifiedText);
    }

    [Theory]
    [InlineData("file:///c:/windows/system32/calc.exe")]
    [InlineData("ftp://ftp.localhost.com/")]
    [InlineData("ftp://user:pass@ftp.localhost.com/")]
    public void TryParse_UnsupportedUrlFormats(string input)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("http://localhost:8080</url>", "<a target=\"_blank\" href=\"http://localhost:8080\">http://localhost:8080</a>&lt;/url&gt;")]
    [InlineData("http://localhost:8080\"", "<a target=\"_blank\" href=\"http://localhost:8080\">http://localhost:8080</a>&quot;")]
    public void TryParse_ExcludeInvalidTrailingChars(string input, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);
        Assert.True(result);

        Assert.Equal(expectedOutput, modifiedText);
    }

    [Theory]
    [InlineData("http://www.localhost:8080")]
    [InlineData("HTTP://WWW.LOCALHOST:8080")]
    [InlineData("mhttp://www.localhost:8080")]
    [InlineData("httphttp://www.localhost:8080")]
    [InlineData(" http://www.localhost:8080")]
    public void GenerateUrlRegEx_MatchUrlAfterContent(string content)
    {
        var regex = UrlParser.GenerateUrlRegEx();
        var match = regex.Match(content);
        Assert.Equal("http://www.localhost:8080", match.Value.ToLowerInvariant());
    }
}
