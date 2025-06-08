// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class BicepProvisioner(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IBicepCliExecutor bicepCliExecutor,
    ISecretClientProvider secretClientProvider)
{
    public static bool ShouldProvision(AzureBicepResource resource)
        => !resource.IsContainer();

    public async Task<bool> ConfigureResourceAsync(IConfiguration configuration, AzureBicepResource resource, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection($"Azure:Deployments:{resource.Name}");

        if (!section.Exists())
        {
            return false;
        }

        var currentCheckSum = await BicepUtilities.GetCurrentChecksumAsync(resource, section, cancellationToken).ConfigureAwait(false);
        var configCheckSum = section["CheckSum"];

        if (currentCheckSum != configCheckSum)
        {
            return false;
        }

        if (section["Outputs"] is string outputJson)
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

        if (section["Id"] is string deploymentId &&
            ResourceIdentifier.TryParse(deploymentId, out var id) &&
            id is not null)
        {
            portalUrls.Add(new(Name: "deployment", Url: GetDeploymentUrl(id), IsInternal: false));
        }

        await notificationService.PublishUpdateAsync(resource, state =>
        {
            ImmutableArray<ResourcePropertySnapshot> props = [
                .. state.Properties,
                    new("azure.subscription.id", configuration["Azure:SubscriptionId"]),
                    // new("azure.resource.group", configuration["Azure:ResourceGroup"]!),
                    new("azure.tenant.domain", configuration["Azure:Tenant"]),
                    new("azure.location", configuration["Azure:Location"]),
                    new(CustomResourceKnownProperties.Source, section["Id"])
            ];

            return state with
            {
                State = new("Provisioned", KnownResourceStateStyles.Success),
                Urls = [.. portalUrls],
                Properties = props
            };
        }).ConfigureAwait(false);

        return true;
    }

    public async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        var resourceGroup = context.ResourceGroup;
        var resourceLogger = loggerService.GetLogger(resource);

        if (BicepUtilities.GetExistingResourceGroup(resource) is { } existingResourceGroup)
        {
            var existingResourceGroupName = existingResourceGroup is ParameterResource parameterResource
                ? parameterResource.Value
                : (string)existingResourceGroup;
            resourceGroup = await context.Subscription.GetResourceGroupAsync(existingResourceGroupName, cancellationToken).ConfigureAwait(false);
        }

        await notificationService.PublishUpdateAsync(resource, state => state with
        {
            ResourceType = resource.GetType().Name,
            State = new("Starting", KnownResourceStateStyles.Info),
            Properties = state.Properties.SetResourcePropertyRange([
                new("azure.subscription.id", context.Subscription.Id.Name),
                new("azure.resource.group", resourceGroup.Id.Name),
                new("azure.tenant.domain", context.Tenant.Data.DefaultDomain),
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

        var armTemplateContents = await bicepCliExecutor.CompileBicepToArmAsync(path, cancellationToken).ConfigureAwait(false);

        var deployments = resourceGroup.GetArmDeployments();

        resourceLogger.LogInformation("Deploying {Name} to {ResourceGroup}", resource.Name, resourceGroup.Data.Name);

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

        var operation = await deployments.CreateOrUpdateAsync(WaitUntil.Started, resource.Name, new ArmDeploymentContent(new(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(armTemplateContents),
            Parameters = BinaryData.FromObjectAsJson(parameters),
            DebugSettingDetailLevel = "ResponseContent"
        }),
        cancellationToken).ConfigureAwait(false);

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
        resourceLogger.LogInformation("Deployment of {Name} to {ResourceGroup} took {Elapsed}", resource.Name, resourceGroup.Data.Name, sw.Elapsed);

        var deployment = operation.Value;

        var outputs = deployment.Data.Properties.Outputs;

        if (deployment.Data.Properties.ProvisioningState == ResourcesProvisioningState.Succeeded)
        {
            template.Dispose();
        }
        else
        {
            throw new InvalidOperationException($"Deployment of {resource.Name} to {resourceGroup.Data.Name} failed with {deployment.Data.Properties.ProvisioningState}");
        }

        // e.g. {  "sqlServerName": { "type": "String", "value": "<value>" }}

        var outputObj = outputs?.ToObjectFromJson<JsonObject>();

        var az = context.UserSecrets.Prop("Azure");
        az["Tenant"] = context.Tenant.Data.DefaultDomain;

        var resourceConfig = context.UserSecrets
            .Prop("Azure")
            .Prop("Deployments")
            .Prop(resource.Name);

        // Clear the entire section
        resourceConfig.AsObject().Clear();

        // Save the deployment id to the configuration
        resourceConfig["Id"] = deployment.Id.ToString();

        // Stash all parameters as a single JSON string
        resourceConfig["Parameters"] = parameters.ToJsonString();

        if (outputObj is not null)
        {
            // Same for outputs
            resourceConfig["Outputs"] = outputObj.ToJsonString();
        }

        // Write resource scope to config for consistent checksums
        if (scope is not null)
        {
            resourceConfig["Scope"] = scope.ToJsonString();
        }

        // Save the checksum to the configuration
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

        var subId = provisioningContext.Subscription.Data.Id.ToString();
        var rgName = resourceGroup.Data.Name;
        var subAndRg = $"{subId}/resourceGroups/{rgName}";

        var deployId = deploymentName;

        var path = $"{subAndRg}/providers/Microsoft.Resources/deployments/{deployId}";
        var encodedPath = Uri.EscapeDataString(path);

        return $"{prefix}/{encodedPath}";
    }

    public static string GetDeploymentUrl(ResourceIdentifier deploymentId) =>
        $"{PortalDeploymentOverviewUrl}/{Uri.EscapeDataString(deploymentId.ToString())}";
}
