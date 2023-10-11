// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Xunit;

namespace Aspire.Dashboard.Tests.ConsoleLogsTests;

public class AnsiParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("This is some text without any codes")]
    [InlineData("This is some text \x1C with an invalid code")]
    public void ConvertToHtml_ReturnsInputUnchangedIfNoCodesPresent(string input)
    {
        var expectedOutput = input;
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
    }

    [Theory]
    [InlineData("\x1B[2m", "")]
    [InlineData("\x1B[23m", "")]
    [InlineData("\x1B[2m\x1B[23m", "")]
    [InlineData("Real Text Before\x1B[2m\x1B[23m", "Real Text Before")]
    [InlineData("\x1B[2m\x1B[23mReal Text After", "Real Text After")]
    [InlineData("\x1B[2mReal Text Between\x1B[23m", "Real Text Between")]
    [InlineData("Real Text Before\x1B[2mReal Text Between\x1B[23mReal Text After", "Real Text BeforeReal Text BetweenReal Text After")]
    public void ConvertToHtml_IgnoresUnsupportedButValidCodes(string input, string expectedOutput)
    {
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_ColorOpenedButNotClosed_AutoClosedWithResidualState()
    {
        var input = "\x1B[32mThis is some green text";
        var expectedOutput = "<span class=\"ansi-fg-green\">This is some green text</span>";
        var expectedResidualState = new AnsiParser.ParserState() { ForegroundColor = ConsoleColor.Green };
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(expectedResidualState, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_ColorOpenedAndClosed()
    {
        var input = "\x1B[32mThis is some green text\x1B[39m";
        var expectedOutput = "<span class=\"ansi-fg-green\">This is some green text</span>";
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_WithForegroundAndBackgroundSet()
    {
        var input = "\x1B[32m\x1B[42mThis is some green text with a green background\x1B[39m\x1B[49m";
        var expectedOutput = "<span class=\"ansi-fg-green ansi-bg-green\">This is some green text with a green background</span>";
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_PartiallyFormatted()
    {
        var input = "\x1B[32mThis is some green text\x1B[39m and this is some plain text";
        var expectedOutput = "<span class=\"ansi-fg-green\">This is some green text</span> and this is some plain text";
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_MultipleDistinctFormats()
    {
        var input = "\x1B[32mGreen text\x1B[39m Plain text\x1B[42m Green background\x1B[49m";
        var expectedOutput = "<span class=\"ansi-fg-green\">Green text</span> Plain text<span class=\"ansi-bg-green\"> Green background</span>";
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_OverlappingFormats()
    {
        var input = "\x1B[32mGreen text\x1B[42m Green text Green background\x1B[39m Green background\x1B[49m";
        var expectedOutput = "<span class=\"ansi-fg-green\">Green text</span><span class=\"ansi-fg-green ansi-bg-green\"> Green text Green background</span><span class=\"ansi-bg-green\"> Green background</span>";
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_FormattingAcrossLines()
    {
        var input1 = "\x1B[32mThis is some green text";
        var input2 = "This is some more green text\u001b[39m";
        var expectedOutput1 = "<span class=\"ansi-fg-green\">This is some green text</span>";
        var expectedOutput2 = "<span class=\"ansi-fg-green\">This is some more green text</span>";
        var result1 = AnsiParser.ConvertToHtml(input1);
        var result2 = AnsiParser.ConvertToHtml(input2, result1.ResidualState);

        Assert.Equal(expectedOutput1, result1.ConvertedText);
        Assert.Equal(new AnsiParser.ParserState() { ForegroundColor = ConsoleColor.Green }, result1.ResidualState);
        Assert.Equal(expectedOutput2, result2.ConvertedText);
        Assert.Equal(default, result2.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_OverlappingFormattingAcrossLines()
    {
        var input1 = "\x1B[32mGreen text\x1B[42m Green text";
        var input2 = "Green background\x1B[39m Green background\x1B[49m";
        var expectedOutput1 = "<span class=\"ansi-fg-green\">Green text</span><span class=\"ansi-fg-green ansi-bg-green\"> Green text</span>";
        var expectedOutput2 = "<span class=\"ansi-fg-green ansi-bg-green\">Green background</span><span class=\"ansi-bg-green\"> Green background</span>";
        var result1 = AnsiParser.ConvertToHtml(input1);
        var result2 = AnsiParser.ConvertToHtml(input2, result1.ResidualState);

        Assert.Equal(expectedOutput1, result1.ConvertedText);
        Assert.Equal(new AnsiParser.ParserState() { ForegroundColor = ConsoleColor.Green, BackgroundColor = ConsoleColor.Green }, result1.ResidualState);
        Assert.Equal(expectedOutput2, result2.ConvertedText);
        Assert.Equal(default, result2.ResidualState);
    }

    [Theory]
    [InlineData("\x1B[30mBlack\x1B[39m", "<span class=\"ansi-fg-black\">Black</span>")]
    [InlineData("\x1B[31mRed\x1B[39m", "<span class=\"ansi-fg-red\">Red</span>")]
    [InlineData("\x1B[32mGreen\x1B[39m", "<span class=\"ansi-fg-green\">Green</span>")]
    [InlineData("\x1B[33mYellow\x1B[39m", "<span class=\"ansi-fg-yellow\">Yellow</span>")]
    [InlineData("\x1B[34mBlue\x1B[39m", "<span class=\"ansi-fg-blue\">Blue</span>")]
    [InlineData("\x1B[35mMagenta\x1B[39m", "<span class=\"ansi-fg-magenta\">Magenta</span>")]
    [InlineData("\x1B[36mCyan\x1B[39m", "<span class=\"ansi-fg-cyan\">Cyan</span>")]
    [InlineData("\x1B[37mWhite\x1B[39m", "<span class=\"ansi-fg-white\">White</span>")]
    [InlineData("\x1B[1m\x1B[30mBright Black\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightblack\">Bright Black</span>")]
    [InlineData("\x1B[1m\x1B[31mBright Red\x1b[22m\x1B[39m", "<span class=\"ansi-fg-brightred\">Bright Red</span>")]
    [InlineData("\x1B[1m\x1B[32mBright Green\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightgreen\">Bright Green</span>")]
    [InlineData("\x1B[1m\x1B[33mBright Yellow\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightyellow\">Bright Yellow</span>")]
    [InlineData("\x1B[1m\x1B[34mBright Blue\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightblue\">Bright Blue</span>")]
    [InlineData("\x1B[1m\x1B[35mBright Magenta\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightmagenta\">Bright Magenta</span>")]
    [InlineData("\x1B[1m\x1B[36mBright Cyan\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightcyan\">Bright Cyan</span>")]
    [InlineData("\x1B[1m\x1B[37mBright White\x1B[22m\x1B[39m", "<span class=\"ansi-fg-brightwhite\">Bright White</span>")]
    public void ConvertToHtml_ForegroundColorSupport(string input, string expectedOutput)
    {
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Theory]
    [InlineData("\x1B[40mBlack\x1B[49m", "<span class=\"ansi-bg-black\">Black</span>")]
    [InlineData("\x1B[41mRed\x1B[49m", "<span class=\"ansi-bg-red\">Red</span>")]
    [InlineData("\x1B[42mGreen\x1B[49m", "<span class=\"ansi-bg-green\">Green</span>")]
    [InlineData("\x1B[43mYellow\x1B[49m", "<span class=\"ansi-bg-yellow\">Yellow</span>")]
    [InlineData("\x1B[44mBlue\x1B[49m", "<span class=\"ansi-bg-blue\">Blue</span>")]
    [InlineData("\x1B[45mMagenta\x1B[49m", "<span class=\"ansi-bg-magenta\">Magenta</span>")]
    [InlineData("\x1B[46mCyan\x1B[49m", "<span class=\"ansi-bg-cyan\">Cyan</span>")]
    [InlineData("\x1B[47mWhite\x1B[49m", "<span class=\"ansi-bg-white\">White</span>")]
    public void ConvertToHtml_BackgroundColorSupport(string input, string expectedOutput)
    {
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(default, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_HandlesResetIntensity()
    {
        var input = "\x1B[1m\x1B[32m\x1B[42mBright Green Text on Green Background\x1B[22mNo Longer Bright";
        var expectedOutput = "<span class=\"ansi-fg-brightgreen ansi-bg-green\">Bright Green Text on Green Background</span><span class=\"ansi-fg-green ansi-bg-green\">No Longer Bright</span>";
        var expectedResidualState = new AnsiParser.ParserState() {  ForegroundColor = ConsoleColor.Green, BackgroundColor = ConsoleColor.Green };
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(expectedResidualState, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_HandlesDefaultForeground()
    {
        var input = "\x1B[1m\x1B[32m\x1B[42mBright Green Text on Green Background\x1B[39mNo Longer Green Text";
        var expectedOutput = "<span class=\"ansi-fg-brightgreen ansi-bg-green\">Bright Green Text on Green Background</span><span class=\"ansi-bg-green\">No Longer Green Text</span>";
        var expectedResidualState = new AnsiParser.ParserState() { Bright = true, BackgroundColor = ConsoleColor.Green };
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(expectedResidualState, result.ResidualState);
    }

    [Fact]
    public void ConvertToHtml_HandlesDefaultBackground()
    {
        var input = "\x1B[1m\x1B[32m\x1B[42mBright Green Text on Green Background\x1B[49mNo Longer Green Background";
        var expectedOutput = "<span class=\"ansi-fg-brightgreen ansi-bg-green\">Bright Green Text on Green Background</span><span class=\"ansi-fg-brightgreen\">No Longer Green Background</span>";
        var expectedResidualState = new AnsiParser.ParserState() { Bright = true, ForegroundColor = ConsoleColor.Green };
        var result = AnsiParser.ConvertToHtml(input);

        Assert.Equal(expectedOutput, result.ConvertedText);
        Assert.Equal(expectedResidualState, result.ResidualState);
    }
}
