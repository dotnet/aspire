// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Mcp.Docs;

namespace Aspire.Cli.Tests.Mcp.Docs;

public class LlmsTxtParserTests
{
    [Fact]
    public async Task ParseAsync_WithEmptyString_ReturnsEmptyList()
    {
        var result = await LlmsTxtParser.ParseAsync("");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceOnly_ReturnsEmptyList()
    {
        var result = await LlmsTxtParser.ParseAsync("   \n\t\n   ");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithNoH1Headers_ReturnsEmptyList()
    {
        var content = """
            This is just regular text.
            No headers here.
            ## This is H2 but no H1
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithSingleDocument_ParsesCorrectly()
    {
        var content = """
            # My Document Title
            > This is the summary.

            Some body content here.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        var doc = result[0];
        Assert.Equal("My Document Title", doc.Title);
        Assert.Equal("my-document-title", doc.Slug);
        Assert.Equal("This is the summary.", doc.Summary);
        Assert.Contains("Some body content here.", doc.Content);
    }

    [Fact]
    public async Task ParseAsync_WithMultipleDocuments_ParsesAll()
    {
        var content = """
            # First Document
            > First summary.

            First content.

            # Second Document
            > Second summary.

            Second content.

            # Third Document
            Third content without summary.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Equal(3, result.Count);
        Assert.Equal("First Document", result[0].Title);
        Assert.Equal("Second Document", result[1].Title);
        Assert.Equal("Third Document", result[2].Title);
    }

    [Fact]
    public async Task ParseAsync_WithSections_ParsesSectionsCorrectly()
    {
        var content = """
            # Main Document
            > Document summary.

            ## Section One
            Section one content.

            ## Section Two
            Section two content.

            ### Subsection
            Subsection content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        var doc = result[0];
        Assert.Equal(3, doc.Sections.Count);

        Assert.Equal("Section One", doc.Sections[0].Heading);
        Assert.Equal(2, doc.Sections[0].Level);

        Assert.Equal("Section Two", doc.Sections[1].Heading);
        Assert.Equal(2, doc.Sections[1].Level);

        Assert.Equal("Subsection", doc.Sections[2].Heading);
        Assert.Equal(3, doc.Sections[2].Level);
    }

    [Fact]
    public async Task ParseAsync_SectionContent_IncludesNestedHeadings()
    {
        var content = """
            # Document

            ## Parent Section
            Parent content.

            ### Child Section
            Child content.

            ## Another Section
            Another content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        var doc = result[0];
        var parentSection = doc.Sections.First(s => s.Heading == "Parent Section");

        // Parent section should include content up to the next H2
        Assert.Contains("Parent content.", parentSection.Content);
        Assert.Contains("### Child Section", parentSection.Content);
        Assert.Contains("Child content.", parentSection.Content);
        Assert.DoesNotContain("Another content.", parentSection.Content);
    }

    [Fact]
    public async Task ParseAsync_WithNoSummary_SummaryIsNull()
    {
        var content = """
            # Document Without Summary

            Just regular content, no blockquote.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Null(result[0].Summary);
    }

    [Fact]
    public async Task ParseAsync_SlugGeneration_HandlesSpecialCharacters()
    {
        var content = """
            # Hello, World! How's It Going?
            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("hello-world-hows-it-going", result[0].Slug);
    }

    [Fact]
    public async Task ParseAsync_SlugGeneration_HandlesMultipleSpaces()
    {
        var content = """
            # Title   With   Multiple   Spaces
            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        // Multiple spaces should become single hyphens
        Assert.Equal("title-with-multiple-spaces", result[0].Slug);
    }

    [Fact]
    public async Task ParseAsync_SlugGeneration_TrimsHyphens()
    {
        var content = """
            # - Title With Leading Dash -
            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        // Leading and trailing hyphens should be trimmed
        Assert.DoesNotMatch("^-", result[0].Slug);
        Assert.DoesNotMatch("-$", result[0].Slug);
    }

    [Fact]
    public async Task ParseAsync_WithCodeBlocks_PreservesContent()
    {
        var content = """
            # Code Example

            ## Usage

            ```csharp
            var builder = DistributedApplication.CreateBuilder(args);
            var redis = builder.AddRedis("cache");
            ```

            More text.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Contains("```csharp", result[0].Content);
        Assert.Contains("var redis = builder.AddRedis(\"cache\");", result[0].Content);
    }

    [Fact]
    public async Task ParseAsync_H1WithoutSpace_NotRecognizedAsDocument()
    {
        // "#NoSpace" should not be recognized as H1
        var content = """
            #NoSpaceAfterHash
            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_H2AtStart_NotRecognizedAsDocument()
    {
        var content = """
            ## This is H2
            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_WithLeadingWhitespace_StillParsesH1()
    {
        var content = """
               # Document With Leading Spaces
            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("Document With Leading Spaces", result[0].Title);
    }

    [Fact]
    public async Task ParseAsync_PreservesNewlinesInContent()
    {
        var content = """
            # Document

            Line 1.

            Line 2.

            Line 3.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Contains("\n", result[0].Content);
    }

    [Fact]
    public async Task ParseAsync_DocumentBoundariesAreCorrect()
    {
        var content = """
            # First
            First content with ## inside text.

            # Second
            Second content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Equal(2, result.Count);
        Assert.Contains("## inside text", result[0].Content);
        Assert.DoesNotContain("Second content", result[0].Content);
        Assert.DoesNotContain("First content", result[1].Content);
    }

    [Fact]
    public async Task ParseAsync_RealWorldExample_ParsesCorrectly()
    {
        var content = """
            # Aspire overview
            > Aspire is an opinionated stack for building observable, production-ready, distributed apps.

            Aspire is delivered through a collection of NuGet packages that handle specific cloud-native concerns.

            ## Why use Aspire?

            Aspire provides:
            - **Orchestration**: Built-in tooling for running and connecting multi-project applications
            - **Integrations**: NuGet packages for popular services like Redis, PostgreSQL, and Azure
            - **Tooling**: Project templates and CLI tools

            ### Getting started

            To get started with Aspire, install the workload:

            ```bash
            dotnet workload install aspire
            ```

            # Service defaults
            > Configure common defaults for ASP.NET Core apps.

            Service defaults provide a consistent configuration for ASP.NET Core applications.

            ## Configuration

            Add service defaults to your project:

            ```csharp
            builder.AddServiceDefaults();
            ```
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Equal(2, result.Count);

        // First document
        var overview = result[0];
        Assert.Equal("Aspire overview", overview.Title);
        Assert.Equal("net-aspire-overview", overview.Slug);
        Assert.Equal("Aspire is an opinionated stack for building observable, production-ready, distributed apps.", overview.Summary);
        Assert.Equal(2, overview.Sections.Count);
        Assert.Equal("Why use Aspire?", overview.Sections[0].Heading);
        Assert.Equal("Getting started", overview.Sections[1].Heading);

        // Second document
        var serviceDefaults = result[1];
        Assert.Equal("Service defaults", serviceDefaults.Title);
        Assert.Equal("service-defaults", serviceDefaults.Slug);
        Assert.Equal("Configure common defaults for ASP.NET Core apps.", serviceDefaults.Summary);
        Assert.Single(serviceDefaults.Sections);
        Assert.Equal("Configuration", serviceDefaults.Sections[0].Heading);
    }

    [Fact]
    public async Task ParseAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var content = """
            # Document 1
            Content 1.

            # Document 2
            Content 2.
            """;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // TaskCanceledException derives from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await LlmsTxtParser.ParseAsync(content, cts.Token));
    }

    [Fact]
    public async Task ParseAsync_EmptyTitle_SkipsDocument()
    {
        var content = """
            #
            Content without title.

            # Valid Title
            Valid content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("Valid Title", result[0].Title);
    }

    [Fact]
    public async Task ParseAsync_MultipleSummaryBlockquotes_UsesFirst()
    {
        var content = """
            # Document
            > First summary.
            > Second line that should be ignored.

            Content.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal("First summary.", result[0].Summary);
    }

    [Fact]
    public async Task ParseAsync_BlockquoteAfterSection_NotUsedAsSummary()
    {
        var content = """
            # Document

            ## Section
            > This blockquote is in a section, not a summary.
            """;

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Null(result[0].Summary);
    }

    [Theory]
    [InlineData("Simple", "simple")]
    [InlineData("Two Words", "two-words")]
    [InlineData("UPPERCASE", "uppercase")]
    [InlineData("MixedCase", "mixedcase")]
    [InlineData("with-hyphens", "with-hyphens")]
    [InlineData("123 Numbers", "123-numbers")]
    [InlineData("Special!@#$%Characters", "specialcharacters")]
    public async Task ParseAsync_SlugGeneration_VariousCases(string title, string expectedSlug)
    {
        var content = $"# {title}\nContent.";

        var result = await LlmsTxtParser.ParseAsync(content);

        Assert.Single(result);
        Assert.Equal(expectedSlug, result[0].Slug);
    }
}
