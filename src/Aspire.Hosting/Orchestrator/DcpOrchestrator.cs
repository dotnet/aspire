// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Orchestrator;

internal sealed class DcpOrchestrator : IDcpOrchestrator
{
    private readonly DistributedApplicationModel _model;
    private readonly ILookup<IResource, IResource> _parentChildLookup;
    private readonly IDistributedApplicationLifecycleHook[] _lifecycleHooks;
    private readonly ResourceNotificationService _notificationService;
    private readonly ResourceLoggerService _loggerService;
    private readonly IDistributedApplicationEventing _eventing;
    private readonly IServiceProvider _serviceProvider;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly IDcpExecutor _dcpExecutor;

    public DcpOrchestrator(
        IDcpExecutor dcpExecutor,
        DistributedApplicationExecutionContext executionContext,
        DistributedApplicationModel model,
        IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
        ResourceNotificationService notificationService,
        ResourceLoggerService loggerService,
        IDistributedApplicationEventing eventing,
        IServiceProvider serviceProvider,
        DcpExecutorEvents dcpExecutorEvents)
    {
        _dcpExecutor = dcpExecutor;
        _executionContext = executionContext;
        _model = model;
        _parentChildLookup = RelationshipEvaluator.GetParentChildLookup(model);
        _lifecycleHooks = lifecycleHooks.ToArray();
        _notificationService = notificationService;
        _loggerService = loggerService;
        _eventing = eventing;
        _serviceProvider = serviceProvider;

        dcpExecutorEvents.Subscribe<OnResourceChangedContext>(OnResourceChanged);
        dcpExecutorEvents.Subscribe<OnEndpointsAllocatedContext>(OnEndpointsAllocated);
        dcpExecutorEvents.Subscribe<OnResourceStartingContext>(OnResourceStarting);
        dcpExecutorEvents.Subscribe<OnResourceFailedToStartContext>(OnResourceFailedToStart);

        eventing.Subscribe<InitializeResourceEvent>(async (e, ct) =>
        {
            if (e.Resource is ProjectResource or ExecutableResource or ContainerResource)
            {
                _loggerService.GetLogger(e.Resource).LogInformation("Initializing resource {ResourceName}", e.Resource.Name);

                await dcpExecutor.InitializeResourceAsync(e.Resource, ct).ConfigureAwait(false);
            }
        });
    }

    public async Task OnEndpointsAllocated(OnEndpointsAllocatedContext context)
    {
        var afterEndpointsAllocatedEvent = new AfterEndpointsAllocatedEvent(_serviceProvider, _model);
        await _eventing.PublishAsync(afterEndpointsAllocatedEvent, context.CancellationToken).ConfigureAwait(false);

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(_model, context.CancellationToken).ConfigureAwait(false);
        }

        // Fire the endpoints allocated event for all resources.
        foreach (var resource in _model.Resources)
        {
            await _eventing.PublishAsync(new ResourceEndpointsAllocatedEvent(resource), EventDispatchBehavior.NonBlockingConcurrent, context.CancellationToken).ConfigureAwait(false);
        }
    }

    public async Task OnResourceStarting(OnResourceStartingContext context)
    {
        switch (context.ResourceType)
        {
            case KnownResourceTypes.Project:
            case KnownResourceTypes.Executable:
                await PublishUpdateAsync(_notificationService, context.Resource, context.DcpResourceName, s => s with
                {
                    State = KnownResourceStates.Starting,
                    ResourceType = context.ResourceType,
                    // HealthReports = GetInitialHealthReports(context.Resource)
                })
                .ConfigureAwait(false);

                break;
            case KnownResourceTypes.Container:
                await PublishUpdateAsync(_notificationService, context.Resource, context.DcpResourceName, s => s with
                {
                    State = KnownResourceStates.Starting,
                    Properties = s.Properties.SetResourceProperty(KnownProperties.Container.Image, context.Resource.TryGetContainerImageName(out var imageName) ? imageName : ""),
                    ResourceType = KnownResourceTypes.Container,
                    // HealthReports = GetInitialHealthReports(context.Resource)
                })
                .ConfigureAwait(false);

                // Debug.Assert(context.DcpResourceName is not null, "Container that is starting should always include the DCP name.");
                // await SetChildResourceAsync(context.Resource, state: KnownResourceStates.Starting, startTimeStamp: null, stopTimeStamp: null).ConfigureAwait(false);
                break;
            default:
                break;
        }

        // await PublishConnectionStringAvailableEvent(context.Resource, context.CancellationToken).ConfigureAwait(false);

        var beforeResourceStartedEvent = new BeforeResourceStartedEvent(context.Resource, _serviceProvider);
        await _eventing.PublishAsync(beforeResourceStartedEvent, context.CancellationToken).ConfigureAwait(false);

        static Task PublishUpdateAsync(ResourceNotificationService notificationService, IResource resource, string? resourceId, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
        {
            return resourceId != null
                ? notificationService.PublishUpdateAsync(resource, resourceId, stateFactory)
                : notificationService.PublishUpdateAsync(resource, stateFactory);
        }
    }

    public async Task OnResourceChanged(OnResourceChangedContext context)
    {
        await _notificationService.PublishUpdateAsync(context.Resource, context.DcpResourceName, context.UpdateSnapshot).ConfigureAwait(false);

        if (context.ResourceType == KnownResourceTypes.Container)
        {
            // await SetChildResourceAsync(context.Resource, context.Status.State, context.Status.StartupTimestamp, context.Status.FinishedTimestamp).ConfigureAwait(false);
        }
    }

    public async Task OnResourceFailedToStart(OnResourceFailedToStartContext context)
    {
        if (context.DcpResourceName != null)
        {
            await _notificationService.PublishUpdateAsync(context.Resource, context.DcpResourceName, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);

            if (context.ResourceType == KnownResourceTypes.Container)
            {
                // await SetChildResourceAsync(context.Resource, KnownResourceStates.FailedToStart, startTimeStamp: null, stopTimeStamp: null).ConfigureAwait(false);
            }
        }
        else
        {
            await _notificationService.PublishUpdateAsync(context.Resource, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);
        }
    }

    public async Task StartResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        var resourceReference = _dcpExecutor.GetResource(resourceName);

        // Figure out if the resource is waiting or not using PublishUpdateAsync, and if it is then set the
        // state to "Starting" to force waiting to complete.
        var isWaiting = false;
        await _notificationService.PublishUpdateAsync(
            resourceReference.ModelResource,
            resourceReference.DcpResourceName,
            s =>
            {
                if (s.State?.Text == KnownResourceStates.Waiting)
                {
                    isWaiting = true;
                    return s with { State = KnownResourceStates.Starting };
                }

                return s;
            }).ConfigureAwait(false);

        // A waiting resource is already trying to start up and asking DCP to start it will result in a conflict.
        // We only want to ask the DCP to start the resource if it wasn't.
        if (!isWaiting)
        {
            await _dcpExecutor.StartResourceAsync(resourceReference, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StopResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        var resourceReference = _dcpExecutor.GetResource(resourceName);
        await _dcpExecutor.StopResourceAsync(resourceReference, cancellationToken).ConfigureAwait(false);
    }
}
