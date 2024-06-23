// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class AWSProvisioner(
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsPublishMode)
        {
            return;
        }

        var awsResources = appModel.Resources.OfType<IAWSResource>().ToList();
        if (awsResources.Count == 0)
        {
            return;
        }

        // parent -> children lookup
        var parentChildLookup = appModel.Resources.OfType<IResourceWithParent>()
            .Select(x => (Child: x, Root: x.Parent.TrySelectParentResource<IAWSResource>()))
            .Where(x => x.Root is not null)
            .ToLookup(x => x.Root, x => x.Child);

        // Mark all resources as starting
        foreach (var r in awsResources)
        {
            r.ProvisioningTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await UpdateStateAsync(r, parentChildLookup, s => s with
            {
                State = new ResourceStateSnapshot("Starting", KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);
        }

        // This is fully async, so we can just fire and forget
        _ = Task.Run(() => ProvisionAWSResourcesAsync(awsResources, parentChildLookup, cancellationToken), cancellationToken);
    }

    private async Task ProvisionAWSResourcesAsync(IList<IAWSResource> awsResources, ILookup<IAWSResource?, IResourceWithParent> parentChildLookup, CancellationToken cancellationToken)
    {
        foreach (var resource in awsResources)
        {
            var provisioner = SelectProvisioner(resource);

            var resourceLogger = loggerService.GetLogger(resource);

            if (provisioner is null)
            {
                resource.ProvisioningTaskCompletionSource?.TrySetResult();

                resourceLogger.LogWarning("No provisioner found for {ResourceType} skipping", resource.GetType().Name);
            }
            else
            {
                resourceLogger.LogInformation("Provisioning {ResourceName}...", resource.Name);

                try
                {
                    await provisioner.GetOrCreateResourceAsync(resource, cancellationToken).ConfigureAwait(false);

                    await UpdateStateAsync(resource, parentChildLookup, s => s with
                    {
                        State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success)
                    }).ConfigureAwait(false);
                    resource.ProvisioningTaskCompletionSource?.TrySetResult();
                }
                catch (Exception ex)
                {
                    resourceLogger.LogError(ex, "Error provisioning {ResourceName}", resource.Name);

                    await UpdateStateAsync(resource, parentChildLookup, s => s with
                    {
                        State = new ResourceStateSnapshot("Failed to Provision", KnownResourceStateStyles.Error)
                    }).ConfigureAwait(false);
                    resource.ProvisioningTaskCompletionSource?.TrySetException(new InvalidOperationException($"Unable to resolve references from {resource.Name}"));
                }
            }
        }
    }

    private IAWSResourceProvisioner? SelectProvisioner(IAWSResource resource)
    {
        var type = resource.GetType();

        while (type is not null)
        {
            var provisioner = serviceProvider.GetKeyedService<IAWSResourceProvisioner>(type);

            if (provisioner is not null)
            {
                return provisioner;
            }

            type = type.BaseType;
        }

        return null;
    }

    private async Task UpdateStateAsync(IAWSResource resource, ILookup<IAWSResource?, IResourceWithParent> parentChildLookup, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        await notificationService.PublishUpdateAsync(resource, stateFactory).ConfigureAwait(false);
        foreach (var child in parentChildLookup[resource])
        {
            await notificationService.PublishUpdateAsync(child, stateFactory).ConfigureAwait(false);
        }
    }
}
