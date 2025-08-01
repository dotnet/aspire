// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Aspire.Cli.Cache;

/// <summary>
/// A disk-based cache implementation that stores entries as JSON files.
/// </summary>
internal sealed class DiskCache : IDiskCache
{
    private readonly ILogger<DiskCache> _logger;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DiskCache(ILogger<DiskCache> logger)
    {
        _logger = logger;
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _cacheDirectory = Path.Combine(homeDirectory, ".aspire", "cache");
        EnsureCacheDirectoryExists();
    }

    public async Task<TItem?> GetOrCreateAsync<TItem>(string key, Func<ICacheEntry, Task<TItem>> factory) where TItem : class
    {
        var fileName = GetCacheFileName(key);
        var filePath = Path.Combine(_cacheDirectory, fileName);

        await _semaphore.WaitAsync();
        try
        {
            // Try to load from cache first
            if (await TryLoadFromCacheAsync<TItem>(filePath) is { } cachedValue)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);

            // Create a cache entry for the factory
            var cacheEntry = new MemoryCacheEntryOptionsAccessor();
            var value = await factory(cacheEntry);

            // Save to cache
            await SaveToCacheAsync(filePath, value, cacheEntry);

            return value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Cache entries are known types")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Cache entries are known types")]
    private async Task<TItem?> TryLoadFromCacheAsync<TItem>(string filePath) where TItem : class
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return default;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var cacheEntry = JsonSerializer.Deserialize<DiskCacheEntry<TItem>>(jsonContent, s_jsonOptions);

            if (cacheEntry?.IsExpired == true)
            {
                _logger.LogDebug("Cache entry expired, deleting file: {FilePath}", filePath);
                File.Delete(filePath);
                return default;
            }

            return cacheEntry?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cache entry from {FilePath}", filePath);
            // Delete corrupted cache file
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore deletion errors
            }
            return default;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Cache entries are known types")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Cache entries are known types")]
    private async Task SaveToCacheAsync<TItem>(string filePath, TItem value, MemoryCacheEntryOptionsAccessor cacheEntry) where TItem : class
    {
        try
        {
            var diskEntry = new DiskCacheEntry<TItem>
            {
                Value = value,
                CreatedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = cacheEntry.AbsoluteExpirationRelativeToNow.HasValue
                    ? DateTimeOffset.UtcNow.Add(cacheEntry.AbsoluteExpirationRelativeToNow.Value)
                    : cacheEntry.AbsoluteExpiration
            };

            var jsonContent = JsonSerializer.Serialize(diskEntry, s_jsonOptions);
            await File.WriteAllTextAsync(filePath, jsonContent);

            _logger.LogDebug("Saved cache entry to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save cache entry to {FilePath}", filePath);
        }
    }

    private static string GetCacheFileName(string key)
    {
        // Create a deterministic file name using SHA256 hash of the key
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = SHA256.HashData(keyBytes);
        var hash = Convert.ToHexString(hashBytes);
        return $"{hash.ToLowerInvariant()}.json";
    }

    private void EnsureCacheDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
                _logger.LogDebug("Created cache directory: {CacheDirectory}", _cacheDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create cache directory: {CacheDirectory}", _cacheDirectory);
        }
    }

    /// <summary>
    /// Helper class to provide access to ICacheEntry properties for setting expiration.
    /// </summary>
    private sealed class MemoryCacheEntryOptionsAccessor : ICacheEntry
    {
        public object Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose()
        {
            // No-op for our implementation
        }
    }
}