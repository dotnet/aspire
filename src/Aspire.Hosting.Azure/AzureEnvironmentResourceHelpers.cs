// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECONTAINERRUNTIME001
#pragma warning disable ASPIREAZURE001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Helper methods for Azure environment resources to handle container image operations.
/// </summary>
internal static class AzureEnvironmentResourceHelpers
{
    public static async Task LoginToRegistryAsync(IContainerRegistry registry, PipelineStepContext context)
    {
        var acrLoginService = context.Services.GetRequiredService<IAcrLoginService>();
        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

        // Find the AzureEnvironmentResource from the application model
        var azureEnvironment = context.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
        if (azureEnvironment == null)
        {
            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
        }

        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");
        
        var registryEndpoint = await registry.Endpoint.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                              throw new InvalidOperationException("Failed to retrieve container registry endpoint.");

        var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (loginTask.ConfigureAwait(false))
        {
            try
            {
                // Get tenant ID from the provisioning context (always available from subscription)
                var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);
                var tenantId = provisioningContext.Tenant.TenantId?.ToString()
                    ?? throw new InvalidOperationException("Tenant ID is required for ACR authentication but was not available in provisioning context.");

                // Use the ACR login service to perform authentication
                await acrLoginService.LoginAsync(
                    registryEndpoint,
                    tenantId,
                    tokenCredentialProvider.TokenCredential,
                    context.CancellationToken).ConfigureAwait(false);

                await loginTask.CompleteAsync($"Successfully logged in to **{registryEndpoint}**", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await loginTask.FailAsync($"Login to ACR **{registryEndpoint}** failed: {ex.Message}", cancellationToken: context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
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

    public static async Task<(string? HostName, bool IsAvailable)> GetDnlHostNameAsync(IResource resource, PipelineStepContext context)
    {
        // Get required services
        var httpClientFactory = context.Services.GetService<IHttpClientFactory>();

        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
        }

        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

        // Find the AzureEnvironmentResource from the application model
        var azureEnvironment = context.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
        if (azureEnvironment == null)
        {
            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
        }

        var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);
        var subscriptionId = provisioningContext.Subscription.Id.SubscriptionId?.ToString()
            ?? throw new InvalidOperationException("SubscriptionId is required.");
        var resourceGroup = provisioningContext.ResourceGroup.Name
            ?? throw new InvalidOperationException("ResourceGroup name is required.");

        // Prepare ARM endpoint and request
        var armEndpoint = "https://management.azure.com";
        var apiVersion = "2022-03-01";
        var siteName = resource.Name.ToLowerInvariant();
        var url = $"{armEndpoint}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/CheckNameAvailability?api-version={apiVersion}";

        var requestBody = new
        {
            name = siteName,
            type = "Microsoft.Web/sites"
        };

        var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
        // Get access token for ARM
        var token = await tokenCredentialProvider.TokenCredential
            .GetTokenAsync(tokenRequest, context.CancellationToken)
            .ConfigureAwait(false);

        var httpClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await httpClient.SendAsync(request, context.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(context.CancellationToken).ConfigureAwait(false);
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(responseStream, cancellationToken: context.CancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var isAvailable = root.GetProperty("nameAvailable").GetBoolean();
        var hostName = root.GetProperty("hostName").GetString();

        return (hostName, isAvailable);
    }

    private static async Task TagAndPushImage(string localTag, string targetTag, CancellationToken cancellationToken, IResourceContainerImageBuilder containerImageBuilder)
    {
        await containerImageBuilder.TagImageAsync(localTag, targetTag, cancellationToken).ConfigureAwait(false);
        await containerImageBuilder.PushImageAsync(targetTag, cancellationToken).ConfigureAwait(false);
    }
}
