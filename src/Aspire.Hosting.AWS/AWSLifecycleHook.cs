// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.AWS.Provisioning;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS;

internal sealed class AWSLifecycleHook(
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var awsResources = appModel.Resources.OfType<IAWSResource>().ToList();
        if (awsResources.Count == 0) // Skip when no AWS resources are found
        {
            return Task.CompletedTask;
        }

        // Create a lookup for all resources implementing IResourceWithParent and have IAWSResource as parent in the tree.
        // Typical children that are listed here are IStackResource with IConstructResource as children.
        // This is important for state reporting so that a stack and it child resources are handled.
        var parentChildLookup = appModel.Resources.OfType<IResourceWithParent>()
            .Select(x => (Child: x, Root: x.Parent.TrySelectParentResource<IAWSResource>()))
            .Where(x => x.Root is not null)
            .ToLookup(x => x.Root, x => x.Child);

        // Synthesize AWS CDK resources before provisioning or writing the manifest
        SynthesizeAWSCDKResources(awsResources, parentChildLookup);

        // Provisioning resources is fully async, so we can just fire and forget
        _ = Task.Run(() => ProvisionAWSResourcesAsync(awsResources, parentChildLookup, cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private static void SynthesizeAWSCDKResources(IList<IAWSResource> awsResources, ILookup<IAWSResource?, IResourceWithParent> parentChildLookup)
    {
        // Only look at StackResources
        var stackResources = awsResources.OfType<StackResource>().ToList();
        foreach (var stackResource in stackResources)
        {
            // Apply construct modifier annotations as some constructs needs te be altered after the fact, like adding outputs.
            var constructResources = parentChildLookup[stackResource].OfType<IResourceWithConstruct>();
            foreach (var constructResource in constructResources.Concat([stackResource]))
            {
                // Find Construct Modifier Annotations
                if (!constructResource.TryGetAnnotationsOfType<IConstructModifierAnnotation>(out var modifiers))
                {
                    continue;
                }

                // Modify stack
                foreach (var modifier in modifiers)
                {
                    modifier.ChangeConstruct(constructResource.Construct);
                }
            }
        }

        // Create a lookup for stack resources and their AWS CDK app
        var appLookup = stackResources
            .Select(r => (Child: r, r.App))
            .ToLookup(r => r.App, r => r.Child);

        foreach (var app in appLookup)
        {
            // Synthesize AWS CDK app
            var cloudAssembly = app.Key.Synth();
            // Attach the stack artifact to the stack resources for provisioning
            foreach (var stackResource in app)
            {
                var stackArtifact = cloudAssembly.Stacks.FirstOrDefault(stack => stack.StackName == stackResource.StackName)
                                    ?? throw new InvalidOperationException($"Stack '{stackResource.StackName}' not found in synthesized cloud assembly.");
                // Annotate the resource with information for writing the manifest and provisioning.
                stackResource.Annotations.Add(new StackArtifactResourceAnnotation(stackArtifact));
            }
        }
    }

    #region Provisioning

    private async Task ProvisionAWSResourcesAsync(IList<IAWSResource> awsResources, ILookup<IAWSResource?, IResourceWithParent> parentChildLookup, CancellationToken cancellationToken)
    {
        // Skip when publishing, this is intended for provisioning only.
        if (executionContext.IsPublishMode)
        {
            return;
        }

        // Mark all resources as starting
        foreach (var r in awsResources)
        {
            r.ProvisioningTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await UpdateStateAsync(r, parentChildLookup, s => s with
            {
                State = new ResourceStateSnapshot("Starting", KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);
        }

        foreach (var resource in awsResources)
        {
            // Resolve a provisioner for the AWS Resource
            var provisioner = SelectProvisioner(resource);

            var resourceLogger = loggerService.GetLogger(resource);

            if (provisioner is null) // Skip when no provisioner is found
            {
                resource.ProvisioningTaskCompletionSource?.TrySetResult();

                resourceLogger.LogWarning("No provisioner found for {ResourceType} skipping", resource.GetType().Name);
            }
            else
            {
                resourceLogger.LogInformation("Provisioning {ResourceName}...", resource.Name);

                try
                {
                    // Provision resources
                    await provisioner.GetOrCreateResourceAsync(resource, cancellationToken).ConfigureAwait(false);

                    // Mark resources as running
                    await UpdateStateAsync(resource, parentChildLookup, s => s with
                    {
                        State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Success)
                    }).ConfigureAwait(false);
                    resource.ProvisioningTaskCompletionSource?.TrySetResult();
                }
                catch (Exception ex)
                {
                    resourceLogger.LogError(ex, "Error provisioning {ResourceName}", resource.Name);

                    // Mark resources as failed
                    await UpdateStateAsync(resource, parentChildLookup, s => s with
                    {
                        State = new ResourceStateSnapshot("Failed to Provision", KnownResourceStateStyles.Error)
                    }).ConfigureAwait(false);
                    resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
                }
            }
        }
    }

    private IAWSResourceProvisioner? SelectProvisioner(IAWSResource resource)
    {
        var type = resource.GetType();
        while (type is not null) // Loop through all the base types to find a resource that as a provisioner
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
        // Update the state of the IAWSRsource and all it's children
        await notificationService.PublishUpdateAsync(resource, stateFactory).ConfigureAwait(false);
        foreach (var child in parentChildLookup[resource])
        {
            await notificationService.PublishUpdateAsync(child, stateFactory).ConfigureAwait(false);
        }
    }

    #endregion
}
