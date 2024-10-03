// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public class ResourceNotificationService
{
    // Resource state is keyed by the resource and the unique name of the resource. This could be the name of the resource, or a replica ID.
    private readonly ConcurrentDictionary<(IResource, string), ResourceNotificationState> _resourceNotificationStates = new();
    private readonly ILogger<ResourceNotificationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationToken _applicationStopping;
    private readonly ResourceLoggerService _resourceLoggerService;

    private Action<ResourceEvent>? OnResourceUpdated { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <remarks>
    /// Obsolete. Use the constructor that accepts an <see cref="ILogger{ResourceNotificationService}"/>, <see cref="IHostApplicationLifetime"/> and <see cref="IServiceProvider"/>.<br/>
    /// This constructor will be removed in the next major version of Aspire.
    /// </remarks>
    /// <param name="logger">The logger.</param>
    /// <param name="hostApplicationLifetime">The host application lifetime.</param>
    [Obsolete($"""
        {nameof(ResourceNotificationService)} now requires an {nameof(IServiceProvider)} and {nameof(ResourceLoggerService)}.
        Use the constructor that accepts an {nameof(ILogger)}<{nameof(ResourceNotificationService)}>, {nameof(IHostApplicationLifetime)}, {nameof(IServiceProvider)} and {nameof(ResourceLoggerService)}.
        This constructor will be removed in the next major version of Aspire.
        """)]
    public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = new NullServiceProvider();
        _applicationStopping = hostApplicationLifetime?.ApplicationStopping ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        _resourceLoggerService = new ResourceLoggerService();
    }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostApplicationLifetime">The host application lifetime.</param>
    /// <param name="resourceLoggerService">The resource logger service.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ResourceNotificationService(ILogger<ResourceNotificationService> logger, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider, ResourceLoggerService resourceLoggerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider;
        _applicationStopping = hostApplicationLifetime?.ApplicationStopping ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        _resourceLoggerService = resourceLoggerService ?? throw new ArgumentNullException(nameof(resourceLoggerService));
    }

    private class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// Waits for a resource to reach the specified state. See <see cref="KnownResourceStates"/> for common states.
    /// </summary>
    /// <remarks>
    /// This method returns a task that will complete when the resource reaches the specified target state. If the resource
    /// is already in the target state, the method will return immediately.<br/>
    /// If the resource doesn't reach one of the target states before <paramref name="cancellationToken"/> is signaled, this method
    /// will throw <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="targetState">The state to wait for the resource to transition to. See <see cref="KnownResourceStates"/> for common states.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the wait operation.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "targetState(s) parameters are mutually exclusive.")]
    public Task WaitForResourceAsync(string resourceName, string? targetState = null, CancellationToken cancellationToken = default)
    {
        string[] targetStates = !string.IsNullOrEmpty(targetState) ? [targetState] : [KnownResourceStates.Running];
        return WaitForResourceAsync(resourceName, targetStates, cancellationToken);
    }

    /// <summary>
    /// Waits for a resource to reach one of the specified states. See <see cref="KnownResourceStates"/> for common states.
    /// </summary>
    /// <remarks>
    /// This method returns a task that will complete when the resource reaches one of the specified target states. If the resource
    /// is already in the target state, the method will return immediately.<br/>
    /// If the resource doesn't reach one of the target states before <paramref name="cancellationToken"/> is signaled, this method
    /// will throw <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="targetStates">The set of states to wait for the resource to transition to one of. See <see cref="KnownResourceStates"/> for common states.</param>
    /// <param name="cancellationToken">A cancellation token that cancels the wait operation when signaled.</param>
    /// <returns>A <see cref="Task{String}"/> representing the wait operation and which of the target states the resource reached.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "targetState(s) parameters are mutually exclusive.")]
    public async Task<string> WaitForResourceAsync(string resourceName, IEnumerable<string> targetStates, CancellationToken cancellationToken = default)
    {
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping, cancellationToken);
        var watchToken = watchCts.Token;
        await foreach (var resourceEvent in WatchAsync(watchToken).ConfigureAwait(false))
        {
            if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName)
                && resourceEvent.Snapshot.State?.Text is { Length: > 0 } statusText
                && targetStates.Contains(statusText, StringComparers.ResourceState))
            {
                return statusText;
            }
        }

        throw new OperationCanceledException($"The operation was cancelled before the resource reached one of the target states: [{string.Join(", ", targetStates)}]");
    }

    private async Task WaitUntilHealthyAsync(IResource resource, IResource dependency, CancellationToken cancellationToken)
    {
        var resourceLogger = _resourceLoggerService.GetLogger(resource);
        resourceLogger.LogInformation("Waiting for resource '{Name}' to enter the '{State}' state.", dependency.Name, KnownResourceStates.Running);

        await PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);
        var resourceEvent = await WaitForResourceAsync(dependency.Name, re => IsContinuableState(re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);
        var snapshot = resourceEvent.Snapshot;

        if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
        {
            resourceLogger.LogError(
                "Dependency resource '{ResourceName}' failed to start.",
                dependency.Name
                );

            throw new DistributedApplicationException($"Dependency resource '{dependency.Name}' failed to start.");
        }
        else if (snapshot.State!.Text == KnownResourceStates.Finished || snapshot.State!.Text == KnownResourceStates.Exited)
        {
            resourceLogger.LogError(
                "Resource '{ResourceName}' has entered the '{State}' state prematurely.",
                dependency.Name,
                snapshot.State.Text
                );

            throw new DistributedApplicationException(
                $"Resource '{dependency.Name}' has entered the '{snapshot.State.Text}' state prematurely."
                );
        }

        // If our dependency resource has health check annotations we want to wait until they turn healthy
        // otherwise we don't care about their health status.
        if (dependency.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var _))
        {
            resourceLogger.LogInformation("Waiting for resource '{Name}' to become healthy.", dependency.Name);
            await WaitForResourceHealthyAsync(dependency.Name, cancellationToken).ConfigureAwait(false);
        }

        resourceLogger.LogInformation("Finished waiting for resource '{Name}'.", dependency.Name);

        static bool IsContinuableState(CustomResourceSnapshot snapshot) =>
            snapshot.State?.Text == KnownResourceStates.Running ||
            snapshot.State?.Text == KnownResourceStates.Finished ||
            snapshot.State?.Text == KnownResourceStates.Exited ||
            snapshot.State?.Text == KnownResourceStates.FailedToStart;
    }

    /// <summary>
    /// Waits for a resource to become healthy.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    /// <remarks>
    /// This method returns a task that will complete with the resource is healthy. A resource
    /// without <see cref="HealthCheckAnnotation"/> annotations will be considered healthy.
    /// </remarks>
    public Task<ResourceEvent> WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        return WaitForResourceAsync(resourceName, re => re.Snapshot.HealthStatus == HealthStatus.Healthy, cancellationToken: cancellationToken);
    }

    private async Task WaitUntilCompletionAsync(IResource resource, IResource dependency, int exitCode, CancellationToken cancellationToken)
    {
        if (dependency.TryGetLastAnnotation<ReplicaAnnotation>(out var replicaAnnotation) && replicaAnnotation.Replicas > 1)
        {
            throw new DistributedApplicationException("WaitForCompletion cannot be used with resources that have replicas.");
        }

        var resourceLogger = _resourceLoggerService.GetLogger(resource);
        resourceLogger.LogInformation("Waiting for resource '{Name}' to complete.", dependency.Name);

        await PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);
        var resourceEvent = await WaitForResourceAsync(dependency.Name, re => IsKnownTerminalState(re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);
        var snapshot = resourceEvent.Snapshot;

        if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
        {
            resourceLogger.LogError(
                "Dependency resource '{ResourceName}' failed to start.",
                dependency.Name
                );

            throw new DistributedApplicationException($"Dependency resource '{dependency.Name}' failed to start.");
        }
        else if ((snapshot.State!.Text == KnownResourceStates.Finished || snapshot.State!.Text == KnownResourceStates.Exited) && snapshot.ExitCode is not null && snapshot.ExitCode != exitCode)
        {
            resourceLogger.LogError(
                "Resource '{ResourceName}' has entered the '{State}' state with exit code '{ExitCode}' expected '{ExpectedExitCode}'.",
                dependency.Name,
                snapshot.State.Text,
                snapshot.ExitCode,
                exitCode
                );

            throw new DistributedApplicationException(
                $"Resource '{dependency.Name}' has entered the '{snapshot.State.Text}' state with exit code '{snapshot.ExitCode}', expected '{exitCode}'."
                );
        }

        resourceLogger.LogInformation("Finished waiting for resource '{Name}'.", dependency.Name);

        static bool IsKnownTerminalState(CustomResourceSnapshot snapshot) =>
            KnownResourceStates.TerminalStates.Contains(snapshot.State?.Text) ||
            snapshot.ExitCode is not null;
    }

    /// <summary>
    /// Waits for all dependencies of the resource to be ready.
    /// </summary>
    /// <param name="resource">The resource with dependencies to wait for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public async Task WaitForDependenciesAsync(IResource resource, CancellationToken cancellationToken)
    {
        if (!resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
        {
            return;
        }

        var pendingDependencies = new List<Task>();
        foreach (var waitAnnotation in waitAnnotations)
        {
            var pendingDependency = waitAnnotation.WaitType switch
            {
                WaitType.WaitUntilHealthy => WaitUntilHealthyAsync(resource, waitAnnotation.Resource, cancellationToken),
                WaitType.WaitForCompletion => WaitUntilCompletionAsync(resource, waitAnnotation.Resource, waitAnnotation.ExitCode, cancellationToken),
                _ => throw new DistributedApplicationException($"Unexpected wait type: {waitAnnotation.WaitType}")
            };
            pendingDependencies.Add(pendingDependency);
        }

        await Task.WhenAll(pendingDependencies).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits until a resource satisfies the specified predicate.
    /// </summary>
    /// <remarks>
    /// This method returns a task that will complete when the specified predicate returns <see langword="true" />.<br/>
    /// If the predicate isn't satisfied before <paramref name="cancellationToken"/> is signaled, this method
    /// will throw <see cref="OperationCanceledException"/>.
    /// </remarks>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="predicate">A predicate which is evaluated for each <see cref="ResourceEvent"/> for the selected resource.</param>
    /// <param name="cancellationToken">A cancellation token that cancels the wait operation when signaled.</param>
    /// <returns>A <see cref="Task{ResourceEvent}"/> representing the wait operation and which of the target states the resource reached.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
                                                     Justification = "predicate and targetState(s) parameters are mutually exclusive.")]
    public async Task<ResourceEvent> WaitForResourceAsync(string resourceName, Func<ResourceEvent, bool> predicate, CancellationToken cancellationToken = default)
    {
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping, cancellationToken);
        var watchToken = watchCts.Token;
        await foreach (var resourceEvent in WatchAsync(watchToken).ConfigureAwait(false))
        {
            if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName)
                && resourceEvent.Snapshot.State?.Text is { Length: > 0 } statusText
                && predicate(resourceEvent))
            {
                return resourceEvent;
            }
        }

        throw new OperationCanceledException($"The operation was cancelled before the resource met the predicate condition.");
    }

    private readonly object _onResourceUpdatedLock = new();

    /// <summary>
    /// Watch for changes to the state for all resources.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<ResourceEvent> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Return the last snapshot for each resource.
        foreach (var state in _resourceNotificationStates)
        {
            var (resource, resourceId) = state.Key;

            if (state.Value.LastSnapshot is not null)
            {
                yield return new ResourceEvent(resource, resourceId, state.Value.LastSnapshot);
            }
        }

        var channel = Channel.CreateUnbounded<ResourceEvent>();

        void WriteToChannel(ResourceEvent resourceEvent) =>
            channel.Writer.TryWrite(resourceEvent);

        lock (_onResourceUpdatedLock)
        {
            OnResourceUpdated += WriteToChannel;
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            lock (_onResourceUpdatedLock)
            {
                OnResourceUpdated -= WriteToChannel;
            }

            channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="resource">The resource to update</param>
    /// <param name="resourceId"> The id of the resource.</param>
    /// <param name="stateFactory">A factory that creates the new state based on the previous state.</param>
    public Task PublishUpdateAsync(IResource resource, string resourceId, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        var notificationState = GetResourceNotificationState(resource, resourceId);

        lock (notificationState)
        {
            var previousState = GetCurrentSnapshot(resource, notificationState);

            var newState = stateFactory(previousState);

            newState = UpdateCommands(resource, newState);

            notificationState.LastSnapshot = newState;

            OnResourceUpdated?.Invoke(new ResourceEvent(resource, resourceId, newState));

            if (_logger.IsEnabled(LogLevel.Debug) && newState.State?.Text is { Length: > 0 } newStateText && !string.IsNullOrWhiteSpace(newStateText))
            {
                var previousStateText = previousState?.State?.Text;
                if (!string.IsNullOrWhiteSpace(previousStateText) && !string.Equals(previousStateText, newStateText, StringComparison.OrdinalIgnoreCase))
                {
                    // The state text has changed from the previous state
                    _logger.LogDebug("Resource {Resource}/{ResourceId} changed state: {PreviousState} -> {NewState}", resource.Name, resourceId, previousStateText, newStateText);
                }
                else if (string.IsNullOrWhiteSpace(previousStateText))
                {
                    // There was no previous state text so just log the new state
                    _logger.LogDebug("Resource {Resource}/{ResourceId} changed state: {NewState}", resource.Name, resourceId, newStateText);
                }
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Resource {Resource}/{ResourceId} update published: " +
                    "ResourceType = {ResourceType}, CreationTimeStamp = {CreationTimeStamp:s}, State = {{ Text = {StateText}, Style = {StateStyle} }}, " +
                    "HealthStatus = {HealthStatus} " +
                    "ExitCode = {ExitCode}, EnvironmentVariables = {{ {EnvironmentVariables} }}, Urls = {{ {Urls} }}, " +
                    "Properties = {{ {Properties} }}",
                    resource.Name, resourceId,
                    newState.ResourceType, newState.CreationTimeStamp, newState.State?.Text, newState.State?.Style, newState.HealthStatus,
                    newState.ExitCode, string.Join(", ", newState.EnvironmentVariables.Select(e => $"{e.Name} = {e.Value}")), string.Join(", ", newState.Urls.Select(u => $"{u.Name} = {u.Url}")),
                    string.Join(", ", newState.Properties.Select(p => $"{p.Name} = {p.Value}")));
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Use command annotations to update resource snapshot.
    /// </summary>
    private CustomResourceSnapshot UpdateCommands(IResource resource, CustomResourceSnapshot previousState)
    {
        ImmutableArray<ResourceCommandSnapshot>.Builder? builder = null;

        foreach (var annotation in resource.Annotations.OfType<ResourceCommandAnnotation>())
        {
            var existingCommand = FindByType(previousState.Commands, annotation.Type);

            if (existingCommand == null)
            {
                if (builder == null)
                {
                    builder = ImmutableArray.CreateBuilder<ResourceCommandSnapshot>(previousState.Commands.Length);
                    builder.AddRange(previousState.Commands);
                }

                // Command doesn't exist in snapshot. Create from annotation.
                builder.Add(CreateCommandFromAnnotation(annotation, previousState, _serviceProvider));
            }
            else
            {
                // Command already exists in snapshot. Update its state based on annotation callback.
                var newState = annotation.UpdateState(new UpdateCommandStateContext { ResourceSnapshot = previousState, ServiceProvider = _serviceProvider });

                if (existingCommand.State != newState)
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<ResourceCommandSnapshot>(previousState.Commands.Length);
                        builder.AddRange(previousState.Commands);
                    }

                    var newCommand = existingCommand with
                    {
                        State = newState
                    };

                    builder.Replace(existingCommand, newCommand);
                }
            }
        }

        // Commands are unchanged. Return unchanged state.
        if (builder == null)
        {
            return previousState;
        }

        return previousState with { Commands = builder.ToImmutable() };

        static ResourceCommandSnapshot? FindByType(ImmutableArray<ResourceCommandSnapshot> commands, string type)
        {
            for (var i = 0; i < commands.Length; i++)
            {
                if (commands[i].Type == type)
                {
                    return commands[i];
                }
            }

            return null;
        }

        static ResourceCommandSnapshot CreateCommandFromAnnotation(ResourceCommandAnnotation annotation, CustomResourceSnapshot previousState, IServiceProvider serviceProvider)
        {
            var state = annotation.UpdateState(new UpdateCommandStateContext { ResourceSnapshot = previousState, ServiceProvider = serviceProvider });

            return new ResourceCommandSnapshot(annotation.Type, state, annotation.DisplayName, annotation.DisplayDescription, annotation.Parameter, annotation.ConfirmationMessage, annotation.IconName, annotation.IconVariant, annotation.IsHighlighted);
        }
    }

    /// <summary>
    /// Updates the snapshot of the <see cref="CustomResourceSnapshot"/> for a resource.
    /// </summary>
    /// <param name="resource">The resource to update</param>
    /// <param name="stateFactory">A factory that creates the new state based on the previous state.</param>
    public async Task PublishUpdateAsync(IResource resource, Func<CustomResourceSnapshot, CustomResourceSnapshot> stateFactory)
    {
        var resourceNames = resource.GetResolvedResourceNames();
        foreach (var resourceName in resourceNames)
        {
            await PublishUpdateAsync(resource, resourceName, stateFactory).ConfigureAwait(false);
        }
    }

    private static CustomResourceSnapshot GetCurrentSnapshot(IResource resource, ResourceNotificationState notificationState)
    {
        var previousState = notificationState.LastSnapshot;

        if (previousState is null)
        {
            if (resource.Annotations.OfType<ResourceSnapshotAnnotation>().LastOrDefault() is { } annotation)
            {
                previousState = annotation.InitialSnapshot;
            }

            // If there is no initial snapshot, create an empty one.
            previousState ??= new CustomResourceSnapshot()
            {
                ResourceType = resource.GetType().Name,
                Properties = []
            };
        }

        return previousState;
    }

    private ResourceNotificationState GetResourceNotificationState(IResource resource, string resourceId) =>
        _resourceNotificationStates.GetOrAdd((resource, resourceId), _ => new ResourceNotificationState());

    /// <summary>
    /// The annotation that allows publishing and subscribing to changes in the state of a resource.
    /// </summary>
    private sealed class ResourceNotificationState
    {
        public CustomResourceSnapshot? LastSnapshot { get; set; }
    }
}

/// <summary>
/// Represents a change in the state of a resource.
/// </summary>
/// <param name="resource">The resource associated with the event.</param>
/// <param name="resourceId">The unique id of the resource.</param>
/// <param name="snapshot">The snapshot of the resource state.</param>
public class ResourceEvent(IResource resource, string resourceId, CustomResourceSnapshot snapshot)
{
    /// <summary>
    /// The resource associated with the event.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The unique id of the resource.
    /// </summary>
    public string ResourceId { get; } = resourceId;

    /// <summary>
    /// The snapshot of the resource state.
    /// </summary>
    public CustomResourceSnapshot Snapshot { get; } = snapshot;
}
