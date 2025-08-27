// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.NuGet;

/// <summary>
/// Provides persistent disk-based caching functionality for NuGet package search results.
/// </summary>
internal interface IDiskCache
{
    /// <summary>
    /// Retrieves a cached value from disk storage.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached value if found and not expired, otherwise null.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores a value in disk cache with an optional expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time for the cached item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached item from disk storage.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all expired items from the disk cache.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CleanupExpiredItemsAsync(CancellationToken cancellationToken = default);
}