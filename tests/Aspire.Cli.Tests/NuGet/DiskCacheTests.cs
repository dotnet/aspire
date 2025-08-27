// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.NuGet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Tests.NuGet;

public class DiskCacheTests : IDisposable
{
    private readonly TempDirectory _tempDirectory;
    private readonly DiskCache _diskCache;
    private readonly ILogger<DiskCache> _logger;

    public DiskCacheTests()
    {
        _tempDirectory = new TempDirectory();
        _logger = NullLogger<DiskCache>.Instance;
        
        // Override the user directory for testing
        Environment.SetEnvironmentVariable("HOME", _tempDirectory.Path);
        Environment.SetEnvironmentVariable("USERPROFILE", _tempDirectory.Path);
        
        _diskCache = new DiskCache(_logger);
    }

    [Fact]
    public async Task SetAsync_Should_StoreValueOnDisk()
    {
        // Arrange
        var key = "test-key";
        var packages = new List<NuGetPackage>
        {
            new() { Id = "Test.Package", Version = "1.0.0", Source = "nuget.org" }
        };

        // Act
        await _diskCache.SetAsync(key, packages);

        // Assert
        var retrieved = await _diskCache.GetAsync<List<NuGetPackage>>(key);
        
        Assert.NotNull(retrieved);
        Assert.Single(retrieved);
        Assert.Equal("Test.Package", retrieved.First().Id);
        Assert.Equal("1.0.0", retrieved.First().Version);
        Assert.Equal("nuget.org", retrieved.First().Source);
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNullForNonExistentKey()
    {
        // Act
        var result = await _diskCache.GetAsync<List<NuGetPackage>>("non-existent-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNullForExpiredItem()
    {
        // Arrange
        var key = "expired-key";
        var packages = new List<NuGetPackage>
        {
            new() { Id = "Test.Package", Version = "1.0.0", Source = "nuget.org" }
        };

        // Act - Store with very short expiration
        await _diskCache.SetAsync(key, packages, TimeSpan.FromMilliseconds(1));
        
        // Wait for expiration
        await Task.Delay(10);

        var result = await _diskCache.GetAsync<List<NuGetPackage>>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_Should_DeleteCachedItem()
    {
        // Arrange
        var key = "remove-key";
        var packages = new List<NuGetPackage>
        {
            new() { Id = "Test.Package", Version = "1.0.0", Source = "nuget.org" }
        };

        await _diskCache.SetAsync(key, packages);

        // Act
        await _diskCache.RemoveAsync(key);

        // Assert
        var result = await _diskCache.GetAsync<List<NuGetPackage>>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task CleanupExpiredItemsAsync_Should_RemoveExpiredItems()
    {
        // Arrange
        var expiredKey = "expired-key";
        var validKey = "valid-key";
        var packages = new List<NuGetPackage>
        {
            new() { Id = "Test.Package", Version = "1.0.0", Source = "nuget.org" }
        };

        // Store one item with very short expiration and one without expiration
        await _diskCache.SetAsync(expiredKey, packages, TimeSpan.FromMilliseconds(1));
        await _diskCache.SetAsync(validKey, packages);
        
        // Wait for one item to expire
        await Task.Delay(10);

        // Act
        await _diskCache.CleanupExpiredItemsAsync();

        // Assert
        var expiredResult = await _diskCache.GetAsync<List<NuGetPackage>>(expiredKey);
        var validResult = await _diskCache.GetAsync<List<NuGetPackage>>(validKey);
        
        Assert.Null(expiredResult);
        Assert.NotNull(validResult);
    }

    [Fact]
    public async Task SetAsync_Should_HandleMultiplePackages()
    {
        // Arrange
        var key = "multi-packages";
        var packages = new List<NuGetPackage>
        {
            new() { Id = "Package.One", Version = "1.0.0", Source = "nuget.org" },
            new() { Id = "Package.Two", Version = "2.0.0", Source = "private-feed" },
            new() { Id = "Package.Three", Version = "1.5.0", Source = "nuget.org" }
        };

        // Act
        await _diskCache.SetAsync(key, packages);

        // Assert
        var retrieved = await _diskCache.GetAsync<List<NuGetPackage>>(key);
        
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.Count);
        Assert.Contains(retrieved, p => p.Id == "Package.One");
        Assert.Contains(retrieved, p => p.Id == "Package.Two");
        Assert.Contains(retrieved, p => p.Id == "Package.Three");
    }

    [Fact]
    public async Task SetAsync_Should_OverwriteExistingKey()
    {
        // Arrange
        var key = "overwrite-key";
        var originalPackages = new List<NuGetPackage>
        {
            new() { Id = "Original.Package", Version = "1.0.0", Source = "nuget.org" }
        };
        var newPackages = new List<NuGetPackage>
        {
            new() { Id = "New.Package", Version = "2.0.0", Source = "private-feed" }
        };

        // Act
        await _diskCache.SetAsync(key, originalPackages);
        await _diskCache.SetAsync(key, newPackages);

        // Assert
        var retrieved = await _diskCache.GetAsync<List<NuGetPackage>>(key);
        
        Assert.NotNull(retrieved);
        Assert.Single(retrieved);
        Assert.Equal("New.Package", retrieved.First().Id);
        Assert.Equal("2.0.0", retrieved.First().Version);
        Assert.Equal("private-feed", retrieved.First().Source);
    }

    public void Dispose()
    {
        _diskCache?.Dispose();
        _tempDirectory?.Dispose();
    }
}