// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;

namespace Aspire.Cli.Cache;

/// <summary>
/// Represents a disk-based cache that provides functionality similar to IMemoryCache
/// but persists entries to disk for use across CLI executions.
/// </summary>
internal interface IDiskCache
{
    /// <summary>
    /// Gets the value associated with this key if present, or creates and caches the value
    /// using the provided factory function.
    /// </summary>
    /// <typeparam name="TItem">The type of the cached item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory function to create the value if not found in cache.</param>
    /// <returns>The cached value or the newly created value.</returns>
    Task<TItem?> GetOrCreateAsync<TItem>(string key, Func<ICacheEntry, Task<TItem>> factory) where TItem : class;
}