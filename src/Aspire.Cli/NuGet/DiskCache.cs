// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.NuGet;

/// <summary>
/// Provides persistent disk-based caching functionality for NuGet package search results.
/// </summary>
internal sealed class DiskCache : IDiskCache, IDisposable
{
    private readonly ILogger<DiskCache> _logger;
    private readonly string _cacheDirectory;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskCache"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DiskCache(ILogger<DiskCache> logger)
    {
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
        
        // Store cache in user's .aspire directory under a 'cache' subdirectory
        var userAspirePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".aspire");
        
        _cacheDirectory = Path.Combine(userAspirePath, "cache");
        
        // Ensure the cache directory exists
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create cache directory at '{CacheDirectory}'", _cacheDirectory);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        
        if (_disposed)
        {
            return null;
        }

        var cacheFilePath = GetCacheFilePath(key);
        
        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cacheItemJson = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
            var cacheItem = JsonSerializer.Deserialize(cacheItemJson, JsonSourceGenerationContext.Default.DiskCacheItem);
            
            if (cacheItem is null)
            {
                _logger.LogWarning("Failed to deserialize cache item from '{CacheFilePath}'", cacheFilePath);
                return null;
            }

            // Check if the cache item has expired
            if (cacheItem.Expiration.HasValue && DateTime.UtcNow > cacheItem.Expiration.Value)
            {
                _logger.LogDebug("Cache item '{Key}' has expired, removing from disk", key);
                
                // Remove expired item
                try
                {
                    File.Delete(cacheFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete expired cache file '{CacheFilePath}'", cacheFilePath);
                }
                
                return null;
            }

            // Deserialize the actual cached value based on the type
            if (typeof(T) == typeof(List<NuGetPackage>))
            {
                var packages = JsonSerializer.Deserialize(cacheItem.Value, JsonSourceGenerationContext.Default.ListNuGetPackageCli);
                return packages as T;
            }

            // For other types, we might need to add them to the source generation context
            throw new NotSupportedException($"Type {typeof(T)} is not supported for disk caching");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cache item from '{CacheFilePath}'", cacheFilePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading cache item '{Key}' from disk", key);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        
        if (_disposed)
        {
            return;
        }

        var cacheFilePath = GetCacheFilePath(key);
        var expirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;

        string serializedValue;
        if (typeof(T) == typeof(List<NuGetPackage>) && value is List<NuGetPackage> packages)
        {
            serializedValue = JsonSerializer.Serialize(packages, JsonSourceGenerationContext.Default.ListNuGetPackageCli);
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported for disk caching");
        }
        
        var cacheItem = new DiskCacheItem
        {
            Key = key,
            Value = serializedValue,
            CreatedAt = DateTime.UtcNow,
            Expiration = expirationTime
        };

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cacheItemJson = JsonSerializer.Serialize(cacheItem, JsonSourceGenerationContext.Default.DiskCacheItem);
            await File.WriteAllTextAsync(cacheFilePath, cacheItemJson, cancellationToken);
            
            _logger.LogDebug("Cached item '{Key}' to disk with expiration '{Expiration}'", key, expirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write cache item '{Key}' to disk", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        if (_disposed)
        {
            return;
        }

        var cacheFilePath = GetCacheFilePath(key);
        
        if (!File.Exists(cacheFilePath))
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            File.Delete(cacheFilePath);
            _logger.LogDebug("Removed cache item '{Key}' from disk", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache item '{Key}' from disk", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task CleanupExpiredItemsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.cache");
            var expiredCount = 0;
            
            foreach (var cacheFile in cacheFiles)
            {
                try
                {
                    var cacheItemJson = await File.ReadAllTextAsync(cacheFile, cancellationToken);
                    var cacheItem = JsonSerializer.Deserialize(cacheItemJson, JsonSourceGenerationContext.Default.DiskCacheItem);
                    
                    if (cacheItem?.Expiration.HasValue == true && DateTime.UtcNow > cacheItem.Expiration.Value)
                    {
                        File.Delete(cacheFile);
                        expiredCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process cache file '{CacheFile}' during cleanup", cacheFile);
                }
            }
            
            if (expiredCount > 0)
            {
                _logger.LogDebug("Cleaned up {ExpiredCount} expired cache items", expiredCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache cleanup");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string GetCacheFilePath(string key)
    {
        // Create a safe filename from the cache key using SHA256 hash
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = SHA256.HashData(keyBytes);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return Path.Combine(_cacheDirectory, $"{hashString}.cache");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}