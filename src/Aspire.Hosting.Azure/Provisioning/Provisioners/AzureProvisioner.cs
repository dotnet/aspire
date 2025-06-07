// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

// Provisions azure resources for development purposes
internal sealed class AzureProvisioner(
    DistributedApplicationExecutionContext executionContext,
    IConfiguration configuration,
    ILogger<AzureProvisioner> logger,
    IServiceProvider serviceProvider,
    BicepProvisioner bicepProvisioner,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IDistributedApplicationEventing eventing,
    TokenCredentialHolder tokenCredentialHolder,
    IProvisioningContextProvider provisioningContextProvider,
    IUserSecretsManager userSecretsManager
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
        var userSecretsLazy = new Lazy<Task<JsonObject>>(() => userSecretsManager.LoadUserSecretsAsync(cancellationToken));

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
        try
        {
            var userSecrets = await userSecretsLazy.Value.ConfigureAwait(false);
            await userSecretsManager.SaveUserSecretsAsync(userSecrets, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Azure resource connection strings saved to user secrets.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to provision Azure resources because user secrets file is not well-formed JSON.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save user secrets.");
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

        // Only process AzureBicepResource resources
        if (resource.AzureResource is not AzureBicepResource bicepResource)
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogInformation("Skipping {resourceName} because it is not a Bicep resource.", resource.AzureResource.Name);
            return;
        }

        if (!BicepProvisioner.ShouldProvision(bicepResource))
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogInformation("Skipping {resourceName} because it is not configured to be provisioned.", resource.AzureResource.Name);
        }
        else if (await bicepProvisioner.ConfigureResourceAsync(configuration, bicepResource, cancellationToken).ConfigureAwait(false))
        {
            resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetResult();
            resourceLogger.LogInformation("Using connection information stored in user secrets for {resourceName}.", resource.AzureResource.Name);
            await PublishConnectionStringAvailableEventAsync().ConfigureAwait(false);
        }
        else
        {
            if (resource.AzureResource.IsExisting())
            {
                resourceLogger.LogInformation("Resolving {resourceName} as existing resource...", resource.AzureResource.Name);
            }
            else
            {
                resourceLogger.LogInformation("Provisioning {resourceName}...", resource.AzureResource.Name);
            }

            try
            {
                var provisioningContext = await provisioningContextLazy.Value.ConfigureAwait(false);

                await bicepProvisioner.GetOrCreateResourceAsync(
                    bicepResource,
                    provisioningContext,
                    cancellationToken).ConfigureAwait(false);

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
                resourceLogger.LogError(ex, "Error provisioning {ResourceName} because user secrets file is not well-formed JSON.", resource.AzureResource.Name);
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
            }
            catch (Exception ex)
            {
                resourceLogger.LogError(ex, "Error provisioning {ResourceName}.", resource.AzureResource.Name);
                resource.AzureResource.ProvisioningTaskCompletionSource?.TrySetException(new InvalidOperationException($"Unable to resolve references from {resource.AzureResource.Name}"));
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
}
