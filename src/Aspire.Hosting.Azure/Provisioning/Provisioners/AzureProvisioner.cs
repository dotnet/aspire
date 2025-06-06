// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

// Provisions azure resources for development purposes
internal sealed class AzureProvisioner(
    DistributedApplicationExecutionContext executionContext,
    IConfiguration configuration,
    ILogger<AzureProvisioner> logger,
    IServiceProvider serviceProvider,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IDistributedApplicationEventing eventing,
    TokenCredentialHolder tokenCredentialHolder,
    IProvisioningContextProvider provisioningContextProvider,
    IUserSecretsManager userSecretsManager,
    IBicepCliExecutor bicepCliExecutor,
    ISecretClientProvider secretClientProvider
    ) : IDistributedApplicationLifecycleHook
{
    internal const string AspireResourceNameTag = "aspire-resource-name";

    private ILookup<IResource, IResourceWithParent>? _parentChildLookup;

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // AzureProvisioner only applies to RunMode
        if (executionContext.IsPublishMode)
        {
            return;
        }

        var azureResources = AzureResourcePreparer.GetAzureResourcesFromAppModel(appModel);
        if (azureResources.Count == 0)
        {
            return;
        }

        // Create a map of parents to their children used to propagate state changes later.
        _parentChildLookup = appModel.Resources.OfType<IResourceWithParent>().ToLookup(r => r.Parent);

        // Sets the state of the resource and all of its children
        async Task UpdateStateAsync((IResource Resource, IAzureResource AzureResource) resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
        {
            await notificationService.PublishUpdateAsync(resource.AzureResource, stateFactory).ConfigureAwait(false);

            // Some IAzureResource instances are a surrogate for for another resource in the app model
            // to ensure that resource events are published for the resource that the user expects
            // we lookup the resource in the app model here and publish the update to it as well.
            if (resource.Resource != resource.AzureResource)
            {
                await notificationService.PublishUpdateAsync(resource.Resource, stateFactory).ConfigureAwait(false);
            }

            // We basically want child resources to be moved into the same state as their parent resources whenever
            // there is a state update. This is done for us in DCP so we replicate the behavior here in the Azure Provisioner.

            var childResources = _parentChildLookup[resource.Resource].ToList();

            for (var i = 0; i < childResources.Count; i++)
            {
                var child = childResources[i];

                // Add any level of children
                foreach (var grandChild in _parentChildLookup[child])
                {
                    if (!childResources.Contains(grandChild))
                    {
                        childResources.Add(grandChild);
                    }
                }

                await notificationService.PublishUpdateAsync(child, stateFactory).ConfigureAwait(false);
            }
        }

        // After the resource is provisioned, set its state
        async Task AfterProvisionAsync((IResource Resource, IAzureResource AzureResource) resource)
        {
            try
            {
                await resource.AzureResource.ProvisioningTaskCompletionSource!.Task.ConfigureAwait(false);

                var rolesFailed = await WaitForRoleAssignments(resource).ConfigureAwait(false);
                if (!rolesFailed)
                {
                    await UpdateStateAsync(resource, s => s with
                    {
                        State = new("Running", KnownResourceStateStyles.Success)
                    })
                    .ConfigureAwait(false);
                }
            }
            catch (MissingConfigurationException)
            {
                await UpdateStateAsync(resource, s => s with
                {
                    State = new("Missing subscription configuration", KnownResourceStateStyles.Error)
                })
                .ConfigureAwait(false);
            }
            catch (Exception)
            {
                await UpdateStateAsync(resource, s => s with
                {
                    State = new("Failed to Provision", KnownResourceStateStyles.Error)
                })
                .ConfigureAwait(false);
            }
        }

        async Task<bool> WaitForRoleAssignments((IResource Resource, IAzureResource AzureResource) resource)
        {
            var rolesFailed = false;
            if (resource.AzureResource.TryGetAnnotationsOfType<RoleAssignmentResourceAnnotation>(out var roleAssignments))
            {
                try
                {
                    foreach (var roleAssignment in roleAssignments)
                    {
                        await roleAssignment.RolesResource.ProvisioningTaskCompletionSource!.Task.ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    rolesFailed = true;
                    await UpdateStateAsync(resource, s => s with
                    {
                        State = new("Failed to Provision Roles", KnownResourceStateStyles.Error)
                    })
                    .ConfigureAwait(false);
                }
            }

            return rolesFailed;
        }

        // Mark all resources as starting
        foreach (var r in azureResources)
        {
            r.AzureResource!.ProvisioningTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await UpdateStateAsync(r, s => s with
            {
                State = new("Starting", KnownResourceStateStyles.Info)
            })
            .ConfigureAwait(false);

            // After the resource is provisioned, set its state
            _ = AfterProvisionAsync(r);
        }

        // This is fully async so we can just fire and forget
        _ = Task.Run(() => ProvisionAzureResources(
            configuration,
            logger,
            azureResources,
            cancellationToken), cancellationToken);
    }

    private async Task ProvisionAzureResources(
        IConfiguration configuration,
        ILogger<AzureProvisioner> logger,
        IList<(IResource Resource, IAzureResource AzureResource)> azureResources,
        CancellationToken cancellationToken)
    {
        var userSecretsPath = userSecretsManager.GetUserSecretsPath();
        var userSecretsLazy = new Lazy<Task<JsonObject>>(() => userSecretsManager.LoadUserSecretsAsync(userSecretsPath, cancellationToken));

        // Make resources wait on the same provisioning context
        var provisioningContextLazy = new Lazy<Task<ProvisioningContext>>(() => provisioningContextProvider.CreateProvisioningContextAsync(tokenCredentialHolder, userSecretsLazy, cancellationToken));

        var tasks = new List<Task>();

        foreach (var resource in azureResources)
        {
            tasks.Add(ProcessResourceAsync(configuration, provisioningContextLazy, resource, cancellationToken));
        }

        var task = Task.WhenAll(tasks);

        // Suppress throwing so that we can save the user secrets even if the task fails
        await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        // If we created any resources then save the user secrets
        if (userSecretsPath is not null)
        {
            try
            {
                var userSecrets = await userSecretsLazy.Value.ConfigureAwait(false);
                await userSecretsManager.SaveUserSecretsAsync(userSecretsPath, userSecrets, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Azure resource connection strings saved to user secrets.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
            }
        }

        // Set the completion source for all resources
        foreach (var resource in azureResources)
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
        }
    }

    private async Task ProcessResourceAsync(IConfiguration configuration, Lazy<Task<ProvisioningContext>> provisioningContextLazy, (IResource Resource, IAzureResource AzureResource) resource, CancellationToken cancellationToken)
    {
        var beforeResourceStartedEvent = new BeforeResourceStartedEvent(resource.Resource, serviceProvider);
        await eventing.PublishAsync(beforeResourceStartedEvent, cancellationToken).ConfigureAwait(false);

        var resourceLogger = loggerService.GetLogger(resource.AzureResource);

        // Only handle AzureBicepResource directly
        if (resource.AzureResource is not AzureBicepResource bicepResource)
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogWarning("Only Bicep resources are supported. Skipping {resourceType}.", resource.AzureResource.GetType().Name);
            return;
        }

        if (!ShouldProvision(bicepResource))
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogInformation("Skipping {resourceName} because it is not configured to be provisioned.", bicepResource.Name);
        }
        else if (await ConfigureResourceAsync(configuration, bicepResource, cancellationToken).ConfigureAwait(false))
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogInformation("Using connection information stored in user secrets for {resourceName}.", bicepResource.Name);
            await PublishConnectionStringAvailableEventAsync().ConfigureAwait(false);
        }
        else
        {
            if (bicepResource.IsExisting())
            {
                resourceLogger.LogInformation("Resolving {resourceName} as existing resource...", bicepResource.Name);
            }
            else
            {
                resourceLogger.LogInformation("Provisioning {resourceName}...", bicepResource.Name);
            }

            try
            {
                var provisioningContext = await provisioningContextLazy.Value.ConfigureAwait(false);

                await GetOrCreateResourceAsync(bicepResource, provisioningContext, cancellationToken).ConfigureAwait(false);

                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
                await PublishConnectionStringAvailableEventAsync().ConfigureAwait(false);
            }
            catch (AzureCliNotOnPathException ex)
            {
                resourceLogger.LogCritical("Using Azure resources during local development requires the installation of the Azure CLI. See https://aka.ms/dotnet/aspire/azcli for instructions.");
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (MissingConfigurationException ex)
            {
                resourceLogger.LogCritical("Resource could not be provisioned because Azure subscription, location, and resource group information is missing. See https://aka.ms/dotnet/aspire/azure/provisioning for more details.");
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (JsonException ex)
            {
                resourceLogger.LogError(ex, "Error provisioning {ResourceName} because user secrets file is not well-formed JSON.", bicepResource.Name);
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (Exception ex)
            {
                resourceLogger.LogError(ex, "Error provisioning {ResourceName}.", bicepResource.Name);
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(new InvalidOperationException($"Unable to resolve references from {bicepResource.Name}"));
            }
        }

        async Task PublishConnectionStringAvailableEventAsync()
        {
            var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(resource.Resource, serviceProvider);
            await eventing.PublishAsync(connectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);

            if (_parentChildLookup![resource.Resource] is { } children)
            {
                foreach (var child in children.OfType<IResourceWithConnectionString>())
                {
                    var childConnectionStringAvailableEvent = new ConnectionStringAvailableEvent(child, serviceProvider);
                    await eventing.PublishAsync(childConnectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    // Bicep provisioning methods integrated from BicepProvisioner

    private static bool ShouldProvision(AzureBicepResource resource)
        => !resource.IsContainer();

    private async Task<bool> ConfigureResourceAsync(IConfiguration configuration, AzureBicepResource resource, CancellationToken cancellationToken)
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

    private async Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
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

        // Use the bicep CLI executor to compile the bicep file to ARM template
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

    private static string GetChecksum(AzureBicepResource resource, JsonObject parameters, JsonObject? scope)
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

    private static async ValueTask<string?> GetCurrentChecksumAsync(AzureBicepResource resource, IConfiguration section, CancellationToken cancellationToken = default)
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
    private static async Task SetParametersAsync(JsonObject parameters, AzureBicepResource resource, bool skipDynamicValues = false, CancellationToken cancellationToken = default)
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

    private static async Task SetScopeAsync(JsonObject scope, AzureBicepResource resource, CancellationToken cancellationToken = default)
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

    private static string GetDeploymentUrl(ResourceIdentifier deploymentId) =>
        $"{PortalDeploymentOverviewUrl}/{Uri.EscapeDataString(deploymentId.ToString())}";
}
