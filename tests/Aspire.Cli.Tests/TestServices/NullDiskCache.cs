// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Caching;

namespace Aspire.Cli.Tests.TestServices;

/// <summary>
/// A no-op disk cache used in tests where deterministic, cache-less behavior is desired.
/// Always returns null on get and ignores set/clear operations.
/// </summary>
internal sealed class NullDiskCache : IDiskCache
{
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task SetAsync(string key, string content, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ClearAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
