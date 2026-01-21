// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// In-memory cache for aspire.dev documentation content with optional disk persistence.
/// </summary>
internal sealed class DocsCache(IMemoryCache memoryCache, ILogger<DocsCache> logger) : IDocsCache
{
    private static readonly TimeSpan s_defaultTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan s_chunksTtl = TimeSpan.FromHours(4);

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

    public Task SetAsync(string key, string content, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetCacheKey(key);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? s_defaultTtl
        };

        _memoryCache.Set(cacheKey, content, options);
        _logger.LogDebug("DocsCache set for key: {Key}, TTL: {Ttl}", key, ttl ?? s_defaultTtl);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocChunk>?> GetChunksAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetChunksCacheKey(key);
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<DocChunk>? chunks))
        {
            _logger.LogDebug("DocsCache chunks hit for key: {Key}, chunk count: {Count}", key, chunks?.Count ?? 0);
            return Task.FromResult(chunks);
        }

        _logger.LogDebug("DocsCache chunks miss for key: {Key}", key);
        return Task.FromResult<IReadOnlyList<DocChunk>?>(null);
    }

    public Task SetChunksAsync(string key, IReadOnlyList<DocChunk> chunks, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cacheKey = GetChunksCacheKey(key);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? s_chunksTtl
        };

        _memoryCache.Set(cacheKey, chunks, options);

        _logger.LogDebug("DocsCache set chunks for key: {Key}, chunk count: {Count}, TTL: {Ttl}", key, chunks.Count, ttl ?? s_chunksTtl);

        return Task.CompletedTask;
    }

    private static string GetCacheKey(string key) => $"docs:{key}";

    private static string GetChunksCacheKey(string key) => $"docs:chunks:{key}";
}
