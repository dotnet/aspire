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
    [InlineData("This is some text with a URL at the end: https://bing.com/", true, "This is some text with a URL at the end: <a target=\"_blank\" href=\"https://bing.com/\" rel=\"noopener noreferrer nofollow\">https://bing.com/</a>")]
    [InlineData("https://bing.com/ This is some text with a URL at the beginning", true, "<a target=\"_blank\" href=\"https://bing.com/\" rel=\"noopener noreferrer nofollow\">https://bing.com/</a> This is some text with a URL at the beginning")]
    [InlineData("This is some text with a https://bing.com/ in the middle", true, "This is some text with a <a target=\"_blank\" href=\"https://bing.com/\" rel=\"noopener noreferrer nofollow\">https://bing.com/</a> in the middle")]
    public void TryParse_ReturnsCorrectResult(string input, bool expectedResult, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedOutput, modifiedText);
    }

    [Theory]
    [InlineData("http://bing.com", "<a target=\"_blank\" href=\"http://bing.com\" rel=\"noopener noreferrer nofollow\">http://bing.com</a>")]
    [InlineData("https://bing.com", "<a target=\"_blank\" href=\"https://bing.com\" rel=\"noopener noreferrer nofollow\">https://bing.com</a>")]
    [InlineData("http://www.bing.com", "<a target=\"_blank\" href=\"http://www.bing.com\" rel=\"noopener noreferrer nofollow\">http://www.bing.com</a>")]
    [InlineData("http://bing.com/", "<a target=\"_blank\" href=\"http://bing.com/\" rel=\"noopener noreferrer nofollow\">http://bing.com/</a>")]
    [InlineData("http://bing.com/dir", "<a target=\"_blank\" href=\"http://bing.com/dir\" rel=\"noopener noreferrer nofollow\">http://bing.com/dir</a>")]
    [InlineData("http://bing.com/index.aspx", "<a target=\"_blank\" href=\"http://bing.com/index.aspx\" rel=\"noopener noreferrer nofollow\">http://bing.com/index.aspx</a>")]
    [InlineData("http://localhost", "<a target=\"_blank\" href=\"http://localhost\" rel=\"noopener noreferrer nofollow\">http://localhost</a>")]
    [InlineData(@"\x1B[32mhttp\x1B[0m://\x1B[1mlocalhost\x1B[0m:\x1B[33m5173\x1B[0m/", @"<a target=""_blank"" href=""http://localhost:5173/"" rel=""noopener noreferrer nofollow"">\x1B[32mhttp\x1B[0m://\x1B[1mlocalhost\x1B[0m:\x1B[33m5173\x1B[0m/</a>")]
    [InlineData(@"https\x1B[0m://\x1B[44m127.0.0.1\x1B[0m:\x1B[36m8080\x1B[0m/path/to/api", @"<a target=""_blank"" href=""https://127.0.0.1:8080/path/to/api"" rel=""noopener noreferrer nofollow"">https\x1B[0m://\x1B[44m127.0.0.1\x1B[0m:\x1B[36m8080\x1B[0m/path/to/api</a>")]
    [InlineData(@"\x1B[1;35mhttp://example.com\x1B[0m:\x1B[1;33m3000\x1B[0m/assets/js/app.js\x1B[32m?v=123\x1B[0m", @"<a target=""_blank"" href=""http://example.com:3000/assets/js/app.js?v=123"" rel=""noopener noreferrer nofollow"">\x1B[1;35mhttp://example.com\x1B[0m:\x1B[1;33m3000\x1B[0m/assets/js/app.js\x1B[32m?v=123\x1B[0m</a>")]
    [InlineData(@"\x1B[36mhttp\x1B[0m://\x1B[31mmy-domain.org\x1B[0m/\x1B[33m#section\x1B[0m", @"<a target=""_blank"" href=""http://my-domain.org/#section"" rel=""noopener noreferrer nofollow"">\x1B[36mhttp\x1B[0m://\x1B[31mmy-domain.org\x1B[0m/\x1B[33m#section\x1B[0m</a>")]
    [InlineData(@"\x1B[1;32mhttps://dev.server.local\x1B[0m:\x1B[1;31m5001\x1B[0m/path/with\x1B[1;34m/colors\x1B[0m", @"<a target=""_blank"" href=""https://dev.server.local:5001/path/with/colors"" rel=""noopener noreferrer nofollow"">\x1B[1;32mhttps://dev.server.local\x1B[0m:\x1B[1;31m5001\x1B[0m/path/with\x1B[1;34m/colors\x1B[0m</a>")]
    [InlineData(@"\x1B[33mhttp\x1B[0m://\x1B[1msub.domain\x1B[0m:\x1B[36m9000\x1B[0m/path/with/multiple\x1B[31m/segments\x1B[0m", @"<a target=""_blank"" href=""http://sub.domain:9000/path/with/multiple/segments"" rel=""noopener noreferrer nofollow"">\x1B[33mhttp\x1B[0m://\x1B[1msub.domain\x1B[0m:\x1B[36m9000\x1B[0m/path/with/multiple\x1B[31m/segments\x1B[0m</a>")]
    [InlineData(@"\x1B[1;34mhttps://localhost\x1B[0m:\x1B[1;32m3000\x1B[0m/api/users/\x1B[35m12345\x1B[0m", @"<a target=""_blank"" href=""https://localhost:3000/api/users/12345"" rel=""noopener noreferrer nofollow"">\x1B[1;34mhttps://localhost\x1B[0m:\x1B[1;32m3000\x1B[0m/api/users/\x1B[35m12345\x1B[0m</a>")]
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
    [InlineData("script:alert('hi')")]
    [InlineData("http://script:alert('hi')")]
    public void TryParse_AttemptedScriptInjection(string input)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("http://localhost:8080</url>", "<a target=\"_blank\" href=\"http://localhost:8080\" rel=\"noopener noreferrer nofollow\">http://localhost:8080</a>&lt;/url&gt;")]
    [InlineData("http://localhost:8080\"", "<a target=\"_blank\" href=\"http://localhost:8080\" rel=\"noopener noreferrer nofollow\">http://localhost:8080</a>&quot;")]
    public void TryParse_ExcludeInvalidTrailingChars(string input, string? expectedOutput)
    {
        var result = UrlParser.TryParse(input, WebUtility.HtmlEncode, out var modifiedText);
        Assert.True(result);

        Assert.Equal(expectedOutput, modifiedText);
    }

    [Fact]
    public void TryParse_QueryString()
    {
        var result = UrlParser.TryParse("https://www.example.com?query=string&param=value", WebUtility.HtmlEncode, out var modifiedText);
        Assert.True(result);

        Assert.Equal("<a target=\"_blank\" href=\"https://www.example.com?query=string&param=value\" rel=\"noopener noreferrer nofollow\">https://www.example.com?query=string&amp;param=value</a>", modifiedText);
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

    [Theory]
    [InlineData("http://www.localhost:8080!", "http://www.localhost:8080!")]
    [InlineData("http://www.localhost:8080/path!", "http://www.localhost:8080/path!")]
    [InlineData("http://www.localhost:8080/path;", "http://www.localhost:8080/path")]
    [InlineData("http://www.localhost:8080;", "http://www.localhost:8080")]
    [InlineData("http://www.local;host:8080;", "http://www.local")]
    public void GenerateUrlRegEx_MatchUrlBeforeContent(string content, string expected)
    {
        var regex = UrlParser.GenerateUrlRegEx();
        var match = regex.Match(content);
        Assert.Equal(expected, match.Value.ToLowerInvariant());
    }
}
