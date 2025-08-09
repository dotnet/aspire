// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli.Cache;

/// <summary>
/// Represents a cache entry stored on disk with metadata.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
internal sealed class DiskCacheEntry<T>
{
    /// <summary>
    /// The cached value.
    /// </summary>
    [JsonPropertyName("value")]
    public T? Value { get; set; }

    /// <summary>
    /// The UTC timestamp when the entry was created.
    /// </summary>
    [JsonPropertyName("createdUtc")]
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>
    /// The UTC timestamp when the entry expires, if any.
    /// </summary>
    [JsonPropertyName("expiresUtc")]
    public DateTimeOffset? ExpiresUtc { get; set; }

    /// <summary>
    /// Checks if the cache entry has expired.
    /// </summary>
    public bool IsExpired => ExpiresUtc.HasValue && DateTimeOffset.UtcNow > ExpiresUtc.Value;
}