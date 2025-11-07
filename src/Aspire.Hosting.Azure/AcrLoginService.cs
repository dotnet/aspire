// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECONTAINERRUNTIME001
#pragma warning disable ASPIREPIPELINES002

using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Default implementation of <see cref="IAcrLoginService"/> that handles ACR authentication
/// using Azure credentials and OAuth2 token exchange.
/// </summary>
internal sealed class AcrLoginService : IAcrLoginService
{
    private const string AcrUsername = "00000000-0000-0000-0000-000000000000";
    private const string AcrScope = "https://containerregistry.azure.net/.default";
    private const string AcrTokensSectionName = "AcrTokens";
    // Safety margin to account for clock skew and network latency (5 minutes)
    private static readonly TimeSpan s_tokenExpirationSafetyMargin = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IContainerRuntime _containerRuntime;
    private readonly IDeploymentStateManager _deploymentStateManager;
    private readonly ILogger<AcrLoginService> _logger;

    private sealed class AcrRefreshTokenResponse
    {
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    private sealed class CachedToken
    {
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_at_utc")]
        public DateTime ExpiresAtUtc { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcrLoginService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for making OAuth2 exchange requests.</param>
    /// <param name="containerRuntime">The container runtime for performing registry login.</param>
    /// <param name="deploymentStateManager">The deployment state manager for caching tokens.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public AcrLoginService(
        IHttpClientFactory httpClientFactory,
        IContainerRuntime containerRuntime,
        IDeploymentStateManager deploymentStateManager,
        ILogger<AcrLoginService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _containerRuntime = containerRuntime ?? throw new ArgumentNullException(nameof(containerRuntime));
        _deploymentStateManager = deploymentStateManager ?? throw new ArgumentNullException(nameof(deploymentStateManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task LoginAsync(
        string registryEndpoint,
        string tenantId,
        TokenCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registryEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(credential);

        // Check if we have a valid cached token
        var cacheKey = GetCacheKey(registryEndpoint, tenantId);
        var cachedToken = await TryGetCachedTokenAsync(cacheKey, cancellationToken).ConfigureAwait(false);

        if (cachedToken != null)
        {
            _logger.LogDebug("Using cached ACR refresh token for registry: {RegistryEndpoint}", registryEndpoint);
            
            // Login to the registry using the cached token
            await _containerRuntime.LoginToRegistryAsync(registryEndpoint, AcrUsername, cachedToken.RefreshToken!, cancellationToken).ConfigureAwait(false);
            return;
        }

        _logger.LogDebug("No valid cached token found, performing fresh login for registry: {RegistryEndpoint}", registryEndpoint);

        // Step 1: Acquire AAD access token for ACR audience
        var tokenRequestContext = new TokenRequestContext([AcrScope]);
        var aadToken = await credential.GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("AAD access token acquired for ACR audience, registry: {RegistryEndpoint}, token length: {TokenLength}",
            registryEndpoint, aadToken.Token.Length);

        // Step 2: Exchange AAD token for ACR refresh token
        var (refreshToken, expiresIn) = await ExchangeAadTokenForAcrRefreshTokenAsync(
            registryEndpoint, tenantId, aadToken.Token, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("ACR refresh token acquired, length: {TokenLength}, expires in: {ExpiresIn} seconds",
            refreshToken.Length, expiresIn);

        // Step 3: Cache the token
        await CacheTokenAsync(cacheKey, refreshToken, expiresIn, cancellationToken).ConfigureAwait(false);

        // Step 4: Login to the registry using container runtime
        await _containerRuntime.LoginToRegistryAsync(registryEndpoint, AcrUsername, refreshToken, cancellationToken).ConfigureAwait(false);
    }

    private static string GetCacheKey(string registryEndpoint, string tenantId)
    {
        // Create a cache key that uniquely identifies the registry and tenant combination
        return $"{registryEndpoint}_{tenantId}";
    }

    private async Task<CachedToken?> TryGetCachedTokenAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _deploymentStateManager.AcquireSectionAsync(AcrTokensSectionName, cancellationToken).ConfigureAwait(false);

            if (section.Data.TryGetPropertyValue(cacheKey, out var tokenNode))
            {
                var cachedToken = JsonSerializer.Deserialize<CachedToken>(tokenNode!.ToJsonString(), s_jsonOptions);
                
                if (cachedToken != null &&
                    !string.IsNullOrEmpty(cachedToken.RefreshToken) &&
                    cachedToken.ExpiresAtUtc > DateTime.UtcNow.Add(s_tokenExpirationSafetyMargin))
                {
                    return cachedToken;
                }

                _logger.LogDebug("Cached token for {CacheKey} is expired or invalid, expiration: {ExpiresAt}",
                    cacheKey, cachedToken?.ExpiresAtUtc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached token for {CacheKey}, will perform fresh login", cacheKey);
        }

        return null;
    }

    private async Task CacheTokenAsync(string cacheKey, string refreshToken, int expiresInSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var section = await _deploymentStateManager.AcquireSectionAsync(AcrTokensSectionName, cancellationToken).ConfigureAwait(false);

            var cachedToken = new CachedToken
            {
                RefreshToken = refreshToken,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds)
            };

            var tokenJson = JsonSerializer.Serialize(cachedToken, s_jsonOptions);
            section.Data[cacheKey] = System.Text.Json.Nodes.JsonNode.Parse(tokenJson);

            await _deploymentStateManager.SaveSectionAsync(section, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Cached ACR token for {CacheKey}, expires at: {ExpiresAt}", cacheKey, cachedToken.ExpiresAtUtc);
        }
        catch (Exception ex)
        {
            // Log but don't fail if caching fails - the login already succeeded
            _logger.LogWarning(ex, "Failed to cache token for {CacheKey}, token caching will be skipped", cacheKey);
        }
    }

    private async Task<(string refreshToken, int expiresIn)> ExchangeAadTokenForAcrRefreshTokenAsync(
        string registryEndpoint,
        string tenantId,
        string aadAccessToken,
        CancellationToken cancellationToken)
    {
        // Use named HTTP client "AcrLogin" which can be configured for debug-level logging
        // via configuration: "Logging": { "LogLevel": { "System.Net.Http.HttpClient.AcrLogin": "Debug" } }
        var httpClient = _httpClientFactory.CreateClient("AcrLogin");
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        // ACR OAuth2 exchange endpoint
        var exchangeUrl = $"https://{registryEndpoint}/oauth2/exchange";

        _logger.LogDebug("Exchanging AAD token for ACR refresh token at {ExchangeUrl} (tenant: {TenantId})",
            exchangeUrl,
            tenantId);

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "access_token",
            ["service"] = registryEndpoint,
            ["tenant"] = tenantId,
            ["access_token"] = aadAccessToken
        };

        using var content = new FormUrlEncodedContent(formData);
        var response = await httpClient.PostAsync(exchangeUrl, content, cancellationToken).ConfigureAwait(false);

        // Read response body as string once
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var truncatedBody = responseBody.Length <= 1000 ? responseBody : responseBody[..1000] + "…";
            throw new HttpRequestException(
                $"POST /oauth2/exchange failed {(int)response.StatusCode} {response.ReasonPhrase}. Body: {truncatedBody}",
                null,
                response.StatusCode);
        }

        // Deserialize from the string we already read
        var tokenResponse = JsonSerializer.Deserialize<AcrRefreshTokenResponse>(responseBody, s_jsonOptions);

        if (string.IsNullOrEmpty(tokenResponse?.RefreshToken))
        {
            throw new InvalidOperationException($"Response missing refresh_token.");
        }

        // Default to 3 hours (10800 seconds) if not provided by ACR
        var expiresIn = tokenResponse.ExpiresIn ?? 10800;

        return (tokenResponse.RefreshToken, expiresIn);
    }
}
