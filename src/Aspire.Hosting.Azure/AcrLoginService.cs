// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECONTAINERRUNTIME001

using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IContainerRuntime _containerRuntime;
    private readonly ILogger<AcrLoginService> _logger;

    private sealed class AcrRefreshTokenResponse
    {
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcrLoginService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for making OAuth2 exchange requests.</param>
    /// <param name="containerRuntime">The container runtime for performing registry login.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public AcrLoginService(IHttpClientFactory httpClientFactory, IContainerRuntime containerRuntime, ILogger<AcrLoginService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _containerRuntime = containerRuntime ?? throw new ArgumentNullException(nameof(containerRuntime));
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

        // Step 1: Acquire AAD access token for ACR audience
        var tokenRequestContext = new TokenRequestContext([AcrScope]);
        var aadToken = await credential.GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("AAD access token acquired for ACR audience, registry: {RegistryEndpoint}, token length: {TokenLength}",
            registryEndpoint, aadToken.Token.Length);

        // Step 2: Exchange AAD token for ACR refresh token
        var refreshToken = await ExchangeAadTokenForAcrRefreshTokenAsync(
            registryEndpoint, tenantId, aadToken.Token, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("ACR refresh token acquired, length: {TokenLength}", refreshToken.Length);

        // Step 3: Login to the registry using container runtime
        await _containerRuntime.LoginToRegistryAsync(registryEndpoint, AcrUsername, refreshToken, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ExchangeAadTokenForAcrRefreshTokenAsync(
        string registryEndpoint,
        string tenantId,
        string aadAccessToken,
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
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

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var truncatedBody = responseBody.Length <= 1000 ? responseBody : responseBody[..1000] + "…";
            throw new HttpRequestException(
                $"POST /oauth2/exchange failed {(int)response.StatusCode} {response.ReasonPhrase}. Body: {truncatedBody}",
                null,
                response.StatusCode);
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<AcrRefreshTokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(tokenResponse?.RefreshToken))
        {
            var truncatedBody = responseBody.Length <= 1000 ? responseBody : responseBody[..1000] + "…";
            throw new InvalidOperationException($"Response missing refresh_token. Body: {truncatedBody}");
        }

        return tokenResponse.RefreshToken;
    }
}
