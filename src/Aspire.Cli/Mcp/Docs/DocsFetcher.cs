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
    /// Fetches the small (abridged) documentation content.
    /// </summary>
    Task<string?> FetchSmallDocsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IDocsFetcher"/> that fetches from aspire.dev.
/// </summary>
internal sealed class DocsFetcher(HttpClient httpClient, IDocsCache cache, ILogger<DocsFetcher> logger) : IDocsFetcher
{
    private const string SmallDocsUrl = "https://aspire.dev/llms-small.txt";

    private readonly HttpClient _httpClient = httpClient;
    private readonly IDocsCache _cache = cache;
    private readonly ILogger<DocsFetcher> _logger = logger;

    public async Task<string?> FetchSmallDocsAsync(CancellationToken cancellationToken = default)
    {
        return await FetchDocsAsync(SmallDocsUrl, "small", cancellationToken);
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
}
