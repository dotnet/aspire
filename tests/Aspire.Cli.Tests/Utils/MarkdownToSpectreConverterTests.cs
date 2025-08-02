// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class MarkdownToSpectreConverterTests
{
    [Fact]
    public void ConvertToSpectre_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var markdown = "";

        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ConvertToSpectre_WithNull_ReturnsNull()
    {
        // Arrange
        string? markdown = null;

        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown!);

        // Assert
        Assert.Equal(markdown, result);
    }

    [Fact]
    public void ConvertToSpectre_WithPlainText_ReturnsUnchanged()
    {
        // Arrange
        var markdown = "This is plain text without any markdown.";

        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal("This is plain text without any markdown.", result);
    }

    [Theory]
    [InlineData("# Header 1", "[bold green]Header 1[/]")]
    [InlineData("## Header 2", "[bold blue]Header 2[/]")]
    [InlineData("### Header 3", "[bold yellow]Header 3[/]")]
    public void ConvertToSpectre_WithHeaders_ConvertsCorrectly(string markdown, string expected)
    {
        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("**bold text**", "[bold]bold text[/]")]
    [InlineData("__also bold__", "[bold]also bold[/]")]
    [InlineData("This is **bold** and this is not.", "This is [bold]bold[/] and this is not.")]
    public void ConvertToSpectre_WithBoldText_ConvertsCorrectly(string markdown, string expected)
    {
        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("*italic text*", "[italic]italic text[/]")]
    [InlineData("_also italic_", "[italic]also italic[/]")]
    [InlineData("This is *italic* and this is not.", "This is [italic]italic[/] and this is not.")]
    public void ConvertToSpectre_WithItalicText_ConvertsCorrectly(string markdown, string expected)
    {
        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("`inline code`", "[grey][bold]inline code[/][/]")]
    [InlineData("This is `code` in text.", "This is [grey][bold]code[/][/] in text.")]
    public void ConvertToSpectre_WithInlineCode_ConvertsCorrectly(string markdown, string expected)
    {
        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("[link text](https://example.com)", "[blue underline]link text[/]")]
    [InlineData("Visit [GitHub](https://github.com) for more info.", "Visit [blue underline]GitHub[/] for more info.")]
    public void ConvertToSpectre_WithLinks_ConvertsCorrectly(string markdown, string expected)
    {
        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToSpectre_WithComplexMarkdown_ConvertsAllElements()
    {
        // Arrange
        var markdown = @"# Main Header
This is **bold** and *italic* text with `inline code`.
## Sub Header
Visit [GitHub](https://github.com) for more information.
### Small Header
Some more text.";

        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        var expected = @"[bold green]Main Header[/]
This is [bold]bold[/] and [italic]italic[/] text with [grey][bold]inline code[/][/].
[bold blue]Sub Header[/]
Visit [blue underline]GitHub[/] for more information.
[bold yellow]Small Header[/]
Some more text.";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToSpectre_WithNestedFormatting_HandlesCorrectly()
    {
        // Arrange - test that ** inside * doesn't break things
        var markdown = "This should not break: **bold** and *italic*";

        // Act
        var result = MarkdownToSpectreConverter.ConvertToSpectre(markdown);

        // Assert
        Assert.Equal("This should not break: [bold]bold[/] and [italic]italic[/]", result);
    }
}