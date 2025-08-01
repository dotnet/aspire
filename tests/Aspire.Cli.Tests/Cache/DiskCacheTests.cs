// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Cache;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Tests.Cache;

public class DiskCacheTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldCacheValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = NullLoggerFactory.Instance.CreateLogger<DiskCache>();
        var cache = new DiskCache(logger);

        var key = "test-key";
        var expectedValue = new List<NuGetPackage>
        {
            new() { Id = "TestPackage", Version = "1.0.0", Source = "nuget.org" }
        };

        var factoryCallCount = 0;
        var factory = new Func<ICacheEntry, Task<List<NuGetPackage>>>(entry =>
        {
            factoryCallCount++;
            return Task.FromResult(expectedValue);
        });

        // First call should invoke factory
        var result1 = await cache.GetOrCreateAsync(key, factory);
        Assert.Equal(1, factoryCallCount);
        Assert.NotNull(result1);
        Assert.Single(result1);
        Assert.Equal("TestPackage", result1.First().Id);

        // Second call should use cache
        var result2 = await cache.GetOrCreateAsync(key, factory);
        Assert.Equal(1, factoryCallCount); // Factory should not be called again
        Assert.NotNull(result2);
        Assert.Single(result2);
        Assert.Equal("TestPackage", result2.First().Id);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldRespectExpiration()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = NullLoggerFactory.Instance.CreateLogger<DiskCache>();
        var cache = new DiskCache(logger);

        var key = "expiring-key";
        var expectedValue = new List<NuGetPackage>
        {
            new() { Id = "ExpiringPackage", Version = "1.0.0", Source = "nuget.org" }
        };

        var factoryCallCount = 0;
        var factory = new Func<ICacheEntry, Task<List<NuGetPackage>>>(entry =>
        {
            factoryCallCount++;
            // Set a very short expiration for testing
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1);
            return Task.FromResult(expectedValue);
        });

        // First call should invoke factory
        var result1 = await cache.GetOrCreateAsync(key, factory);
        Assert.Equal(1, factoryCallCount);
        Assert.NotNull(result1);

        // Wait for expiration
        await Task.Delay(100);

        // Second call should invoke factory again due to expiration
        var result2 = await cache.GetOrCreateAsync(key, factory);
        Assert.Equal(2, factoryCallCount); // Factory should be called again
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldHandleEmptyValues()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = NullLoggerFactory.Instance.CreateLogger<DiskCache>();
        var cache = new DiskCache(logger);

        var key = "empty-key";
        var expectedValue = new List<NuGetPackage>();

        var factoryCallCount = 0;
        var factory = new Func<ICacheEntry, Task<List<NuGetPackage>>>(entry =>
        {
            factoryCallCount++;
            return Task.FromResult(expectedValue);
        });

        var result = await cache.GetOrCreateAsync(key, factory);
        Assert.Equal(1, factoryCallCount);
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}