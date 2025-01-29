// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Orchestrator;

internal sealed class ApplicationOrchestrator
{
    private readonly IDcpExecutor _dcpExecutor;
    private readonly DistributedApplicationModel _model;
    private readonly ILookup<IResource?, IResourceWithParent> _parentChildLookup;
    private readonly IDistributedApplicationLifecycleHook[] _lifecycleHooks;
    private readonly ResourceNotificationService _notificationService;
    private readonly IDistributedApplicationEventing _eventing;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _shutdownCancellation = new();

    private readonly ConcurrentDictionary<IResource, StartingResourceState> _startingResourceState = new();

    private class StartingResourceState
    {
        public CancellationTokenSource? WaitCancellation { get; set; }
        public Task? WaitForDependenciesTask { get; set; }
        public SemaphoreSlim StartingLock { get; } = new(1);
    }

    public ApplicationOrchestrator(DistributedApplicationModel model,
                                   IDcpExecutor dcpExecutor,
                                   DcpExecutorEvents dcpExecutorEvents,
                                   IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
                                   ResourceNotificationService notificationService,
                                   IDistributedApplicationEventing eventing,
                                   IServiceProvider serviceProvider)
    {
        _dcpExecutor = dcpExecutor;
        _model = model;
        _parentChildLookup = GetParentChildLookup(model);
        _lifecycleHooks = lifecycleHooks.ToArray();
        _notificationService = notificationService;
        _eventing = eventing;
        _serviceProvider = serviceProvider;

        dcpExecutorEvents.Subscribe<OnEndpointsAllocatedContext>(OnEndpointsAllocated);
        dcpExecutorEvents.Subscribe<OnResourceStartingContext>(OnResourceStarting);
        dcpExecutorEvents.Subscribe<OnResourcesPreparedContext>(OnResourcesPrepared);
        dcpExecutorEvents.Subscribe<OnResourceChangedContext>(OnResourceChanged);
        dcpExecutorEvents.Subscribe<OnResourceFailedToStartContext>(OnResourceFailedToStart);

        _eventing.Subscribe<BeforeResourceStartedEvent>(async (@event, ct) =>
        {
            if (!_startingResourceState.TryGetValue(@event.Resource, out var state))
            {
                // Resource doesn't support cancellation of waiting for dependencies.
                await _notificationService.WaitForDependenciesAsync(@event.Resource, ct).ConfigureAwait(false);
            }
            else
            {
                //Debug.Assert(state.WaitCancellation is not null, "Cancellation token source should have been created.");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                var waitForDependenciesTask = _notificationService.WaitForDependenciesAsync(@event.Resource, cts.Token);
                await _notificationService.PublishUpdateAsync(@event.Resource, s => s with
                {
                    WaitForEvent = new(waitForDependenciesTask)
                }).ConfigureAwait(false);

                if (waitForDependenciesTask.IsCompletedSuccessfully)
                {
                    return;
                }

                var waitForNonWaitingStateTask = _notificationService.WaitForResourceAsync(
                    @event.Resource.GetResolvedResourceNames().Single(),
                    e => e.Snapshot.State?.Text != KnownResourceStates.Waiting,
                    cts.Token);

                try
                {
                    await Task.WhenAny(waitForDependenciesTask, waitForNonWaitingStateTask).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation.
                }
                finally
                {
                    cts.Cancel();
                }
            }
        });
    }

    private async Task OnEndpointsAllocated(OnEndpointsAllocatedContext context)
    {
        var afterEndpointsAllocatedEvent = new AfterEndpointsAllocatedEvent(_serviceProvider, _model);
        await _eventing.PublishAsync(afterEndpointsAllocatedEvent, context.CancellationToken).ConfigureAwait(false);

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(_model, context.CancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnResourceStarting(OnResourceStartingContext context)
    {
        // Note that this state is for an IResource which could mean it is shared between multiple DCP resources in the case of replicas.
        // Sharing the state is required because the BeforeResourceStartedEvent doesn't take a DCP resource name, which means it is shared
        // and the IResource is the only key available.
        var state = _startingResourceState.GetOrAdd(context.Resource, r =>
        {
            return new StartingResourceState();
        });

        var hasLock = await state.StartingLock.WaitAsync(TimeSpan.Zero, context.CancellationToken).ConfigureAwait(false);
        if (!hasLock)
        {
            // The resource is already starting.
            return;
        }

        try
        {
            switch (context.ResourceType)
            {
                case KnownResourceTypes.Project:
                case KnownResourceTypes.Executable:
                    await _notificationService.PublishUpdateAsync(context.Resource, s => s with
                    {
                        State = KnownResourceStates.Starting,
                        ResourceType = context.ResourceType,
                        HealthReports = GetInitialHealthReports(context.Resource)
                    })
                    .ConfigureAwait(false);
                    break;
                case KnownResourceTypes.Container:
                    await _notificationService.PublishUpdateAsync(context.Resource, s => s with
                    {
                        State = KnownResourceStates.Starting,
                        Properties = s.Properties.SetResourceProperty(KnownProperties.Container.Image, context.Resource.TryGetContainerImageName(out var imageName) ? imageName : ""),
                        ResourceType = KnownResourceTypes.Container,
                        HealthReports = GetInitialHealthReports(context.Resource)
                    })
                    .ConfigureAwait(false);

                    Debug.Assert(context.DcpResourceName is not null, "Container that is starting should always include the DCP name.");
                    await SetChildResourceAsync(context.Resource, context.DcpResourceName, state: KnownResourceStates.Starting, startTimeStamp: null, stopTimeStamp: null).ConfigureAwait(false);
                    break;
                default:
                    break;
            }

            await PublishConnectionStringAvailableEvent(context.Resource, context.CancellationToken).ConfigureAwait(false);

            state.WaitCancellation = new();

            var beforeResourceStartedEvent = new BeforeResourceStartedEvent(context.Resource, _serviceProvider);
            await _eventing.PublishAsync(beforeResourceStartedEvent, context.CancellationToken).ConfigureAwait(false);

            state.WaitForDependenciesTask = null;
        }
        finally
        {
            state.StartingLock.Release();
        }
    }

    private async Task OnResourcesPrepared(OnResourcesPreparedContext _)
    {
        await PublishResourcesWithInitialStateAsync().ConfigureAwait(false);
    }

    private async Task OnResourceChanged(OnResourceChangedContext context)
    {
        await _notificationService.PublishUpdateAsync(context.Resource, context.DcpResourceName, context.UpdateSnapshot).ConfigureAwait(false);

        if (context.ResourceType == KnownResourceTypes.Container)
        {
            await SetChildResourceAsync(context.Resource, context.DcpResourceName, context.Status.State, context.Status.StartupTimestamp, context.Status.FinishedTimestamp).ConfigureAwait(false);
        }
    }

    private async Task OnResourceFailedToStart(OnResourceFailedToStartContext context)
    {
        if (context.DcpResourceName != null)
        {
            await _notificationService.PublishUpdateAsync(context.Resource, context.DcpResourceName, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);

            if (context.ResourceType == KnownResourceTypes.Container)
            {
                await SetChildResourceAsync(context.Resource, context.DcpResourceName, KnownResourceStates.FailedToStart, startTimeStamp: null, stopTimeStamp: null).ConfigureAwait(false);
            }
        }
        else
        {
            await _notificationService.PublishUpdateAsync(context.Resource, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);
        }
    }

    public async Task RunApplicationAsync(CancellationToken cancellationToken = default)
    {
        await _dcpExecutor.RunApplicationAsync(cancellationToken).ConfigureAwait(false);

        var afterResourcesCreatedEvent = new AfterResourcesCreatedEvent(_serviceProvider, _model);
        await _eventing.PublishAsync(afterResourcesCreatedEvent, cancellationToken).ConfigureAwait(false);

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterResourcesCreatedAsync(_model, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCancellation.Cancel();

        await _dcpExecutor.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StartResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        var resourceReference = _dcpExecutor.GetResource(resourceName);

        await resourceReference.ResourceLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            //_notificationService.WaitForDependenciesAsync
            // Clear the BeforeResourceStart snapshot to indicate that the resource is starting.
            await _notificationService.PublishUpdateAsync(
                resourceReference.ModelResource,
                s => s with { State = s.State?.Text == KnownResourceStates.Waiting ? "ForceStart" : s.State?.Text }).ConfigureAwait(false);

            var task = await _notificationService.WaitForResourceAsync(resourceName, e => e.Snapshot.WaitForEvent != null, cancellationToken).ConfigureAwait(false);

            GetResourceSnapshotAsync(resourceName, e => e.Snapshot.WaitForEvent == null)
            //await foreach (var resourceEvent in _notificationService.WatchAsync(cancellationToken).ConfigureAwait(false))
            //{
            //    resourceEvent.Snapshot.ResourceReadyEvent
            //}

            //if (_startingResourceState.TryGetValue(resourceReference.ModelResource, out var state))
            //{
            //    if (state.WaitForDependenciesTask is { } task && !task.IsCompleted)
            //    {
            //        // The resource is already trying to start but is blocked on waiting for its dependencies.
            //        // Cancel waiting to unblock the start then exit.
            //        state.WaitCancellation?.Cancel();
            //        await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            //        return;
            //    }
            //}

            // Resource either isn't waiting or doesn't support it.
            await _dcpExecutor.StartResourceAsync(resourceReference, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            resourceReference.ResourceLock.Release();
        }
    }

    private async Task<CustomResourceSnapshot> GetResourceSnapshotAsync(string resourceName, Func<ResourceEvent, bool> predicate, CancellationToken cancellationToken = default)
    {
        await foreach (var resourceEvent in _notificationService.WatchAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName))
            {
                return resourceEvent.Snapshot;
            }
        }

        throw new OperationCanceledException($"The operation was cancelled before resource '{resourceName}' was found.");
    }

    public async Task StopResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        var resourceReference = _dcpExecutor.GetResource(resourceName);
        await _dcpExecutor.StopResourceAsync(resourceReference, cancellationToken).ConfigureAwait(false);
    }

    private static ILookup<IResource?, IResourceWithParent> GetParentChildLookup(DistributedApplicationModel model)
    {
        static IResource? SelectParentContainerResource(IResource resource) => resource switch
        {
            IResourceWithParent rp => SelectParentContainerResource(rp.Parent),
            IResource r when r.IsContainer() => r,
            _ => null
        };

        // parent -> children lookup
        return model.Resources.OfType<IResourceWithParent>()
                              .Select(x => (Child: x, Root: SelectParentContainerResource(x.Parent)))
                              .Where(x => x.Root is not null)
                              .ToLookup(x => x.Root, x => x.Child);
    }

    private async Task SetChildResourceAsync(IResource resource, string parentName, string? state, DateTime? startTimeStamp, DateTime? stopTimeStamp)
    {
        foreach (var child in _parentChildLookup[resource])
        {
            await _notificationService.PublishUpdateAsync(child, s => s with
            {
                State = state,
                StartTimeStamp = startTimeStamp,
                StopTimeStamp = stopTimeStamp,
                Properties = s.Properties.SetResourceProperty(KnownProperties.Resource.ParentName, parentName)
            }).ConfigureAwait(false);
        }
    }

    private async Task PublishResourcesWithInitialStateAsync()
    {
        // Publish the initial state of the resources that have a snapshot annotation.
        foreach (var resource in _model.Resources)
        {
            await _notificationService.PublishUpdateAsync(resource, s =>
            {
                return s with
                {
                    HealthReports = GetInitialHealthReports(resource)
                };
            }).ConfigureAwait(false);
        }
    }

    private static ImmutableArray<HealthReportSnapshot> GetInitialHealthReports(IResource resource)
    {
        if (!resource.TryGetAnnotationsIncludingAncestorsOfType<HealthCheckAnnotation>(out var annotations))
        {
            return [];
        }

        var reports = annotations.Select(annotation => new HealthReportSnapshot(annotation.Key, null, null, null));
        return [.. reports];
    }

    private async Task PublishConnectionStringAvailableEvent(IResource resource, CancellationToken cancellationToken)
    {
        // If the resource itself has a connection string then publish that the connection string is available.
        if (resource is IResourceWithConnectionString)
        {
            var connectionStringAvailableEvent = new ConnectionStringAvailableEvent(resource, _serviceProvider);
            await _eventing.PublishAsync(connectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);
        }

        // Sometimes the container/executable itself does not have a connection string, and in those cases
        // we need to dispatch the event for the children.
        if (_parentChildLookup[resource] is { } children)
        {
            foreach (var child in children.OfType<IResourceWithConnectionString>())
            {
                var childConnectionStringAvailableEvent = new ConnectionStringAvailableEvent(child, _serviceProvider);
                await _eventing.PublishAsync(childConnectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
