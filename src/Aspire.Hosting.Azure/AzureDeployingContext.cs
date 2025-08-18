// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Publishing;

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
            // Map parameters from the AzurePublishingContext
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
                    try
                    {
                        await bicepProvisioner.GetOrCreateResourceAsync(resource, provisioningContext, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await azureTask.FailAsync($"Provisioning failed: {ex.Message}", cancellationToken).ConfigureAwait(false);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                await deployingStep.FailAsync($"Deployment failed: {ex.Message}", cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }
}
