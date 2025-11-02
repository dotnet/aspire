// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECONTAINERRUNTIME001

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Helper methods for Azure environment resources to handle container image operations.
/// </summary>
internal static class AzureEnvironmentResourceHelpers
{
    private const string AcrUsername = "00000000-0000-0000-0000-000000000000";
    private const string AcrScope = "https://containerregistry.azure.net/.default";

    private sealed class AcrRefreshTokenResponse
    {
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    public static async Task LoginToRegistryAsync(IContainerRegistry registry, PipelineStepContext context)
    {
        var containerRuntime = context.Services.GetRequiredService<IContainerRuntime>();
        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();
        var options = context.Services.GetService<Microsoft.Extensions.Options.IOptions<AzureProvisionerOptions>>();

        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");
        
        var registryEndpoint = await registry.Endpoint.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                              throw new InvalidOperationException("Failed to retrieve container registry endpoint.");

        var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (loginTask.ConfigureAwait(false))
        {
            var tenantId = options?.Value.TenantId;
            await AuthenticateToAcrHelper(loginTask, registryEndpoint, tenantId, containerRuntime, tokenCredentialProvider.TokenCredential, context.Logger, context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AuthenticateToAcrHelper(IReportingTask loginTask, string registryEndpoint, string? tenantId, IContainerRuntime containerRuntime, TokenCredential credential, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Acquire AAD access token for ACR audience (https://containerregistry.azure.net/.default)
            // This is equivalent to: az acr login -n <name> --expose-token
            var tokenRequestContext = new TokenRequestContext([AcrScope]);
            var aadToken = await credential.GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);

            logger.LogDebug("AAD access token acquired for ACR audience, registry: {RegistryEndpoint}, token length: {TokenLength}", registryEndpoint, aadToken.Token.Length);

            // Step 2: Exchange AAD token for ACR refresh token
            // The refresh token is what docker login uses as the password
            var refreshToken = await ExchangeAadTokenForAcrRefreshTokenAsync(registryEndpoint, tenantId, aadToken.Token, logger, cancellationToken).ConfigureAwait(false);

            logger.LogDebug("ACR refresh token acquired, length: {TokenLength}", refreshToken.Length);

            // Step 3: Login to the registry using docker/podman with:
            //   - username: 00000000-0000-0000-0000-000000000000
            //   - password: ACR refresh token (via --password-stdin)
            await containerRuntime.LoginToRegistryAsync(registryEndpoint, AcrUsername, refreshToken, cancellationToken).ConfigureAwait(false);

            await loginTask.CompleteAsync($"Successfully logged in to **{registryEndpoint}**", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await loginTask.FailAsync($"Login to ACR **{registryEndpoint}** failed: {ex.Message}", cancellationToken: cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<string> ExchangeAadTokenForAcrRefreshTokenAsync(string registryEndpoint, string? tenantId, string aadAccessToken, ILogger logger, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        // ACR OAuth2 exchange endpoint
        var exchangeUrl = $"https://{registryEndpoint}/oauth2/exchange";
        
        logger.LogDebug("Exchanging AAD token for ACR refresh token at {ExchangeUrl}{TenantInfo}", 
            exchangeUrl, 
            string.IsNullOrEmpty(tenantId) ? "" : $" (tenant: {tenantId})");

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "access_token",
            ["service"] = registryEndpoint,
            ["access_token"] = aadAccessToken
        };

        // Include tenant if available (required for some ACR configurations)
        if (!string.IsNullOrEmpty(tenantId))
        {
            formData["tenant"] = tenantId;
        }

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

    public static async Task PushImageToRegistryAsync(IContainerRegistry registry, IResource resource, PipelineStepContext context, IResourceContainerImageBuilder containerImageBuilder)
    {
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");

        if (!resource.TryGetContainerImageName(out var localImageName))
        {
            localImageName = resource.Name.ToLowerInvariant();
        }

        IValueProvider cir = new ContainerImageReference(resource);
        var targetTag = await cir.GetValueAsync(context.CancellationToken).ConfigureAwait(false);

        var pushTask = await context.ReportingStep.CreateTaskAsync($"Pushing **{resource.Name}** to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (pushTask.ConfigureAwait(false))
        {
            try
            {
                if (targetTag == null)
                {
                    throw new InvalidOperationException($"Failed to get target tag for {resource.Name}");
                }
                await TagAndPushImage(localImageName, targetTag, context.CancellationToken, containerImageBuilder).ConfigureAwait(false);
                await pushTask.CompleteAsync($"Successfully pushed **{resource.Name}** to `{targetTag}`", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pushTask.CompleteAsync($"Failed to push **{resource.Name}**: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private static async Task TagAndPushImage(string localTag, string targetTag, CancellationToken cancellationToken, IResourceContainerImageBuilder containerImageBuilder)
    {
        await containerImageBuilder.TagImageAsync(localTag, targetTag, cancellationToken).ConfigureAwait(false);
        await containerImageBuilder.PushImageAsync(targetTag, cancellationToken).ConfigureAwait(false);
    }
}
