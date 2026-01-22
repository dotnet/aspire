// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// In-memory cache for aspire.dev documentation content with ETag support.
/// </summary>
internal sealed class DocsCache(IMemoryCache memoryCache, ILogger<DocsCache> logger) : IDocsCache
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<DocsCache> _logger = logger;

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetCacheKey(key);

        if (_memoryCache.TryGetValue(cacheKey, out string? content))
        {
            _logger.LogDebug("DocsCache hit for key: {Key}", key);

            return Task.FromResult<string?>(content);
        }

        _logger.LogDebug("DocsCache miss for key: {Key}", key);

        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string content, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetCacheKey(key);

        // No expiration - content is invalidated when ETag changes
        _memoryCache.Set(cacheKey, content);
        _logger.LogDebug("DocsCache set for key: {Key}", key);

        return Task.CompletedTask;
    }

    public Task<string?> GetETagAsync(string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetETagCacheKey(url);

        if (_memoryCache.TryGetValue(cacheKey, out string? etag))
        {
            _logger.LogDebug("DocsCache ETag hit for url: {Url}", url);

            return Task.FromResult<string?>(etag);
        }

        _logger.LogDebug("DocsCache ETag miss for url: {Url}", url);

        return Task.FromResult<string?>(null);
    }

    public Task SetETagAsync(string url, string? etag, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetETagCacheKey(url);

        if (etag is null)
        {
            _memoryCache.Remove(cacheKey);
            _logger.LogDebug("DocsCache cleared ETag for url: {Url}", url);
        }
        else
        {
            // No expiration - ETag is used to validate content freshness
            _memoryCache.Set(cacheKey, etag);
            _logger.LogDebug("DocsCache set ETag for url: {Url}, ETag: {ETag}", url, etag);
        }

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetCacheKey(key);
        _memoryCache.Remove(cacheKey);
        _logger.LogDebug("DocsCache invalidated key: {Key}", key);

        return Task.CompletedTask;
    }

    private static string GetCacheKey(string key) => $"docs:{key}";

    private static string GetETagCacheKey(string url) => $"docs:etag:{url}";
}
