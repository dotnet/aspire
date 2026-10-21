// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Documentation.Docs;

/// <summary>
/// Cache for aspire.dev documentation content with ETag support.
/// Uses both in-memory cache for fast access and disk cache for persistence across CLI invocations.
/// </summary>
internal sealed class DocsCache : IDocsCache
{
    private const string DocsCacheSubdirectory = "docs";

    private readonly FileBackedDocumentContentCache _contentCache;
    private readonly IConfiguration _configuration;

    // Resolve the source and its cache identities only after project configuration is applied.
    private string LlmsTxtUrl => field ??= DocsSourceConfiguration.GetLlmsTxtUrl(_configuration);
    private string SourceCacheKey => field ??= DocsSourceConfiguration.GetContentCacheKey(LlmsTxtUrl);
    private string IndexCacheKey => field ??= DocsSourceConfiguration.GetIndexCacheKey(LlmsTxtUrl);
    private string IndexSourceFingerprintCacheKey => field ??= $"{IndexCacheKey}:fingerprint";
    private string LegacyIndexCacheKey => field ??= DocsSourceConfiguration.GetLegacyIndexCacheKey(LlmsTxtUrl);
    private string LegacyIndexSourceFingerprintCacheKey => field ??= $"{LegacyIndexCacheKey}:fingerprint";

    public DocsCache(
        IMemoryCache memoryCache,
        CliExecutionContext executionContext,
        IConfiguration configuration,
        ILogger<DocsCache> logger)
    {
        _configuration = configuration;
        _contentCache = new FileBackedDocumentContentCache(memoryCache, executionContext, DocsCacheSubdirectory, logger);
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => _contentCache.GetAsync(key, cancellationToken);

    public Task SetAsync(string key, string content, CancellationToken cancellationToken = default)
        => _contentCache.SetAsync(key, content, cancellationToken);

    public Task<string?> GetETagAsync(string url, CancellationToken cancellationToken = default)
        => _contentCache.GetETagAsync(url, cancellationToken);

    public Task SetETagAsync(string url, string? etag, CancellationToken cancellationToken = default)
        => _contentCache.SetETagAsync(url, etag, cancellationToken);

    public Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
        => _contentCache.InvalidateAsync(key, cancellationToken);

    public async Task<LlmsDocument[]?> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _contentCache.GetJsonAsync(IndexCacheKey, JsonSourceGenerationContext.Default.LlmsDocumentArray, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (documents is not null)
        {
            await MigrateLegacyIndexFingerprintAsync(cancellationToken).ConfigureAwait(false);
            await ClearLegacyCacheAsync(cancellationToken).ConfigureAwait(false);
            return documents;
        }

        if (!HasLegacyIndexCacheKey)
        {
            return null;
        }

        var legacyDocuments = await _contentCache.GetJsonAsync(LegacyIndexCacheKey, JsonSourceGenerationContext.Default.LlmsDocumentArray, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (legacyDocuments is null)
        {
            return null;
        }

        await _contentCache.SetJsonAsync(IndexCacheKey, legacyDocuments, JsonSourceGenerationContext.Default.LlmsDocumentArray, cancellationToken).ConfigureAwait(false);
        await MigrateLegacyIndexFingerprintAsync(cancellationToken).ConfigureAwait(false);
        await ClearLegacyCacheAsync(cancellationToken).ConfigureAwait(false);
        return legacyDocuments;
    }

    public async Task SetIndexAsync(LlmsDocument[] documents, CancellationToken cancellationToken = default)
    {
        await _contentCache.SetJsonAsync(IndexCacheKey, documents, JsonSourceGenerationContext.Default.LlmsDocumentArray, cancellationToken).ConfigureAwait(false);
        await ClearLegacyCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetIndexSourceFingerprintAsync(CancellationToken cancellationToken = default)
    {
        var fingerprint = await _contentCache.GetAsync(IndexSourceFingerprintCacheKey, cancellationToken).ConfigureAwait(false);
        if (fingerprint is not null)
        {
            await ClearLegacyIndexCacheAsync(cancellationToken).ConfigureAwait(false);
            return fingerprint;
        }

        if (!HasLegacyIndexCacheKey)
        {
            return null;
        }

        var legacyFingerprint = await _contentCache.GetAsync(LegacyIndexSourceFingerprintCacheKey, cancellationToken).ConfigureAwait(false);
        if (legacyFingerprint is null)
        {
            return null;
        }

        await _contentCache.SetAsync(IndexSourceFingerprintCacheKey, legacyFingerprint, cancellationToken).ConfigureAwait(false);
        await ClearLegacyIndexCacheAsync(cancellationToken).ConfigureAwait(false);
        return legacyFingerprint;
    }

    public async Task SetIndexSourceFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
    {
        await _contentCache.SetAsync(IndexSourceFingerprintCacheKey, fingerprint, cancellationToken).ConfigureAwait(false);
        await ClearLegacyIndexCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    private bool HasLegacyIndexCacheKey => !string.Equals(LegacyIndexCacheKey, IndexCacheKey, StringComparison.Ordinal);

    private async Task MigrateLegacyIndexFingerprintAsync(CancellationToken cancellationToken)
    {
        if (!HasLegacyIndexCacheKey)
        {
            return;
        }

        var currentFingerprint = await _contentCache.GetAsync(IndexSourceFingerprintCacheKey, cancellationToken).ConfigureAwait(false);
        if (currentFingerprint is not null)
        {
            return;
        }

        var legacyFingerprint = await _contentCache.GetAsync(LegacyIndexSourceFingerprintCacheKey, cancellationToken).ConfigureAwait(false);
        if (legacyFingerprint is not null)
        {
            await _contentCache.SetAsync(IndexSourceFingerprintCacheKey, legacyFingerprint, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ClearLegacyCacheAsync(CancellationToken cancellationToken)
    {
        await ClearLegacySourceCacheAsync(cancellationToken).ConfigureAwait(false);
        await ClearLegacyIndexCacheAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ClearLegacySourceCacheAsync(CancellationToken cancellationToken)
    {
        if (string.Equals(SourceCacheKey, LlmsTxtUrl, StringComparison.Ordinal))
        {
            return;
        }

        await _contentCache.InvalidateAsync(LlmsTxtUrl, cancellationToken).ConfigureAwait(false);
        await _contentCache.SetETagAsync(LlmsTxtUrl, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task ClearLegacyIndexCacheAsync(CancellationToken cancellationToken)
    {
        if (!HasLegacyIndexCacheKey)
        {
            return;
        }

        await _contentCache.InvalidateJsonAsync(LegacyIndexCacheKey, cancellationToken).ConfigureAwait(false);
        await _contentCache.InvalidateAsync(LegacyIndexSourceFingerprintCacheKey, cancellationToken).ConfigureAwait(false);
    }
}
