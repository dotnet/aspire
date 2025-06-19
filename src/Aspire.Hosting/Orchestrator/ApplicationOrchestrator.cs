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
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly CancellationTokenSource _shutdownCancellation = new();

    public ApplicationOrchestrator(DistributedApplicationModel model,
                                   IDcpExecutor dcpExecutor,
                                   DcpExecutorEvents dcpExecutorEvents,
                                   IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
                                   ResourceNotificationService notificationService,
                                   ResourceLoggerService loggerService,
                                   IDistributedApplicationEventing eventing,
                                   IServiceProvider serviceProvider,
                                   DistributedApplicationExecutionContext executionContext)
    {
        _dcpExecutor = dcpExecutor;
        _model = model;
        _parentChildLookup = RelationshipEvaluator.GetParentChildLookup(model);
        _lifecycleHooks = lifecycleHooks.ToArray();
        _notificationService = notificationService;
        _loggerService = loggerService;
        _eventing = eventing;
        _serviceProvider = serviceProvider;
        _executionContext = executionContext;

        dcpExecutorEvents.Subscribe<OnResourcesPreparedContext>(OnResourcesPrepared);
        dcpExecutorEvents.Subscribe<OnResourceChangedContext>(OnResourceChanged);
        dcpExecutorEvents.Subscribe<OnEndpointsAllocatedContext>(OnEndpointsAllocated);
        dcpExecutorEvents.Subscribe<OnResourceStartingContext>(OnResourceStarting);
        dcpExecutorEvents.Subscribe<OnResourceFailedToStartContext>(OnResourceFailedToStart);

        _eventing.Subscribe<ResourceEndpointsAllocatedEvent>(OnResourceEndpointsAllocated);
        _eventing.Subscribe<ConnectionStringAvailableEvent>(PublishConnectionStringValue);
        // Implement WaitFor functionality using BeforeResourceStartedEvent.
        _eventing.Subscribe<BeforeResourceStartedEvent>(WaitForInBeforeResourceStartedEvent);
    }

    private async Task PublishConnectionStringValue(ConnectionStringAvailableEvent @event, CancellationToken token)
    {
        if (@event.Resource is IResourceWithConnectionString resourceWithConnectionString)
        {
            var connectionString = await resourceWithConnectionString.GetConnectionStringAsync(token).ConfigureAwait(false);

            await _notificationService.PublishUpdateAsync(resourceWithConnectionString, state => state with
            {
                Properties = [.. state.Properties, new(CustomResourceKnownProperties.ConnectionString, connectionString) { IsSensitive = true }]
            })
            .ConfigureAwait(false);
        }
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
#pragma warning disable CS0618 // Type or member is obsolete
        var afterEndpointsAllocatedEvent = new AfterEndpointsAllocatedEvent(_serviceProvider, _model);
#pragma warning restore CS0618 // Type or member is obsolete
        await _eventing.PublishAsync(afterEndpointsAllocatedEvent, context.CancellationToken).ConfigureAwait(false);

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(_model, context.CancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PublishResourceEndpointUrls(IResource resource, CancellationToken cancellationToken)
    {
        // Process URLs for the resource.
        await ProcessResourceUrlCallbacks(resource, cancellationToken).ConfigureAwait(false);

        // Publish update with URLs.
        var urls = GetResourceUrls(resource);
        await _notificationService.PublishUpdateAsync(resource, s => s with { Urls = [.. urls] }).ConfigureAwait(false);
    }

    private static IEnumerable<UrlSnapshot> GetResourceUrls(IResource resource)
    {
        IEnumerable<UrlSnapshot> urls = [];
        if (resource.TryGetUrls(out var resourceUrls))
        {
            urls = resourceUrls.Select(url => new UrlSnapshot(Name: url.Endpoint?.EndpointName, Url: url.Url, IsInternal: url.DisplayLocation == UrlDisplayLocation.DetailsOnly)
            {
                // Endpoint URLs are inactive (hidden in the dashboard) when published here. It is assumed they will get activated later when the endpoint is considered active
                // by whatever allocated the endpoint in the first place, e.g. for resources controlled by DCP, when DCP detects the endpoint is listening.
                IsInactive = url.Endpoint is not null,
                DisplayProperties = new(url.DisplayText ?? "", url.DisplayOrder ?? 0)
            });
        }
        return urls;
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
                await SetChildResourceAsync(context.Resource, state: KnownResourceStates.Starting, startTimeStamp: null, stopTimeStamp: null).ConfigureAwait(false);
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

    private async Task OnResourcesPrepared(OnResourcesPreparedContext context)
    {
        await PublishResourcesInitialStateAsync(context.CancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessResourceUrlCallbacks(IResource resource, CancellationToken cancellationToken)
    {
        var urls = new List<ResourceUrlAnnotation>();

        // Project endpoints to URLs
        if (resource.TryGetEndpoints(out var endpoints) && resource is IResourceWithEndpoints resourceWithEndpoints)
        {
            foreach (var endpoint in endpoints)
            {
                // Create a URL for each endpoint
                Debug.Assert(endpoint.AllocatedEndpoint is not null, "Endpoint should be allocated at this point as we're calling this from ResourceEndpointsAllocatedEvent handler.");
                if (endpoint.AllocatedEndpoint is { } allocatedEndpoint)
                {
                    var url = new ResourceUrlAnnotation { Url = allocatedEndpoint.UriString, Endpoint = new EndpointReference(resourceWithEndpoints, endpoint) };
                    urls.Add(url);
                }
            }
        }

        if (resource.TryGetUrls(out var existingUrls))
        {
            // Static URLs added to the resource via WithUrl(string name, string url), i.e. not callback-based
            urls.AddRange(existingUrls);
        }

        // Run the URL callbacks
        if (resource.TryGetAnnotationsOfType<ResourceUrlsCallbackAnnotation>(out var callbacks))
        {
            var urlsCallbackContext = new ResourceUrlsCallbackContext(_executionContext, resource, urls, cancellationToken)
            {
                Logger = _loggerService.GetLogger(resource.Name)
            };
            foreach (var callback in callbacks)
            {
                await callback.Callback(urlsCallbackContext).ConfigureAwait(false);
            }
        }

        // Clear existing URLs
        if (existingUrls is not null)
        {
            var existing = existingUrls.ToArray();
            for (var i = existing.Length - 1; i >= 0; i--)
            {
                var url = existing[i];
                resource.Annotations.Remove(url);
            }
        }

        // Convert relative endpoint URLs to absolute URLs
        foreach (var url in urls)
        {
            if (url.Endpoint is { } endpoint)
            {
                if (url.Url.StartsWith('/') && endpoint.AllocatedEndpoint is { } allocatedEndpoint)
                {
                    url.Url = allocatedEndpoint.UriString.TrimEnd('/') + url.Url;
                }
            }
        }

        // Add URLs
        foreach (var url in urls)
        {
            resource.Annotations.Add(url);
        }
    }

    private async Task OnResourceEndpointsAllocated(ResourceEndpointsAllocatedEvent @event, CancellationToken cancellationToken)
    {
        await ProcessResourceWithoutLifetime(@event.Resource, cancellationToken).ConfigureAwait(false);
        await PublishResourceEndpointUrls(@event.Resource, cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessResourceWithoutLifetime(IResource resource, CancellationToken cancellationToken)
    {
        if (resource is not IResourceWithoutLifetime resourceWithoutLifetime
            || resourceWithoutLifetime is not IValueProvider valueProvider)
        {
            return;
        }

        try
        {
            var value = await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false);

            await _notificationService.PublishUpdateAsync(resourceWithoutLifetime, s =>
            {
                return s with
                {
                    Properties = s.Properties.SetResourceProperty("Value", value ?? "", resourceWithoutLifetime is ParameterResource p && p.Secret)
                };
            })
            .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _notificationService.PublishUpdateAsync(resourceWithoutLifetime, s =>
            {
                return s with
                {
                    State = new("Value missing", KnownResourceStateStyles.Error),
                    Properties = s.Properties.SetResourceProperty("Value", ex.Message)
                };
            })
            .ConfigureAwait(false);

            _loggerService.GetLogger(resourceWithoutLifetime.Name).LogError("{Message}", ex.Message);
        }
    }

    private async Task OnResourceChanged(OnResourceChangedContext context)
    {
        await _notificationService.PublishUpdateAsync(context.Resource, context.DcpResourceName, context.UpdateSnapshot).ConfigureAwait(false);

        if (context.ResourceType == KnownResourceTypes.Container)
        {
            await SetChildResourceAsync(context.Resource, context.Status.State, context.Status.StartupTimestamp, context.Status.FinishedTimestamp).ConfigureAwait(false);
        }
    }

    private async Task OnResourceFailedToStart(OnResourceFailedToStartContext context)
    {
        if (context.DcpResourceName != null)
        {
            await _notificationService.PublishUpdateAsync(context.Resource, context.DcpResourceName, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);

            if (context.ResourceType == KnownResourceTypes.Container)
            {
                await SetChildResourceAsync(context.Resource, KnownResourceStates.FailedToStart, startTimeStamp: null, stopTimeStamp: null).ConfigureAwait(false);
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

    private async Task SetChildResourceAsync(IResource resource, string? state, DateTime? startTimeStamp, DateTime? stopTimeStamp)
    {
        foreach (var child in _parentChildLookup[resource])
        {
            await _notificationService.PublishUpdateAsync(child, s => s with
            {
                State = state,
                StartTimeStamp = startTimeStamp,
                StopTimeStamp = stopTimeStamp
            }).ConfigureAwait(false);

            // Recurse to set the child resources of the child.
            await SetChildResourceAsync(child, state, startTimeStamp, stopTimeStamp)
                .ConfigureAwait(false);
        }
    }

    private async Task PublishResourcesInitialStateAsync(CancellationToken cancellationToken)
    {
        // Publish the initial state of the resources that have a snapshot annotation.
        foreach (var resource in _model.Resources)
        {
            // Process relationships for the resource.
            var relationships = ApplicationModel.ResourceSnapshotBuilder.BuildRelationships(resource);
            var parent = resource is IResourceWithParent hasParent
                ? hasParent.Parent
                : resource.Annotations.OfType<ResourceRelationshipAnnotation>().LastOrDefault(r => r.Type == KnownRelationshipTypes.Parent)?.Resource;
            var urls = GetResourceUrls(resource);

            await _notificationService.PublishUpdateAsync(resource, s =>
            {
                return s with
                {
                    Relationships = relationships,
                    Urls = [.. urls],
                    Properties = parent is null ? s.Properties : s.Properties.SetResourceProperty(KnownProperties.Resource.ParentName, parent.GetResolvedResourceNames()[0]),
                    HealthReports = GetInitialHealthReports(resource)
                };
            }).ConfigureAwait(false);

            // Notify resources that they need to initialize themselves.
            var initializeEvent = new InitializeResourceEvent(resource, _eventing, _loggerService, _notificationService, _serviceProvider);
            await _eventing.PublishAsync(initializeEvent, EventDispatchBehavior.NonBlockingConcurrent, cancellationToken).ConfigureAwait(false);
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
                await PublishConnectionStringAvailableEvent(child, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
