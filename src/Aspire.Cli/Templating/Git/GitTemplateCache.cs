// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Local file cache for resolved template indexes.
/// </summary>
internal sealed class GitTemplateCache
{
    private readonly string _cacheDir;

    public GitTemplateCache(string cacheDir)
    {
        _cacheDir = cacheDir;
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Gets a cached index if it exists and is within the TTL.
    /// </summary>
    public GitTemplateIndex? Get(string cacheKey, TimeSpan ttl)
    {
        var path = GetPath(cacheKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var info = new FileInfo(path);
        if (DateTime.UtcNow - info.LastWriteTimeUtc > ttl)
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);
        }
        catch
        {
            // Corrupted cache entry — delete and return null.
            TryDelete(path);
            return null;
        }
    }

    /// <summary>
    /// Stores an index in the cache.
    /// </summary>
    public void Set(string cacheKey, GitTemplateIndex index)
    {
        var path = GetPath(cacheKey);
        var json = JsonSerializer.Serialize(index, GitTemplateJsonContext.RelaxedEscaping.GitTemplateIndex);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Removes all cached entries.
    /// </summary>
    public void Clear()
    {
        if (Directory.Exists(_cacheDir))
        {
            foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
            {
                TryDelete(file);
            }
        }
    }

    private string GetPath(string cacheKey)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey))).ToLowerInvariant();
        return Path.Combine(_cacheDir, $"{hash}.json");
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
