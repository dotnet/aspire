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

        static IAWSResource? SelectParentAWSResource(IResource resource) => resource switch
        {
            IAWSResource ar => ar,
            IResourceWithParent rp => SelectParentAWSResource(rp.Parent),
            _ => null
        };
        // parent -> children lookup
        var parentChildLookup = appModel.Resources.OfType<IResourceWithParent>()
            .Select(x => (Child: x, Root: SelectParentAWSResource(x.Parent)))
            .Where(x => x.Root is not null)
            .ToLookup(x => x.Root, x => x.Child);

        // Sets the state of the resource and all of its children
        async Task UpdateStateAsync(IAWSResource resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
        {
            await notificationService.PublishUpdateAsync(resource, stateFactory).ConfigureAwait(false);
            foreach (var child in parentChildLookup[resource])
            {
                await notificationService.PublishUpdateAsync(child, stateFactory).ConfigureAwait(false);
            }
        }
        // After the resource is provisioned, set its state
        async Task AfterProvisionAsync(IAWSResource resource)
        {
            try
            {
                await resource.ProvisioningTaskCompletionSource!.Task.ConfigureAwait(false);

                await UpdateStateAsync(resource, s => s with
                {
                    State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success)
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await UpdateStateAsync(resource, s => s with
                {
                    State = new ResourceStateSnapshot("Failed to Provision", KnownResourceStateStyles.Error)
                }).ConfigureAwait(false);
            }
        }
        // Mark all resources as starting
        foreach (var r in awsResources)
        {
            r.ProvisioningTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await UpdateStateAsync(r, s => s with
            {
                State = new ResourceStateSnapshot("Starting", KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);

            // After the resource is provisioned, set its state
            _ = AfterProvisionAsync(r);
        }

        // This is fully async, so we can just fire and forget
        _ = Task.Run(() => ProvisionAWSResources(awsResources, cancellationToken), cancellationToken);
    }

    private async Task ProvisionAWSResources(IList<IAWSResource> awsResources, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var resource in awsResources)
        {
            tasks.Add(ProcessResourceAsync(resource, cancellationToken));
        }

        var task = Task.WhenAll(tasks);

        // Suppress throwing so that we can save the user secrets even if the task fails
        await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        // Set the completion source for all resources
        foreach (var resource in awsResources)
        {
            resource.ProvisioningTaskCompletionSource?.TrySetResult();
        }
    }

    private async Task ProcessResourceAsync(IAWSResource resource, CancellationToken cancellationToken)
    {
        var provisioner = SelectProvisioner(resource);

        var resourceLogger = loggerService.GetLogger(resource);

        if (provisioner is null)
        {
            resource.ProvisioningTaskCompletionSource?.TrySetResult();

            resourceLogger.LogWarning("No provisioner found for {ResourceType} skipping", resource.GetType().Name);
        }
        else if (!provisioner.ShouldProvision(resource))
        {
            resource.ProvisioningTaskCompletionSource?.TrySetResult();

            resourceLogger.LogInformation("Skipping {ResourceName} because it is not configured to be provisioned", resource.Name);
        }
        else
        {
            resourceLogger.LogInformation("Provisioning {ResourceName}...", resource.Name);

            try
            {
                await provisioner.GetOrCreateResourceAsync(resource, cancellationToken).ConfigureAwait(false);

                resource.ProvisioningTaskCompletionSource?.TrySetResult();
            }
            catch (Exception ex)
            {
                resourceLogger.LogError(ex, "Error provisioning {ResourceName}", resource.Name);

                resource.ProvisioningTaskCompletionSource?.TrySetException(new InvalidOperationException($"Unable to resolve references from {resource.Name}"));
            }
        }

        return;

        IAWSResourceProvisioner? SelectProvisioner(IAWSResource resource)
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
    }
}
