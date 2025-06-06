// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Hashing;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class BicepProvisioner(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    TokenCredentialHolder tokenCredentialHolder,
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

        var currentCheckSum = await GetCurrentChecksumAsync(resource, section, cancellationToken).ConfigureAwait(false);
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

        // Populate secret outputs from key vault (if any)
        foreach (var item in section.GetSection("SecretOutputs").GetChildren())
        {
            resource.SecretOutputs[item.Key] = item.Value;
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

    private static object? GetExistingResourceGroup(AzureBicepResource resource) =>
        resource.Scope?.ResourceGroup ??
            (resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingResource) ?
                existingResource.ResourceGroup :
                null);

    public async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
    {
        var resourceGroup = context.ResourceGroup;
        var resourceLogger = loggerService.GetLogger(resource);

        if (GetExistingResourceGroup(resource) is { } existingResourceGroup)
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
        await SetParametersAsync(parameters, resource, cancellationToken: cancellationToken).ConfigureAwait(false);
        var scope = new JsonObject();
        await SetScopeAsync(scope, resource, cancellationToken: cancellationToken).ConfigureAwait(false);

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
        resourceConfig["CheckSum"] = GetChecksum(resource, parameters, scope);

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
        var client = secretClientProvider.GetSecretClient(new(vaultUri), tokenCredentialHolder.Credential);
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

    internal static string GetChecksum(AzureBicepResource resource, JsonObject parameters, JsonObject? scope)
    {
        // TODO: PERF Inefficient

        // Combine the parameter values with the bicep template to create a unique value
        var input = parameters.ToJsonString() + resource.GetBicepTemplateString();
        if (scope is not null)
        {
            input += scope.ToJsonString();
        }

        // Hash the contents
        var hashedContents = Crc32.Hash(Encoding.UTF8.GetBytes(input));

        // Convert the hash to a string
        return Convert.ToHexString(hashedContents).ToLowerInvariant();
    }

    internal static async ValueTask<string?> GetCurrentChecksumAsync(AzureBicepResource resource, IConfiguration section, CancellationToken cancellationToken = default)
    {
        // Fill in parameters from configuration
        if (section["Parameters"] is not string jsonString)
        {
            return null;
        }

        try
        {
            var parameters = JsonNode.Parse(jsonString)?.AsObject();
            var scope = section["Scope"] is string scopeString
                ? JsonNode.Parse(scopeString)?.AsObject()
                : null;

            if (parameters is null)
            {
                return null;
            }

            // Now overwrite with live object values skipping known and generated values.
            // This is important because the provisioner will fill in the known values and
            // generated values would change every time, so they can't be part of the checksum.
            await SetParametersAsync(parameters, resource, skipDynamicValues: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (scope is not null)
            {
                await SetScopeAsync(scope, resource, cancellationToken).ConfigureAwait(false);
            }

            // Get the checksum of the new values
            return GetChecksum(resource, parameters, scope);
        }
        catch
        {
            // Unable to parse the JSON, to treat it as not existing
            return null;
        }
    }

    // Known values since they will be filled in by the provisioner
    private static readonly string[] s_knownParameterNames =
    [
        AzureBicepResource.KnownParameters.PrincipalName,
        AzureBicepResource.KnownParameters.PrincipalId,
        AzureBicepResource.KnownParameters.PrincipalType,
        AzureBicepResource.KnownParameters.Location,
    ];

    // Converts the parameters to a JSON object compatible with the ARM template
    internal static async Task SetParametersAsync(JsonObject parameters, AzureBicepResource resource, bool skipDynamicValues = false, CancellationToken cancellationToken = default)
    {
        // Convert the parameters to a JSON object
        foreach (var parameter in resource.Parameters)
        {
            if (skipDynamicValues &&
                (s_knownParameterNames.Contains(parameter.Key) || IsParameterWithGeneratedValue(parameter.Value)))
            {
                continue;
            }

            // Execute parameter values which are deferred.
            var parameterValue = parameter.Value is Func<object?> f ? f() : parameter.Value;

            parameters[parameter.Key] = new JsonObject()
            {
                ["value"] = parameterValue switch
                {
                    string s => s,
                    IEnumerable<string> s => new JsonArray(s.Select(s => JsonValue.Create(s)).ToArray()),
                    int i => i,
                    bool b => b,
                    Guid g => g.ToString(),
                    JsonNode node => node,
                    IValueProvider v => await v.GetValueAsync(cancellationToken).ConfigureAwait(false),
                    null => null,
                    _ => throw new NotSupportedException($"The parameter value type {parameterValue.GetType()} is not supported.")
                }
            };
        }
    }

    internal static async Task SetScopeAsync(JsonObject scope, AzureBicepResource resource, CancellationToken cancellationToken = default)
    {
        // Resolve the scope from the AzureBicepResource if it has already been set
        // via the ConfigureInfrastructure callback. If not, fallback to the ExistingAzureResourceAnnotation.
        var targetScope = GetExistingResourceGroup(resource);

        scope["resourceGroup"] = targetScope switch
        {
            string s => s,
            IValueProvider v => await v.GetValueAsync(cancellationToken).ConfigureAwait(false),
            null => null,
            _ => throw new NotSupportedException($"The scope value type {targetScope.GetType()} is not supported.")
        };
    }

    private static bool IsParameterWithGeneratedValue(object? value)
    {
        return value is ParameterResource { Default: not null };
    }

    private const string PortalDeploymentOverviewUrl = "https://portal.azure.com/#view/HubsExtension/DeploymentDetailsBlade/~/overview/id";

    private static string GetDeploymentUrl(ProvisioningContext provisioningContext, ResourceGroupResource resourceGroup, string deploymentName)
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
