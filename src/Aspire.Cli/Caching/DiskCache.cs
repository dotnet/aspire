// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Caching;

internal sealed class DiskCache : IDiskCache
{
    private static readonly TimeSpan s_defaultCacheExpiryWindow = TimeSpan.FromHours(3);
    private static readonly TimeSpan s_defaultMaxCacheAge = TimeSpan.FromDays(7);

    private readonly ILogger<DiskCache> _logger;
    private readonly DirectoryInfo _cacheDirectory;
    private readonly TimeSpan _expiryWindow;
    private readonly TimeSpan _maxAge;

    public DiskCache(ILogger<DiskCache> logger, CliExecutionContext executionContext, IConfiguration configuration)
    {
        _logger = logger;
        _cacheDirectory = new DirectoryInfo(Path.Combine(executionContext.CacheDirectory.FullName, "nuget-search"));
        if (!_cacheDirectory.Exists)
        {
            try { _cacheDirectory.Create(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { _logger.LogDebug(ex, "Failed to create cache directory {CacheDirectory}", _cacheDirectory.FullName); }
        }
        _expiryWindow = ReadWindow(configuration, "PackageSearchCacheExpirySeconds", s_defaultCacheExpiryWindow);
        _maxAge = ReadWindow(configuration, "PackageSearchMaxCacheAgeSeconds", s_defaultMaxCacheAge);
    }

    private static TimeSpan ReadWindow(IConfiguration configuration, string key, TimeSpan fallback)
    {
        if (configuration[key] is string secondsString && double.TryParse(secondsString, out var seconds) && seconds > 0)
        {
            return TimeSpan.FromSeconds(seconds);
        }
        return fallback;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var keyHash = HashKey(key);
            var cacheFilePath = ResolveValidCacheFile(keyHash);
            if (cacheFilePath is null)
            {
                _logger.LogDebug("Disk cache miss for key {RawKey}", key);
                return null;
            }
            _logger.LogDebug("Disk cache hit for key {RawKey} (file: {CacheFilePath})", key, cacheFilePath);
            try { return await File.ReadAllTextAsync(cacheFilePath, cancellationToken).ConfigureAwait(false); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { _logger.LogDebug(ex, "Failed to read cache file {CacheFilePath}; treating as miss", cacheFilePath); return null; }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogDebug(ex, "Failed to retrieve cache entry for key {RawKey}", key);
            return null;
        }
    }

    public async Task SetAsync(string key, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_cacheDirectory.Exists)
            {
                try { _cacheDirectory.Create(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { _logger.LogDebug(ex, "Failed to create cache directory {CacheDirectory}", _cacheDirectory.FullName); return; }
            }
            var keyHash = HashKey(key);
            var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var fileName = $"{keyHash}.{currentUnixTime}.json";
            var fullPath = Path.Combine(_cacheDirectory.FullName, fileName);
            var tempFile = fullPath + ".tmp";
            await File.WriteAllTextAsync(tempFile, content, cancellationToken).ConfigureAwait(false);
            if (File.Exists(fullPath)) { try { File.Delete(fullPath); } catch { } }
            File.Move(tempFile, fullPath);
            _logger.LogDebug("Stored disk cache entry for key {RawKey} (file: {CacheFilePath})", key, fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogDebug(ex, "Failed to write disk cache entry for key {RawKey}", key);
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cacheDirectory.Exists)
            {
                foreach (var file in _cacheDirectory.GetFiles("*.json", SearchOption.TopDirectoryOnly))
                {
                    try { file.Delete(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { _logger.LogDebug(ex, "Failed to delete cache file {CacheFile}", file.FullName); }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogDebug(ex, "Failed to clear disk cache at {CacheDirectory}", _cacheDirectory.FullName);
        }
        return Task.CompletedTask;
    }

    private static string HashKey(string key)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

    private string? ResolveValidCacheFile(string keyHash)
    {
        try
        {
            if (!_cacheDirectory.Exists)
            {
                return null;
            }
            var currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var matchingFiles = _cacheDirectory.GetFiles($"{keyHash}.*.json");
            var allFiles = _cacheDirectory.GetFiles("*.json");
            var list = new List<(FileInfo file, long timestamp)>();
            foreach (var file in matchingFiles)
            {
                var nameNoExt = Path.GetFileNameWithoutExtension(file.Name);
                var parts = nameNoExt.Split('.');
                if (parts.Length >= 2 && long.TryParse(parts[1], out var ts)) { list.Add((file, ts)); }
                else { TryDelete(file, invalid: true); }
            }
            list.Sort((a, b) => b.timestamp.CompareTo(a.timestamp));
            string? valid = null;
            for (var i = 0; i < list.Count; i++)
            {
                var (file, ts) = list[i];
                var age = TimeSpan.FromSeconds(currentUnixTime - ts);
                if (i == 0 && age <= _expiryWindow) { valid = file.FullName; }
                else { TryDelete(file, expired: age > _expiryWindow); }
            }
            foreach (var file in allFiles)
            {
                if (matchingFiles.Contains(file)) { continue; }
                var nameNoExt = Path.GetFileNameWithoutExtension(file.Name);
                var parts = nameNoExt.Split('.');
                if (parts.Length >= 2 && long.TryParse(parts[1], out var ts))
                {
                    var age = TimeSpan.FromSeconds(currentUnixTime - ts);
                    if (age > _maxAge) { TryDelete(file, old: true); }
                }
                else { TryDelete(file, invalid: true); }
            }
            return valid;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogDebug(ex, "Failed to resolve cache file for key hash {KeyHash}", keyHash);
            return null;
        }
    }

    private void TryDelete(FileInfo file, bool expired = false, bool old = false, bool invalid = false)
    {
        try
        {
            file.Delete();
            if (expired)
            {
                _logger.LogDebug("Deleted expired cache file: {CacheFile}", file.FullName);
            }
            else if (old)
            {
                _logger.LogDebug("Deleted old cache file during global cleanup: {CacheFile}", file.FullName);
            }
            else if (invalid)
            {
                _logger.LogDebug("Deleted invalid cache file: {CacheFile}", file.FullName);
            }
            else
            {
                _logger.LogDebug("Deleted cache file: {CacheFile}", file.FullName);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _logger.LogDebug(ex, "Failed to delete cache file {CacheFile}", file.FullName);
        }
    }
}
