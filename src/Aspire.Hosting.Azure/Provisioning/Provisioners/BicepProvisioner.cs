// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DeploymentState;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class BicepProvisioner(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IBicepCompiler bicepCompiler,
    ISecretClientProvider secretClientProvider,
    DistributedApplicationExecutionContext executionContext) : IBicepProvisioner
{
    /// <inheritdoc />
    public async Task<bool> ConfigureResourceAsync(IDeploymentStateProvider deploymentStateProvider, AzureBicepResource resource, CancellationToken cancellationToken)
    {
        var state = await deploymentStateProvider.LoadAsync(cancellationToken).ConfigureAwait(false);
        var deploymentSection = state["Azure"]?["Deployments"]?[resource.Name]?.AsObject();

        if (deploymentSection is null)
        {
            return false;
        }

        var currentCheckSum = await BicepUtilities.GetCurrentChecksumAsync(resource, deploymentSection, cancellationToken).ConfigureAwait(false);
        var configCheckSum = deploymentSection["CheckSum"]?.ToString();

        if (currentCheckSum != configCheckSum)
        {
            return false;
        }

        if (deploymentSection["Outputs"]?.ToString() is string outputJson)
        {
            JsonNode? outputObj = null;
            try
            {
                outputObj = JsonNode.Parse(outputJson);

                if (outputObj is null)
                {
                    return false;
                }
            }
            catch
            {
                // Unable to parse the JSON, to treat it as not existing
                return false;
            }

            foreach (var item in outputObj.AsObject())
            {
                // TODO: Handle complex output types
                // Populate the resource outputs
                resource.Outputs[item.Key] = item.Value?.Prop("value").ToString();
            }
        }

        if (resource is IAzureKeyVaultResource kvr)
        {
            ConfigureSecretResolver(kvr);
        }

        var portalUrls = new List<UrlSnapshot>();

        if (deploymentSection["Id"]?.ToString() is string deploymentId &&
            ResourceIdentifier.TryParse(deploymentId, out var id) &&
            id is not null)
        {
            portalUrls.Add(new(Name: "deployment", Url: GetDeploymentUrl(id), IsInternal: false));
        }

        await notificationService.PublishUpdateAsync(resource, s =>
        {
            ImmutableArray<ResourcePropertySnapshot> props = [
                .. s.Properties,
                    new("azure.subscription.id", state["Azure"]?["SubscriptionId"]?.ToString()),
                    new("azure.tenant.domain", state["Azure"]?["Tenant"]?.ToString()),
                    new("azure.location", state["Azure"]?["Location"]?.ToString()),
                    new(CustomResourceKnownProperties.Source, deploymentSection["Id"]?.ToString())
            ];

            return s with
            {
                State = new("Provisioned", KnownResourceStateStyles.Success),
                Urls = [.. portalUrls],
                Properties = props
            };
        }).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        var resourceGroup = context.ResourceGroup;
        var resourceLogger = loggerService.GetLogger(resource);

        if (BicepUtilities.GetExistingResourceGroup(resource) is { } existingResourceGroup)
        {
            var existingResourceGroupName = existingResourceGroup is ParameterResource parameterResource
                ? (await parameterResource.GetValueAsync(cancellationToken).ConfigureAwait(false))!
                : (string)existingResourceGroup;
            var response = await context.Subscription.GetResourceGroups().GetAsync(existingResourceGroupName, cancellationToken).ConfigureAwait(false);
            resourceGroup = response.Value;
        }

        await notificationService.PublishUpdateAsync(resource, state => state with
        {
            ResourceType = resource.GetType().Name,
            State = new("Starting", KnownResourceStateStyles.Info),
            Properties = state.Properties.SetResourcePropertyRange([
                new("azure.subscription.id", context.Subscription.Id.Name),
                new("azure.resource.group", resourceGroup.Id.Name),
                new("azure.tenant.domain", context.Tenant.DefaultDomain),
                new("azure.location", context.Location.ToString()),
            ])
        }).ConfigureAwait(false);

        var template = resource.GetBicepTemplateFile();
        var path = template.Path;

        // GetBicepTemplateFile may have added new well-known parameters, so we need
        // to populate them only after calling GetBicepTemplateFile.
        PopulateWellKnownParameters(resource, context);

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            return state with
            {
                State = new("Compiling ARM template", KnownResourceStateStyles.Info)
            };
        })
        .ConfigureAwait(false);

        var armTemplateContents = await bicepCompiler.CompileBicepToArmAsync(path, cancellationToken).ConfigureAwait(false);

        // Convert the parameters to a JSON object
        var parameters = new JsonObject();
        await BicepUtilities.SetParametersAsync(parameters, resource, cancellationToken: cancellationToken).ConfigureAwait(false);

        var scope = new JsonObject();
        await BicepUtilities.SetScopeAsync(scope, resource, cancellationToken: cancellationToken).ConfigureAwait(false);

        var sw = Stopwatch.StartNew();

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            return state with
            {
                State = new("Creating ARM Deployment", KnownResourceStateStyles.Info)
            };
        })
        .ConfigureAwait(false);

        resourceLogger.LogInformation("Deploying {Name} to {ResourceGroup}", resource.Name, resourceGroup.Name);

        // Resources with a Subscription scope should use a subscription-level deployment.
        var deployments = resource.Scope?.Subscription != null
            ? context.Subscription.GetArmDeployments()
            : resourceGroup.GetArmDeployments();
        var deploymentName = executionContext.IsPublishMode ? $"{resource.Name}-{DateTimeOffset.Now.ToUnixTimeSeconds()}" : resource.Name;

        var deploymentContent = new ArmDeploymentContent(new(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(armTemplateContents),
            Parameters = BinaryData.FromObjectAsJson(parameters),
            DebugSettingDetailLevel = "ResponseContent"
        });
        var operation = await deployments.CreateOrUpdateAsync(WaitUntil.Started, deploymentName, deploymentContent, cancellationToken).ConfigureAwait(false);

        // Resolve the deployment URL before waiting for the operation to complete
        var url = GetDeploymentUrl(context, resourceGroup, resource.Name);

        resourceLogger.LogInformation("Deployment started: {Url}", url);

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            return state with
            {
                State = new("Waiting for Deployment", KnownResourceStateStyles.Info),
                Urls = [.. state.Urls, new(Name: "deployment", Url: url, IsInternal: false)],
            };
        })
        .ConfigureAwait(false);

        await operation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);

        sw.Stop();
        resourceLogger.LogInformation("Deployment of {Name} to {ResourceGroup} took {Elapsed}", resource.Name, resourceGroup.Name, sw.Elapsed);

        var deployment = operation.Value;

        var outputs = deployment.Data.Properties.Outputs;

        if (deployment.Data.Properties.ProvisioningState == ResourcesProvisioningState.Succeeded)
        {
            if (context.ExecutionContext.IsRunMode)
            {
                template.Dispose();
            }
        }
        else
        {
            throw new InvalidOperationException($"Deployment of {resource.Name} to {resourceGroup.Name} failed with {deployment.Data.Properties.ProvisioningState}");
        }

        // e.g. {  "sqlServerName": { "type": "String", "value": "<value>" }}
        var outputObj = outputs?.ToObjectFromJson<JsonObject>();

        var state = await context.DeploymentStateProvider.LoadAsync(cancellationToken).ConfigureAwait(false);
        var az = state.Prop("Azure");
        az["Tenant"] = context.Tenant.DefaultDomain;

        var resourceConfig = state
            .Prop("Azure")
            .Prop("Deployments")
            .Prop(resource.Name);

        resourceConfig.AsObject().Clear();

        resourceConfig["Id"] = deployment.Id.ToString();

        resourceConfig["Parameters"] = parameters.ToJsonString();

        if (outputObj is not null)
        {
            resourceConfig["Outputs"] = outputObj.ToJsonString();
        }

        if (scope is not null)
        {
            resourceConfig["Scope"] = scope.ToJsonString();
        }

        resourceConfig["CheckSum"] = BicepUtilities.GetChecksum(resource, parameters, scope);

        if (outputObj is not null)
        {
            foreach (var item in outputObj.AsObject())
            {
                // TODO: Handle complex output types
                // Populate the resource outputs
                resource.Outputs[item.Key] = item.Value?.Prop("value").ToString();
            }
        }

        // Populate secret outputs from key vault (if any)
        if (resource is IAzureKeyVaultResource kvr)
        {
            ConfigureSecretResolver(kvr);
        }

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            ImmutableArray<ResourcePropertySnapshot> properties = [
                .. state.Properties,
                new(CustomResourceKnownProperties.Source, deployment.Id.Name)
            ];

            return state with
            {
                State = new("Provisioned", KnownResourceStateStyles.Success),
                CreationTimeStamp = DateTime.UtcNow,
                Properties = properties
            };
        })
        .ConfigureAwait(false);

        await context.DeploymentStateProvider.SaveAsync(state, cancellationToken).ConfigureAwait(false);
    }

    private void ConfigureSecretResolver(IAzureKeyVaultResource kvr)
    {
        var resource = (AzureBicepResource)kvr;

        var vaultUri = resource.Outputs[kvr.VaultUriOutputReference.Name] as string ?? throw new InvalidOperationException($"{kvr.VaultUriOutputReference.Name} not found in outputs.");

        // Set the client for resolving secrets at runtime
        var client = secretClientProvider.GetSecretClient(new(vaultUri));
        kvr.SecretResolver = async (secretRef, ct) =>
        {
            var secret = await client.GetSecretAsync(secretRef.SecretName, cancellationToken: ct).ConfigureAwait(false);
            return secret.Value.Value;
        };
    }

    private static void PopulateWellKnownParameters(AzureBicepResource resource, ProvisioningContext context)
    {
        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalId, out var principalId) && principalId is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalId] = context.Principal.Id;
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalName, out var principalName) && principalName is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalName] = context.Principal.Name;
        }

        if (resource.Parameters.TryGetValue(AzureBicepResource.KnownParameters.PrincipalType, out var principalType) && principalType is null)
        {
            resource.Parameters[AzureBicepResource.KnownParameters.PrincipalType] = "User";
        }

        // Always specify the location
        resource.Parameters[AzureBicepResource.KnownParameters.Location] = context.Location.Name;
    }

    private const string PortalDeploymentOverviewUrl = "https://portal.azure.com/#view/HubsExtension/DeploymentDetailsBlade/~/overview/id";

    private static string GetDeploymentUrl(ProvisioningContext provisioningContext, IResourceGroupResource resourceGroup, string deploymentName)
    {
        var prefix = PortalDeploymentOverviewUrl;

        var subId = provisioningContext.Subscription.Id.ToString();
        var rgName = resourceGroup.Name;
        var subAndRg = $"{subId}/resourceGroups/{rgName}";

        var deployId = deploymentName;

        var path = $"{subAndRg}/providers/Microsoft.Resources/deployments/{deployId}";
        var encodedPath = Uri.EscapeDataString(path);

        return $"{prefix}/{encodedPath}";
    }

    public static string GetDeploymentUrl(ResourceIdentifier deploymentId) =>
        $"{PortalDeploymentOverviewUrl}/{Uri.EscapeDataString(deploymentId.ToString())}";
}
