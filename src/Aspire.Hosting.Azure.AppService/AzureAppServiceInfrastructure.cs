// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.AppService;

internal sealed class AzureAppServiceInfrastructure(
    ILogger<AzureAppServiceInfrastructure> logger,
    IOptions<AzureProvisioningOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext) :
    IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (!executionContext.IsPublishMode)
        {
            return;
        }

        var appServiceEnvironment = appModel.Resources.OfType<AzureAppServiceEnvironmentResource>().FirstOrDefault()
            ?? throw new InvalidOperationException("AppServiceEnvironmentResource not found.");

        var appServiceEnvironmentContext = new AzureAppServiceEnvironmentContext(
            logger,
            executionContext, 
            appServiceEnvironment);

        foreach (var resource in appModel.Resources)
        {
            if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            // We only support project resources for now.
            if (resource is not ProjectResource)
            {
                continue;
            }

            var website = await appServiceEnvironmentContext.CreateAppServiceAsync(resource, provisioningOptions.Value, cancellationToken).ConfigureAwait(false);

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            resource.Annotations.Add(new DeploymentTargetAnnotation(website)
            {
                ContainerRegistry = appServiceEnvironment,
                ComputeEnvironment = appServiceEnvironment
            });
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        }

        static void SetKnownParameterValue(AzureBicepResource r, string key, Func<AzureBicepResource, object> factory)
        {
            if (r.Parameters.TryGetValue(key, out var existingValue) && existingValue is null)
            {
                var value = factory(r);

                r.Parameters[key] = value;
            }
        }

        // Resolve the known parameters for the container app environment
        foreach (var r in appModel.Resources.OfType<AzureBicepResource>())
        {
            // HACK: This forces parameters to be resolved for any AzureProvisioningResource
            r.GetBicepTemplateFile();

            // REVIEW: the secret key vault resources aren't coupled to the container app environment. This
            // is a side effect from how azd worked. We can move them to another service in the future.
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.KeyVaultName, _ => throw new NotSupportedException("Key vault name is not supported."));

            // Set the known parameters for the container app environment
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.PrincipalId, _ => appServiceEnvironment.ContainerRegistryManagedIdentityId);
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.PrincipalType, _ => "ServicePrincipal");
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.PrincipalName, _ => throw new NotSupportedException("principalName is not supported."));
            SetKnownParameterValue(r, AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId, _ => throw new NotSupportedException("logAnalyticsWorkspaceId is not supported."));

            SetKnownParameterValue(r, "containerAppEnvironmentId", _ => throw new NotSupportedException("containerAppEnvironmentId name is not supported."));
            SetKnownParameterValue(r, "containerAppEnvironmentName", _ => throw new NotSupportedException("containerAppEnvironmentNamee is not supported."));
        }
    }
}
