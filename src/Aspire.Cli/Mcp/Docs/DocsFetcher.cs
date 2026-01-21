// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// Service for fetching aspire.dev documentation content.
/// </summary>
internal interface IDocsFetcher
{
    /// <summary>
    /// Fetches the llms.txt index file from aspire.dev.
    /// </summary>
    Task<DocsIndex?> FetchIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the small (abridged) documentation content.
    /// </summary>
    Task<string?> FetchSmallDocsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the full documentation content.
    /// </summary>
    Task<string?> FetchFullDocsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the aspire.dev documentation index.
/// </summary>
internal sealed class DocsIndex
{
    public required string Description { get; init; }
    public required string SmallDocsUrl { get; init; }
    public required string FullDocsUrl { get; init; }
}

/// <summary>
/// Default implementation of <see cref="IDocsFetcher"/> that fetches from aspire.dev.
/// </summary>
internal sealed class DocsFetcher : IDocsFetcher
{
    private const string IndexUrl = "https://aspire.dev/llms.txt";
    private const string SmallDocsUrl = "https://aspire.dev/llms-small.txt";
    private const string FullDocsUrl = "https://aspire.dev/llms-full.txt";

    private readonly HttpClient _httpClient;
    private readonly IDocsCache _cache;
    private readonly ILogger<DocsFetcher> _logger;

    public DocsFetcher(HttpClient httpClient, IDocsCache cache, ILogger<DocsFetcher> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DocsIndex?> FetchIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cached = await _cache.GetAsync(IndexUrl, cancellationToken);
            if (cached is not null)
            {
                return ParseIndex(cached);
            }

            _logger.LogDebug("Fetching aspire.dev docs index from {Url}", IndexUrl);

            var response = await _httpClient.GetAsync(IndexUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Cache for 1 hour
            await _cache.SetAsync(IndexUrl, content, TimeSpan.FromHours(1), cancellationToken);

            return ParseIndex(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch aspire.dev docs index");
            return null;
        }
    }

    public async Task<string?> FetchSmallDocsAsync(CancellationToken cancellationToken = default)
    {
        return await FetchDocsAsync(SmallDocsUrl, "small", cancellationToken);
    }

    public async Task<string?> FetchFullDocsAsync(CancellationToken cancellationToken = default)
    {
        return await FetchDocsAsync(FullDocsUrl, "full", cancellationToken);
    }

    private async Task<string?> FetchDocsAsync(string url, string variant, CancellationToken cancellationToken)
    {
        try
        {
            // Check cache first
            var cached = await _cache.GetAsync(url, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Using cached {Variant} docs", variant);
                return cached;
            }

            _logger.LogInformation("Fetching aspire.dev {Variant} docs from {Url}", variant, url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Cache for 4 hours (docs don't change frequently)
            await _cache.SetAsync(url, content, TimeSpan.FromHours(4), cancellationToken);

            _logger.LogDebug("Fetched {Variant} docs, length: {Length} chars", variant, content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch aspire.dev {Variant} docs", variant);
            return null;
        }
    }

    private static DocsIndex? ParseIndex(string content)
    {
        // Parse the llms.txt format which contains links to small and full docs
        // Expected format:
        // # Aspire
        // > Description
        // ## Documentation Sets
        // - [Abridged documentation](https://aspire.dev/llms-small.txt): ...
        // - [Complete documentation](https://aspire.dev/llms-full.txt): ...

        var lines = content.Split('\n');
        string? description = null;
        string smallDocsUrl = SmallDocsUrl;
        string fullDocsUrl = FullDocsUrl;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith(">", StringComparison.Ordinal) && description is null)
            {
                description = trimmed[1..].Trim();
            }
            else if (trimmed.Contains("llms-small.txt", StringComparison.OrdinalIgnoreCase))
            {
                var urlStart = trimmed.IndexOf("https://", StringComparison.OrdinalIgnoreCase);
                if (urlStart >= 0)
                {
                    var urlEnd = trimmed.IndexOf(')', urlStart);
                    if (urlEnd > urlStart)
                    {
                        smallDocsUrl = trimmed[urlStart..urlEnd];
                    }
                }
            }
            else if (trimmed.Contains("llms-full.txt", StringComparison.OrdinalIgnoreCase))
            {
                var urlStart = trimmed.IndexOf("https://", StringComparison.OrdinalIgnoreCase);
                if (urlStart >= 0)
                {
                    var urlEnd = trimmed.IndexOf(')', urlStart);
                    if (urlEnd > urlStart)
                    {
                        fullDocsUrl = trimmed[urlStart..urlEnd];
                    }
                }
            }
        }

        return new DocsIndex
        {
            Description = description ?? "Aspire documentation",
            SmallDocsUrl = smallDocsUrl,
            FullDocsUrl = fullDocsUrl
        };
    }
}
