// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Mcp.Docs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Mcp.Docs;

public class DocsIndexServiceTests
{
    private static IDocsFetcher CreateMockFetcher(string? content)
    {
        return new MockDocsFetcher(content);
    }

    [Fact]
    public async Task ListDocumentsAsync_ReturnsAllDocuments()
    {
        var content = """
            # Redis Integration
            > Connect to Redis for caching.

            Redis content.

            # PostgreSQL Integration
            > Connect to PostgreSQL databases.

            PostgreSQL content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var docs = await service.ListDocumentsAsync();

        Assert.Equal(2, docs.Count);
        Assert.Contains(docs, d => d.Title == "Redis Integration");
        Assert.Contains(docs, d => d.Title == "PostgreSQL Integration");
    }

    [Fact]
    public async Task ListDocumentsAsync_WhenFetchFails_ReturnsEmptyList()
    {
        var fetcher = CreateMockFetcher(null);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var docs = await service.ListDocumentsAsync();

        Assert.Empty(docs);
    }

    [Fact]
    public async Task SearchAsync_FindsDocumentByTitle()
    {
        var content = """
            # Redis Integration
            > Connect to Redis for caching.

            Redis content.

            # PostgreSQL Integration
            > Connect to PostgreSQL databases.

            PostgreSQL content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis");

        Assert.NotEmpty(results);
        Assert.Equal("Redis Integration", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_FindsDocumentBySummary()
    {
        var content = """
            # Integration Guide
            > Learn how to connect Redis caching to your app.

            Some content here.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("caching");

        Assert.NotEmpty(results);
        Assert.Equal("Integration Guide", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_FindsDocumentBySectionHeading()
    {
        var content = """
            # Getting Started
            > Quick start guide.

            ## Configuration Options
            Configure the app using environment variables.

            ## Deployment Steps
            Deploy to Azure Container Apps.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Configuration");

        Assert.NotEmpty(results);
        Assert.Equal("Getting Started", results[0].Title);
        Assert.Equal("Configuration Options", results[0].MatchedSection);
    }

    [Fact]
    public async Task SearchAsync_TitleMatchScoresHigherThanBodyMatch()
    {
        var content = """
            # Redis Overview
            > Official Redis documentation.

            This document covers Redis basics and setup.

            # Database Overview
            > Learn about databases.

            PostgreSQL and MySQL are popular database options. Redis is sometimes mentioned.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis");

        Assert.NotEmpty(results);
        // Document with "Redis" in title should rank higher
        Assert.Equal("Redis Overview", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_FindsCodeIdentifiers()
    {
        var content = """
            # Redis Integration
            > Add Redis to your app.

            ## Usage

            ```csharp
            var redis = builder.AddRedis("cache");
            ```

            Call `AddRedis` to add a Redis resource.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("AddRedis");

        Assert.NotEmpty(results);
        Assert.Equal("Redis Integration", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_RespectsTopKLimit()
    {
        var content = """
            # Doc 1
            > Redis documentation.

            Redis content here.

            # Doc 2
            > More Redis info.

            Redis info here.

            # Doc 3
            > Yet more Redis.

            More Redis content.

            # Doc 4
            > Redis again.

            Redis again here.

            # Doc 5
            > Redis everywhere.

            Redis everywhere here.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis", topK: 3);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        var content = """
            # Some Document
            Content here.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithWhitespaceQuery_ReturnsEmptyResults()
    {
        var content = """
            # Some Document
            Content here.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("   ");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_MultiWordQuery_FindsAllTerms()
    {
        var content = """
            # Redis Caching Guide
            > How to use Redis for caching.

            Implement distributed caching with Redis.

            # Memory Caching
            > In-memory caching without Redis.

            Simple memory cache implementation.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis caching");

        Assert.NotEmpty(results);
        // Document with both terms should rank highest
        Assert.Equal("Redis Caching Guide", results[0].Title);
    }

    [Fact]
    public async Task GetDocumentAsync_BySlug_ReturnsDocument()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("redis-integration");

        Assert.NotNull(doc);
        Assert.Equal("Redis Integration", doc.Title);
        Assert.Equal("redis-integration", doc.Slug);
    }

    [Fact]
    public async Task GetDocumentAsync_CaseInsensitive()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("REDIS-INTEGRATION");

        Assert.NotNull(doc);
        Assert.Equal("Redis Integration", doc.Title);
    }

    [Fact]
    public async Task GetDocumentAsync_UnknownSlug_ReturnsNull()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("nonexistent-doc");

        Assert.Null(doc);
    }

    [Fact]
    public async Task GetDocumentAsync_WithSection_ReturnsOnlySection()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Main content.

            ## Installation
            Install via NuGet.

            ## Configuration
            Configure connection strings.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("redis-integration", "Installation");

        Assert.NotNull(doc);
        Assert.Contains("Install via NuGet", doc.Content);
        Assert.DoesNotContain("Configure connection strings", doc.Content);
    }

    [Fact]
    public async Task GetDocumentAsync_WithPartialSectionName_FindsSection()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            ## Getting Started with Redis
            Quick start content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("redis-integration", "Getting Started");

        Assert.NotNull(doc);
        Assert.Contains("Quick start content", doc.Content);
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsSectionsList()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            ## Installation
            Install content.

            ## Configuration
            Config content.

            ## Usage
            Usage content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("redis-integration");

        Assert.NotNull(doc);
        Assert.Equal(3, doc.Sections.Count);
        Assert.Contains("Installation", doc.Sections);
        Assert.Contains("Configuration", doc.Sections);
        Assert.Contains("Usage", doc.Sections);
    }

    [Fact]
    public async Task EnsureIndexedAsync_OnlyFetchesOnce()
    {
        var callCount = 0;
        var fetcher = new CountingDocsFetcher(() =>
        {
            callCount++;
            return "# Doc\nContent.";
        });
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        await service.EnsureIndexedAsync();
        await service.EnsureIndexedAsync();
        await service.EnsureIndexedAsync();

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SearchAsync_OrdersResultsByScore()
    {
        var content = """
            # Redis Quick Start
            > Get started with Redis in minutes.

            ## Installation
            Install Redis.

            # Advanced Redis Patterns
            > Deep dive into Redis patterns and best practices.

            ## Redis Pub/Sub
            Learn about Redis publish/subscribe.

            ## Redis Clustering
            Configure Redis clustering for high availability.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis");

        // All results should have scores in descending order
        for (var i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Score >= results[i].Score,
                $"Results not in descending score order: {results[i - 1].Score} < {results[i].Score}");
        }
    }

    [Fact]
    public async Task SearchAsync_WithNullQuery_ReturnsEmptyResults()
    {
        var content = """
            # Some Document
            > Summary here.

            Content here.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync(null!);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetDocumentAsync_WithNullSlug_ReturnsNull()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync(null!);

        Assert.Null(doc);
    }

    [Fact]
    public async Task GetDocumentAsync_WithEmptySlug_ReturnsNull()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("");

        Assert.Null(doc);
    }

    [Fact]
    public async Task GetDocumentAsync_WithWhitespaceSlug_ReturnsNull()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("   ");

        Assert.Null(doc);
    }

    [Fact]
    public async Task ListDocumentsAsync_WhenFetchReturnsEmpty_ReturnsEmptyList()
    {
        var fetcher = CreateMockFetcher("");
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var docs = await service.ListDocumentsAsync();

        Assert.Empty(docs);
    }

    [Fact]
    public async Task ListDocumentsAsync_WhenFetchReturnsWhitespace_ReturnsEmptyList()
    {
        var fetcher = CreateMockFetcher("   \n\t\n   ");
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var docs = await service.ListDocumentsAsync();

        Assert.Empty(docs);
    }

    [Fact]
    public async Task SearchAsync_WhenNoDocsIndexed_ReturnsEmptyResults()
    {
        var fetcher = CreateMockFetcher(null);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetDocumentAsync_WhenNoDocsIndexed_ReturnsNull()
    {
        var fetcher = CreateMockFetcher(null);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("any-slug");

        Assert.Null(doc);
    }

    [Fact]
    public async Task ListDocumentsAsync_WhenFetcherThrows_PropagatesException()
    {
        var fetcher = new ThrowingDocsFetcher(new InvalidOperationException("Fetch failed"));
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListDocumentsAsync().AsTask());
    }

    [Fact]
    public async Task SearchAsync_WhenFetcherThrows_PropagatesException()
    {
        var fetcher = new ThrowingDocsFetcher(new HttpRequestException("Network error"));
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.SearchAsync("Redis").AsTask());
    }

    [Fact]
    public async Task GetDocumentAsync_WhenFetcherThrows_PropagatesException()
    {
        var fetcher = new ThrowingDocsFetcher(new TimeoutException("Request timed out"));
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        await Assert.ThrowsAsync<TimeoutException>(() => service.GetDocumentAsync("redis-integration").AsTask());
    }

    [Fact]
    public async Task EnsureIndexedAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var fetcher = new DelayingDocsFetcher("# Doc\nContent.", TimeSpan.FromSeconds(10));
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.EnsureIndexedAsync(cts.Token).AsTask());
    }

    [Fact]
    public async Task EnsureIndexedAsync_WhenFetcherThrows_PropagatesException()
    {
        var fetcher = new ThrowingDocsFetcher(new InvalidOperationException("Critical error"));
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnsureIndexedAsync().AsTask());
    }

    [Fact]
    public async Task SearchAsync_WithSpecialCharactersInQuery_HandlesGracefully()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        // Should not throw
        var results = await service.SearchAsync("Redis!@#$%^&*()");

        // May or may not find results, but should not throw
        Assert.NotNull(results);
    }

    [Fact]
    public async Task SearchAsync_WithVeryLongQuery_HandlesGracefully()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var longQuery = new string('a', 10000);

        // Should not throw
        var results = await service.SearchAsync(longQuery);

        Assert.NotNull(results);
    }

    [Fact]
    public async Task GetDocumentAsync_WithNonExistentSection_ReturnsFullDocument()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Main content here.

            ## Installation
            Install content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var doc = await service.GetDocumentAsync("redis-integration", "NonExistentSection");

        Assert.NotNull(doc);
        // When section not found, returns full content
        Assert.Contains("Main content here", doc.Content);
    }

    [Fact]
    public async Task SearchAsync_WithZeroTopK_ReturnsEmptyResults()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis", topK: 0);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithNegativeTopK_ReturnsEmptyResults()
    {
        var content = """
            # Redis Integration
            > Connect to Redis.

            Redis content.
            """;

        var fetcher = CreateMockFetcher(content);
        var service = new DocsIndexService(fetcher, NullLogger<DocsIndexService>.Instance);

        var results = await service.SearchAsync("Redis", topK: -1);

        Assert.Empty(results);
    }

    private sealed class MockDocsFetcher(string? content) : IDocsFetcher
    {
        public Task<string?> FetchDocsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(content);
        }
    }

    private sealed class CountingDocsFetcher(Func<string?> contentProvider) : IDocsFetcher
    {
        public Task<string?> FetchDocsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(contentProvider());
        }
    }

    private sealed class ThrowingDocsFetcher(Exception exception) : IDocsFetcher
    {
        public Task<string?> FetchDocsAsync(CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }

    private sealed class DelayingDocsFetcher(string? content, TimeSpan delay) : IDocsFetcher
    {
        public async Task<string?> FetchDocsAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            return content;
        }
    }
}
