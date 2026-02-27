// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Fetches, parses, and caches template indexes from git-hosted sources.
/// </summary>
internal sealed class GitTemplateIndexService : IGitTemplateIndexService
{
    private const string DefaultRepo = "https://github.com/dotnet/aspire";
    private const string DefaultRef = "release/latest";
    private const string IndexFileName = "aspire-template-index.json";
    private const int MaxIncludeDepth = 5;

    private static readonly TimeSpan s_defaultCacheTtl = TimeSpan.FromHours(1);

    private readonly GitTemplateCache _cache;
    private readonly IConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitTemplateIndexService> _logger;

    public GitTemplateIndexService(
        GitTemplateCache cache,
        IConfigurationService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<GitTemplateIndexService> logger)
    {
        _cache = cache;
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ResolvedTemplate>> GetTemplatesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        var sources = await GetSourcesAsync(cancellationToken).ConfigureAwait(false);
        var result = new List<ResolvedTemplate>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            await ResolveIndexAsync(source, result, visited, depth: 0, forceRefresh, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        await GetTemplatesAsync(forceRefresh: true, cancellationToken).ConfigureAwait(false);
    }

    private async Task ResolveIndexAsync(
        GitTemplateSource source,
        List<ResolvedTemplate> result,
        HashSet<string> visited,
        int depth,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (depth > MaxIncludeDepth)
        {
            _logger.LogWarning("Max include depth ({Depth}) reached for {Repo}, skipping.", MaxIncludeDepth, source.Repo);
            return;
        }

        if (!visited.Add(source.CacheKey))
        {
            _logger.LogDebug("Cycle detected for {CacheKey}, skipping.", source.CacheKey);
            return;
        }

        var index = forceRefresh ? null : _cache.Get(source.CacheKey, s_defaultCacheTtl);

        if (index is null)
        {
            index = await FetchIndexAsync(source, cancellationToken).ConfigureAwait(false);

            if (index is null)
            {
                _logger.LogDebug("No index found at {Repo}@{Ref}.", source.Repo, source.Ref ?? "HEAD");
                return;
            }

            _cache.Set(source.CacheKey, index);
        }

        foreach (var entry in index.Templates)
        {
            result.Add(new ResolvedTemplate { Entry = entry, Source = source });
        }

        if (index.Includes is { Count: > 0 })
        {
            foreach (var include in index.Includes)
            {
                var includeSource = new GitTemplateSource
                {
                    Name = include.Url,
                    Repo = include.Url,
                    Kind = GitTemplateSourceKind.Configured
                };

                await ResolveIndexAsync(includeSource, result, visited, depth + 1, forceRefresh, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<GitTemplateIndex?> FetchIndexAsync(GitTemplateSource source, CancellationToken cancellationToken)
    {
        var rawUrl = BuildRawUrl(source.Repo, source.Ref ?? DefaultRef, IndexFileName);

        if (rawUrl is null)
        {
            _logger.LogWarning("Cannot build raw URL for {Repo}. Only GitHub URLs are supported.", source.Repo);
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("git-templates");
            var response = await client.GetAsync(rawUrl, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("HTTP {StatusCode} fetching index from {Url}.", response.StatusCode, rawUrl);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateIndex);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogDebug(ex, "Failed to fetch index from {Url}.", rawUrl);
            return null;
        }
    }

    private async Task<List<GitTemplateSource>> GetSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = new List<GitTemplateSource>();

        // 1. Default official source
        var disableDefault = await _configService.GetConfigurationAsync("templates.indexes.default.disabled", cancellationToken).ConfigureAwait(false);
        if (!string.Equals(disableDefault, "true", StringComparison.OrdinalIgnoreCase))
        {
            var defaultRepo = await _configService.GetConfigurationAsync("templates.indexes.default.repo", cancellationToken).ConfigureAwait(false);
            var defaultRef = await _configService.GetConfigurationAsync("templates.indexes.default.ref", cancellationToken).ConfigureAwait(false);

            sources.Add(new GitTemplateSource
            {
                Name = "default",
                Repo = defaultRepo ?? DefaultRepo,
                Ref = defaultRef ?? DefaultRef,
                Kind = GitTemplateSourceKind.Official
            });
        }

        // 2. Additional configured sources
        // Config keys: templates.indexes.<name>.repo and templates.indexes.<name>.ref
        var allConfig = await _configService.GetAllConfigurationAsync(cancellationToken).ConfigureAwait(false);
        var indexNames = allConfig.Keys
            .Where(k => k.StartsWith("templates:indexes:", StringComparison.OrdinalIgnoreCase) && k.EndsWith(":repo", StringComparison.OrdinalIgnoreCase))
            .Select(k =>
            {
                // Extract name from "templates:indexes:<name>:repo"
                var parts = k.Split(':');
                return parts.Length >= 4 ? parts[2] : null;
            })
            .Where(n => n is not null && !string.Equals(n, "default", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var indexName in indexNames)
        {
            allConfig.TryGetValue($"templates:indexes:{indexName}:repo", out var repo);
            allConfig.TryGetValue($"templates:indexes:{indexName}:ref", out var gitRef);

            if (repo is not null)
            {
                sources.Add(new GitTemplateSource
                {
                    Name = indexName!,
                    Repo = repo,
                    Ref = gitRef,
                    Kind = GitTemplateSourceKind.Configured
                });
            }
        }

        return sources;
    }

    /// <summary>
    /// Converts a GitHub repo URL to a raw content URL for a file.
    /// </summary>
    internal static string? BuildRawUrl(string repoUrl, string gitRef, string filePath)
    {
        // Handle https://github.com/owner/repo or https://github.com/owner/repo.git
        if (repoUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
        {
            var path = repoUrl["https://github.com/".Length..].TrimEnd('/');
            if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                path = path[..^4];
            }

            return $"https://raw.githubusercontent.com/{path}/{gitRef}/{filePath}";
        }

        return null;
    }
}
