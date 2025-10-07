// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Markdown;
using Markdig;
using Xunit;

namespace Aspire.Dashboard.Tests.Markdown;

public class MarkdownHelperTests
{
    [Fact]
    public void ToHtml_InCompleteDocumentAndSubListDash_NoHeader()
    {
        // Arrange
        var markdown =
            """
            3. **Waiting Resources:**
               -
            """;

        // Act
        var html = ToHtml(markdown);

        // Assert
        Assert.Equal(
            """
            <ol start="3">
            <li><strong>Waiting Resources:</strong></li>
            </ol>
            """, html.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ToHtml_InCompleteDocumentAndInProgressBold_NoHeader()
    {
        // Arrange
        var markdown =
            """
            3. **Waiting
            """;

        // Act
        var html = ToHtml(markdown);

        // Assert
        Assert.Equal(
            """
            <ol start="3">
            <li><strong>Waiting</strong></li>
            </ol>
            """, html.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ToHtml_InCompleteDocumentAndStartedBold_NoHeader()
    {
        // Arrange
        var markdown =
            """
            3. **
            """;

        // Act
        var html = ToHtml(markdown);

        // Assert
        Assert.Equal(
            """
            <ol start="3">
            <li></li>
            </ol>
            """, html.Trim(), ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("before [")]
    [InlineData("before [link")]
    [InlineData("before [link]")]
    [InlineData("before [link](http://example.com")]
    public void ToHtml_InCompleteDocumentAndLinkInProgress_NoLink(string markdown)
    {
        // Arrange & Act
        var html = ToHtml(markdown);

        // Assert
        Assert.Equal(
            """
            <p>before </p>
            """, html.Trim(), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ToHtml_InCompleteDocumentAndLinkFinished_Link()
    {
        // Arrange
        var markdown = "before [link](http://example.com)";

        // Act
        var html = ToHtml(markdown);

        // Assert
        Assert.Equal(
            """
            <p>before <a href="http://example.com" target="_blank" rel="noopener noreferrer nofollow">link</a></p>
            """, html.Trim(), ignoreLineEndingDifferences: true);
    }

    private static string ToHtml(string markdown)
    {
        return MarkdownHelpers.ToHtml(markdown, new MarkdownOptions
        {
            Pipeline = new MarkdownPipelineBuilder().Build(),
            IncompleteDocument = true,
            AllowedUrlSchemes = null,
            SuppressSurroundingParagraph = false
        });
    }
}
