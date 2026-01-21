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
    Task<string?> FetchDocsAsync(CancellationToken cancellationToken = default);
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

    public async Task<string?> FetchDocsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            var cached = await _cache.GetAsync(SmallDocsUrl, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Using cached docs");
                return cached;
            }

            _logger.LogInformation("Fetching aspire.dev docs from {Url}", SmallDocsUrl);

            var response = await _httpClient.GetAsync(SmallDocsUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Cache for 4 hours (docs don't change frequently)
            await _cache.SetAsync(SmallDocsUrl, content, TimeSpan.FromHours(4), cancellationToken);

            _logger.LogDebug("Fetched docs, length: {Length} chars", content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch aspire.dev docs");
            return null;
        }
    }
}
