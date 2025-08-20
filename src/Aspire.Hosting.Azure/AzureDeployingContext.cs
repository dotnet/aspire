// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Publishing;
using Azure;

namespace Aspire.Hosting.Azure;

internal sealed class AzureDeployingContext(
    IProvisioningContextProvider provisioningContextProvider,
    IUserSecretsManager userSecretsManager,
    IBicepProvisioner bicepProvisioner,
    IPublishingActivityReporter activityReporter)
{
    public async Task DeployModelAsync(AzureEnvironmentResource resource, CancellationToken cancellationToken = default)
    {
        var userSecrets = await userSecretsManager.LoadUserSecretsAsync(cancellationToken).ConfigureAwait(false);
        var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(userSecrets, cancellationToken).ConfigureAwait(false);

        if (resource.PublishingContext is null)
        {
            throw new InvalidOperationException($"Publishing context is not initialized. Please ensure that the {nameof(AzurePublishingContext)} has been initialized before deploying.");
        }

        var deployingStep = await activityReporter.CreateStepAsync("Deploying to Azure", cancellationToken).ConfigureAwait(false);
        await using (deployingStep.ConfigureAwait(false))
        {
            foreach (var (parameterResource, provisioningParameter) in resource.PublishingContext.ParameterLookup)
            {
                if (parameterResource == resource.Location)
                {
                    resource.Parameters[provisioningParameter.BicepIdentifier] = provisioningContext.Location.Name;
                }
                else if (parameterResource == resource.ResourceGroupName)
                {
                    resource.Parameters[provisioningParameter.BicepIdentifier] = provisioningContext.ResourceGroup.Name;
                }
                else if (parameterResource == resource.PrincipalId)
                {
                    resource.Parameters[provisioningParameter.BicepIdentifier] = provisioningContext.Principal.Id.ToString();
                }
                else
                {
                    // TODO: Prompt here.
                    await deployingStep.FailAsync("Deployment contains unresolvable parameters.", cancellationToken).ConfigureAwait(false);
                }
            }

            try
            {
                var azureTask = await deployingStep.CreateTaskAsync("Provisioning Azure environment", cancellationToken).ConfigureAwait(false);
                await using (azureTask.ConfigureAwait(false))
                {
                    await bicepProvisioner.GetOrCreateResourceAsync(resource, provisioningContext, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex switch
                {
                    RequestFailedException requestEx => $"Deployment failed: {ExtractDetailedErrorMessage(requestEx)}",
                    _ => $"Deployment failed: {ex.Message}"
                };

                await deployingStep.FailAsync(errorMessage, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
    }

    /// <summary>
    /// Extracts detailed error information from Azure RequestFailedException responses.
    /// Parses the following JSON error structures:
    /// 1. Standard Azure error format: { "error": { "code": "...", "message": "...", "details": [...] } }
    /// 2. Deployment-specific error format: { "properties": { "error": { "code": "...", "message": "..." } } }
    /// 3. Nested error details with recursive parsing for deeply nested error hierarchies
    /// </summary>
    /// <param name="requestEx">The Azure RequestFailedException containing the error response</param>
    /// <returns>The most specific error message found, or the original exception message if parsing fails</returns>
    private static string ExtractDetailedErrorMessage(RequestFailedException requestEx)
    {
        try
        {
            var response = requestEx.GetRawResponse();
            if (response?.Content is not null)
            {
                var responseContent = response.Content.ToString();
                if (!string.IsNullOrEmpty(responseContent))
                {
                    if (JsonNode.Parse(responseContent) is JsonObject responseObj)
                    {
                        if (responseObj["error"] is JsonObject errorObj)
                        {
                            var code = errorObj["code"]?.ToString();
                            var message = errorObj["message"]?.ToString();

                            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(message))
                            {
                                if (errorObj["details"] is JsonArray detailsArray && detailsArray.Count > 0)
                                {
                                    var deepestErrorMessage = ExtractDeepestErrorMessage(detailsArray);
                                    if (!string.IsNullOrEmpty(deepestErrorMessage))
                                    {
                                        return deepestErrorMessage;
                                    }
                                }

                                return $"{code}: {message}";
                            }
                        }

                        if (responseObj["properties"]?["error"] is JsonObject deploymentErrorObj)
                        {
                            var code = deploymentErrorObj["code"]?.ToString();
                            var message = deploymentErrorObj["message"]?.ToString();

                            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(message))
                            {
                                return $"{code}: {message}";
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException) { }

        return requestEx.Message;
    }

    private static string ExtractDeepestErrorMessage(JsonArray detailsArray)
    {
        foreach (var detail in detailsArray)
        {
            if (detail is JsonObject detailObj)
            {
                var detailCode = detailObj["code"]?.ToString();
                var detailMessage = detailObj["message"]?.ToString();

                if (detailObj["details"] is JsonArray nestedDetailsArray && nestedDetailsArray.Count > 0)
                {
                    var deeperMessage = ExtractDeepestErrorMessage(nestedDetailsArray);
                    if (!string.IsNullOrEmpty(deeperMessage))
                    {
                        return deeperMessage;
                    }
                }

                if (!string.IsNullOrEmpty(detailCode) && !string.IsNullOrEmpty(detailMessage))
                {
                    return $"{detailCode}: {detailMessage}";
                }
            }
        }

        return string.Empty;
    }
}
