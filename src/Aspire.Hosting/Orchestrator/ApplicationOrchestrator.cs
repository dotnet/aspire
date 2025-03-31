// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Orchestrator;

internal sealed class ApplicationOrchestrator
{
    private readonly IDcpExecutor _dcpExecutor;
    private readonly DistributedApplicationModel _model;
    private readonly ILookup<IResource, IResource> _parentChildLookup;
    private readonly IDistributedApplicationLifecycleHook[] _lifecycleHooks;
    private readonly ResourceNotificationService _notificationService;
    private readonly ResourceLoggerService _loggerService;
    private readonly IDistributedApplicationEventing _eventing;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _shutdownCancellation = new();

    public ApplicationOrchestrator(DistributedApplicationModel model,
                                   IDcpExecutor dcpExecutor,
                                   DcpExecutorEvents dcpExecutorEvents,
                                   IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
                                   ResourceNotificationService notificationService,
                                   ResourceLoggerService loggerService,
                                   IDistributedApplicationEventing eventing,
                                   IServiceProvider serviceProvider)
    {
        _dcpExecutor = dcpExecutor;
        _model = model;
        _parentChildLookup = RelationshipEvaluator.GetParentChildLookup(model);
        _lifecycleHooks = lifecycleHooks.ToArray();
        _notificationService = notificationService;
        _loggerService = loggerService;
        _eventing = eventing;
        _serviceProvider = serviceProvider;

        dcpExecutorEvents.Subscribe<OnEndpointsAllocatedContext>(OnEndpointsAllocated);
        dcpExecutorEvents.Subscribe<OnResourceStartingContext>(OnResourceStarting);
        dcpExecutorEvents.Subscribe<OnResourcesPreparedContext>(OnResourcesPrepared);
        dcpExecutorEvents.Subscribe<OnResourceChangedContext>(OnResourceChanged);
        dcpExecutorEvents.Subscribe<OnResourceFailedToStartContext>(OnResourceFailedToStart);

        // Implement WaitFor functionality using BeforeResourceStartedEvent.
        _eventing.Subscribe<BeforeResourceStartedEvent>(WaitForInBeforeResourceStartedEvent);
        _eventing.Subscribe<AfterEndpointsAllocatedEvent>(ProcessResourcesWithoutLifetime);
    }

    private async Task WaitForInBeforeResourceStartedEvent(BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var waitForDependenciesTask = _notificationService.WaitForDependenciesAsync(@event.Resource, cts.Token);
        if (waitForDependenciesTask.IsCompletedSuccessfully)
        {
            // Nothing to wait for. Return immediately.
            return;
        }

        // Wait for either dependencies to be ready or for someone to move the resource out of a waiting state.
        // This happens when resource start command is run, which forces the status to "Starting".
        var waitForNonWaitingStateTask = _notificationService.WaitForResourceAsync(
            @event.Resource.Name,
            e => e.Snapshot.State?.Text != KnownResourceStates.Waiting,
            cts.Token);

        try
        {
            var completedTask = await Task.WhenAny(waitForDependenciesTask, waitForNonWaitingStateTask).ConfigureAwait(false);
            if (completedTask.IsFaulted)
            {
                // Make error visible from completed task.
                await completedTask.ConfigureAwait(false);
            }
        }
        finally
        {
            // Ensure both wait tasks are cancelled.
            cts.Cancel();
        }
    }

    private async Task OnEndpointsAllocated(OnEndpointsAllocatedContext context)
    {
        var afterEndpointsAllocatedEvent = new AfterEndpointsAllocatedEvent(_serviceProvider, _model);
        await _eventing.PublishAsync(afterEndpointsAllocatedEvent, context.CancellationToken).ConfigureAwait(false);

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(_model, context.CancellationToken).ConfigureAwait(false);
        }

        await ProcessUrls(context.CancellationToken).ConfigureAwait(false);
    }

    private async Task OnResourceStarting(OnResourceStartingContext context)
    {
        switch (context.ResourceType)
        {
            case KnownResourceTypes.Project:
            case KnownResourceTypes.Executable:
                await PublishUpdateAsync(_notificationService, context.Resource, context.DcpResourceName, s => s with
                {
                    State = KnownResourceStates.Starting,
                    ResourceType = context.ResourceType,
                    HealthReports = GetInitialHealthReports(context.Resource)
                })
                .ConfigureAwait(false);

                await SetExecutableChildResourceAsync(context.Resource).ConfigureAwait(false);
                break;
            case KnownResourceTypes.Container:
                await PublishUpdateAsync(_notificationService, context.Resource, context.DcpResourceName, s => s with
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

        var beforeResourceStartedEvent = new BeforeResourceStartedEvent(context.Resource, _serviceProvider);
        await _eventing.PublishAsync(beforeResourceStartedEvent, context.CancellationToken).ConfigureAwait(false);

        static Task PublishUpdateAsync(ResourceNotificationService notificationService, IResource resource, string? resourceId, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
        {
            return resourceId != null
                ? notificationService.PublishUpdateAsync(resource, resourceId, stateFactory)
                : notificationService.PublishUpdateAsync(resource, stateFactory);
        }
    }

    private async Task OnResourcesPrepared(OnResourcesPreparedContext _)
    {
        await PublishResourcesWithInitialStateAsync().ConfigureAwait(false);
    }

    private async Task ProcessUrls(CancellationToken cancellationToken)
    {
        // Project endpoints to URLS
        foreach (var resource in _model.Resources.OfType<IResourceWithEndpoints>())
        {
            var urls = new List<ResourceUrlAnnotation>();

            if (resource.TryGetEndpoints(out var endpoints))
            {
                foreach (var endpoint in endpoints)
                {
                    // Create a URL for each endpoint
                    if (endpoint.AllocatedEndpoint is { } allocatedEndpoint)
                    {
                        var url = new ResourceUrlAnnotation { Url = allocatedEndpoint.UriString, Endpoint = new EndpointReference(resource, endpoint) };
                        urls.Add(url);
                    }
                }
            }

            // Run the URL callbacks
            if (resource.TryGetAnnotationsOfType<ResourceUrlsCallbackAnnotation>(out var callbacks))
            {
                var urlsCallbackContext = new ResourceUrlsCallbackContext(new(DistributedApplicationOperation.Run), resource, urls, cancellationToken)
                {
                    Logger = _loggerService.GetLogger(resource.Name)
                };
                foreach (var callback in callbacks)
                {
                    await callback.Callback(urlsCallbackContext).ConfigureAwait(false);
                }
            }

            foreach (var url in urls)
            {
                resource.Annotations.Add(url);
            }
        }
    }

    private Task ProcessResourcesWithoutLifetime(AfterEndpointsAllocatedEvent @event, CancellationToken cancellationToken)
    {
        async Task ProcessValueAsync(IResource resource, IValueProvider vp)
        {
            try
            {
                var value = await vp.GetValueAsync(default).ConfigureAwait(false);

                await _notificationService.PublishUpdateAsync(resource, s =>
                {
                    return s with
                    {
                        Properties = s.Properties.SetResourceProperty("Value", value ?? "", resource is ParameterResource p && p.Secret)
                    };
                })
                .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _notificationService.PublishUpdateAsync(resource, s =>
                {
                    return s with
                    {
                        State = new("Value missing", KnownResourceStateStyles.Error),
                        Properties = s.Properties.SetResourceProperty("Value", ex.Message)
                    };
                })
                .ConfigureAwait(false);

                _loggerService.GetLogger(resource.Name).LogError("{Message}", ex.Message);
            }
        }

        foreach (var resource in _model.Resources.OfType<IResourceWithoutLifetime>())
        {
            if (resource is IValueProvider provider)
            {
                _ = ProcessValueAsync(resource, provider);
            }
        }

        return Task.CompletedTask;
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

            // the parent name needs to be an instance name, not the resource name.
            // parent the children of the child under the first resource instance.
            await SetChildResourceAsync(child, child.GetResolvedResourceNames()[0], state, startTimeStamp, stopTimeStamp)
                .ConfigureAwait(false);
        }
    }

    private async Task SetExecutableChildResourceAsync(IResource resource)
    {
        // the parent name needs to be an instance name, not the resource name.
        // parent the children under the first resource instance.
        var parentName = resource.GetResolvedResourceNames()[0];

        foreach (var child in _parentChildLookup[resource])
        {
            await _notificationService.PublishUpdateAsync(child, s => s with
            {
                Properties = s.Properties.SetResourceProperty(KnownProperties.Resource.ParentName, parentName)
            }).ConfigureAwait(false);

            await SetExecutableChildResourceAsync(child).ConfigureAwait(false);
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
            // only dispatch the event for children that have a connection string and are IResourceWithParent, not parented by annotations.
            foreach (var child in children.OfType<IResourceWithConnectionString>().Where(c => c is IResourceWithParent))
            {
                var childConnectionStringAvailableEvent = new ConnectionStringAvailableEvent(child, _serviceProvider);
                await _eventing.PublishAsync(childConnectionStringAvailableEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
