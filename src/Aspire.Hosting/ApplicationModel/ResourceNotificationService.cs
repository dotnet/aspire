// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service that allows publishing and subscribing to changes in the state of a resource.
/// </summary>
public class ResourceNotificationService : IDisposable
{
    // Resource state is keyed by the resource and the unique name of the resource. This could be the name of the resource, or a replica ID.
    private readonly ConcurrentDictionary<(IResource, string), ResourceNotificationState> _resourceNotificationStates = new();
    private readonly ILogger<ResourceNotificationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _disposing = new();
    private readonly ResourceLoggerService _resourceLoggerService;

    private Action<ResourceEvent>? OnResourceUpdated { get; set; }

    // This is for testing
    internal WaitBehavior DefaultWaitBehavior { get; set; }

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
        _resourceLoggerService = new ResourceLoggerService();
        DefaultWaitBehavior = WaitBehavior.StopOnResourceUnavailable;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ResourceNotificationService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="hostApplicationLifetime">The host application lifetime.</param>
    /// <param name="resourceLoggerService">The resource logger service.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ResourceNotificationService(
        ILogger<ResourceNotificationService> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IServiceProvider serviceProvider,
        ResourceLoggerService resourceLoggerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider;
        _resourceLoggerService = resourceLoggerService ?? throw new ArgumentNullException(nameof(resourceLoggerService));
        DefaultWaitBehavior = serviceProvider.GetService<IOptions<ResourceNotificationServiceOptions>>()?.Value.DefaultWaitBehavior ?? WaitBehavior.StopOnResourceUnavailable;

        // The IHostApplicationLifetime parameter is not used anymore, but we keep it for backwards compatibility.
        // Notification updates will be cancelled when the service is disposed.
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
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Waiting for resource '{Name}' to enter one of the target state: {TargetStates}", resourceName, string.Join(", ", targetStates));
        }

        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token, cancellationToken);
        var watchToken = watchCts.Token;
        await foreach (var resourceEvent in WatchAsync(watchToken).ConfigureAwait(false))
        {
            if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName)
                && resourceEvent.Snapshot.State?.Text is { Length: > 0 } stateText
                && targetStates.Contains(stateText, StringComparers.ResourceState))
            {
                _logger.LogDebug("Finished waiting for resource '{Name}'. Resource state is '{State}'.", resourceName, stateText);
                return stateText;
            }
        }

        throw new OperationCanceledException($"The operation was cancelled before the resource reached one of the target states: [{string.Join(", ", targetStates)}]");
    }

    private async Task WaitUntilHealthyAsync(IResource resource, IResource dependency, WaitBehavior waitBehavior, CancellationToken cancellationToken)
    {
        var resourceLogger = _resourceLoggerService.GetLogger(resource);
        resourceLogger.LogInformation("Waiting for resource '{Name}' to enter the '{State}' state.", dependency.Name, KnownResourceStates.Running);
        await PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);

        var names = dependency.GetResolvedResourceNames();
        var tasks = new Task[names.Length];

        for (var i = 0; i < names.Length; i++)
        {
            var displayName = names.Length > 1 ? names[i] : dependency.Name;
            tasks[i] = Core(displayName, names[i]);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        async Task Core(string displayName, string resourceId)
        {
            var resourceEvent = await WaitForResourceCoreAsync(dependency.Name, re => re.ResourceId == resourceId && IsContinuableState(waitBehavior, re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);
            var snapshot = resourceEvent.Snapshot;

            if (waitBehavior == WaitBehavior.StopOnResourceUnavailable)
            {
                if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
                {
                    resourceLogger.LogError(
                        "Dependency resource '{ResourceName}' failed to start.",
                        displayName
                        );

                    throw new DistributedApplicationException($"Dependency resource '{displayName}' failed to start.");
                }
                else if (snapshot.State!.Text == KnownResourceStates.Finished ||
                         snapshot.State.Text == KnownResourceStates.Exited ||
                         snapshot.State.Text == KnownResourceStates.RuntimeUnhealthy)
                {
                    resourceLogger.LogError(
                        "Resource '{ResourceName}' has entered the '{State}' state prematurely.",
                        displayName,
                        snapshot.State.Text
                        );

                    throw new DistributedApplicationException(
                        $"Resource '{displayName}' has entered the '{snapshot.State.Text}' state prematurely."
                        );
                }
            }

            // If our dependency resource has health check annotations we want to wait until they turn healthy
            // otherwise we don't care about their health status.
            if (dependency.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var _))
            {
                resourceLogger.LogInformation("Waiting for resource '{Name}' to become healthy.", displayName);
                await WaitForResourceCoreAsync(dependency.Name, re => re.ResourceId == resourceId && re.Snapshot.HealthStatus == HealthStatus.Healthy, cancellationToken).ConfigureAwait(false);
            }

            // Now wait for the resource ready event to be executed.
            resourceLogger.LogInformation("Waiting for resource ready to execute for '{Name}'.", displayName);
            resourceEvent = await WaitForResourceCoreAsync(dependency.Name, re => re.ResourceId == resourceId && re.Snapshot.ResourceReadyEvent is not null, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Observe the result of the resource ready event task
            await resourceEvent.Snapshot.ResourceReadyEvent!.EventTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            resourceLogger.LogInformation("Finished waiting for resource '{Name}'.", displayName);

            static bool IsContinuableState(WaitBehavior waitBehavior, CustomResourceSnapshot snapshot) =>
                waitBehavior switch
                {
                    WaitBehavior.WaitOnResourceUnavailable => snapshot.State?.Text == KnownResourceStates.Running,
                    WaitBehavior.StopOnResourceUnavailable => snapshot.State?.Text == KnownResourceStates.Running ||
                                                            snapshot.State?.Text == KnownResourceStates.Finished ||
                                                            snapshot.State?.Text == KnownResourceStates.Exited ||
                                                            snapshot.State?.Text == KnownResourceStates.FailedToStart ||
                                                            snapshot.State?.Text == KnownResourceStates.RuntimeUnhealthy,
                    _ => throw new DistributedApplicationException($"Unexpected wait behavior: {waitBehavior}")
                };
        }
    }

    /// <summary>
    /// Waits for a resource to become healthy.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    /// <remarks>
    /// <para>
    /// This method returns a task that will complete with the resource is healthy. A resource
    /// without <see cref="HealthCheckAnnotation"/> annotations will be considered healthy once
    /// it reaches a <see cref="KnownResourceStates.Running"/> state.
    /// </para>
    /// <para>
    /// If the resource enters an unavailable state such as <see cref="KnownResourceStates.FailedToStart"/> then
    /// this method will continue to wait to enable scenarios where a resource could be restarted and recover. To
    /// control this behavior use <see cref="WaitForResourceHealthyAsync(string, WaitBehavior, CancellationToken)"/>
    /// or configure the default behavior with <see cref="ResourceNotificationServiceOptions.DefaultWaitBehavior"/>.
    /// </para>
    /// </remarks>
    public async Task<ResourceEvent> WaitForResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        return await WaitForResourceHealthyAsync(
            resourceName,
            DefaultWaitBehavior,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for a resource to become healthy.
    /// </summary>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="waitBehavior">The wait behavior.</param>
    /// <returns>A task.</returns>
    /// <remarks>
    /// <para>
    /// This method returns a task that will complete with the resource is healthy. A resource
    /// without <see cref="HealthCheckAnnotation"/> annotations will be considered healthy once
    /// it reaches a <see cref="KnownResourceStates.Running"/> state. The
    /// <see cref="WaitBehavior"/> controls how the wait operation behaves when the resource
    /// enters an unavailable state such as <see cref="KnownResourceStates.FailedToStart"/>.
    /// </para>
    /// <para>
    /// When <see cref="WaitBehavior.WaitOnResourceUnavailable"/> is specified the wait operation
    /// will continue to wait until the resource reaches a <see cref="KnownResourceStates.Running"/> state.
    /// </para>
    /// <para>
    /// When <see cref="WaitBehavior.StopOnResourceUnavailable"/> is specified the wait operation
    /// will throw a <see cref="DistributedApplicationException"/> if the resource enters an
    /// unavailable state.
    /// </para>
    /// </remarks>
    public async Task<ResourceEvent> WaitForResourceHealthyAsync(string resourceName, WaitBehavior waitBehavior, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Waiting for resource '{Name}' to enter the '{State}' state.", resourceName, HealthStatus.Healthy);
        var resourceEvent = await WaitForResourceCoreAsync(resourceName, re => ShouldYield(waitBehavior, re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);

        if (resourceEvent.Snapshot.HealthStatus != HealthStatus.Healthy)
        {
            _logger.LogError("Stopped waiting for resource '{ResourceName}' to become healthy because it failed to start.", resourceName);
            throw new DistributedApplicationException($"Stopped waiting for resource '{resourceName}' to become healthy because it failed to start.");
        }

        _logger.LogDebug("Finished waiting for resource '{Name}'.", resourceName);

        return resourceEvent;

        // Determine if we should yield based on the wait behavior and the snapshot of the resource.
        static bool ShouldYield(WaitBehavior waitBehavior, CustomResourceSnapshot snapshot) =>
            waitBehavior switch
            {
                WaitBehavior.WaitOnResourceUnavailable => snapshot.HealthStatus == HealthStatus.Healthy,
                WaitBehavior.StopOnResourceUnavailable => snapshot.HealthStatus == HealthStatus.Healthy ||
                                                      snapshot.State?.Text == KnownResourceStates.Finished ||
                                                      snapshot.State?.Text == KnownResourceStates.Exited ||
                                                      snapshot.State?.Text == KnownResourceStates.FailedToStart ||
                                                      snapshot.State?.Text == KnownResourceStates.RuntimeUnhealthy,
                _ => throw new DistributedApplicationException($"Unexpected wait behavior: {waitBehavior}")
            };
    }

    private async Task WaitUntilCompletionAsync(IResource resource, IResource dependency, int exitCode, CancellationToken cancellationToken)
    {
        var names = dependency.GetResolvedResourceNames();
        var tasks = new Task[names.Length];

        var resourceLogger = _resourceLoggerService.GetLogger(resource);
        resourceLogger.LogInformation("Waiting for resource '{Name}' to complete.", dependency.Name);

        await PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);

        for (var i = 0; i < names.Length; i++)
        {
            var displayName = names.Length > 1 ? names[i] : dependency.Name;
            tasks[i] = Core(displayName, names[i]);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        async Task Core(string displayName, string resourceId)
        {
            var resourceEvent = await WaitForResourceCoreAsync(dependency.Name, re => re.ResourceId == resourceId && IsKnownTerminalState(re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);
            var snapshot = resourceEvent.Snapshot;

            if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
            {
                resourceLogger.LogError(
                    "Dependency resource '{ResourceName}' failed to start.",
                    displayName
                    );

                throw new DistributedApplicationException($"Dependency resource '{displayName}' failed to start.");
            }
            else if ((snapshot.State!.Text == KnownResourceStates.Finished || snapshot.State!.Text == KnownResourceStates.Exited) && snapshot.ExitCode is not null && snapshot.ExitCode != exitCode)
            {
                resourceLogger.LogError(
                    "Resource '{ResourceName}' has entered the '{State}' state with exit code '{ExitCode}' expected '{ExpectedExitCode}'.",
                    displayName,
                    snapshot.State.Text,
                    snapshot.ExitCode,
                    exitCode
                    );

                throw new DistributedApplicationException(
                    $"Resource '{displayName}' has entered the '{snapshot.State.Text}' state with exit code '{snapshot.ExitCode}', expected '{exitCode}'."
                    );
            }

            resourceLogger.LogInformation("Finished waiting for resource '{Name}'.", displayName);

            static bool IsKnownTerminalState(CustomResourceSnapshot snapshot) =>
                KnownResourceStates.TerminalStates.Contains(snapshot.State?.Text) ||
                snapshot.ExitCode is not null;
        }
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
            if (waitAnnotation.Resource is IResourceWithoutLifetime)
            {
                // IResourceWithoutLifetime are inert and don't need to be waited on.
                continue;
            }

            var pendingDependency = waitAnnotation.WaitType switch
            {
                WaitType.WaitUntilHealthy => WaitUntilHealthyAsync(resource, waitAnnotation.Resource, waitAnnotation.WaitBehavior ?? DefaultWaitBehavior, cancellationToken),
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
        _logger.LogDebug("Waiting for resource '{Name}' to match predicate.", resourceName);
        var resourceEvent = await WaitForResourceCoreAsync(resourceName, predicate, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Finished waiting for resource '{Name}'.", resourceName);

        return resourceEvent;
    }

    private async Task<ResourceEvent> WaitForResourceCoreAsync(string resourceName, Func<ResourceEvent, bool> predicate, CancellationToken cancellationToken = default)
    {
        using var watchCts = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token, cancellationToken);
        var watchToken = watchCts.Token;
        await foreach (var resourceEvent in WatchAsync(watchToken).ConfigureAwait(false))
        {
            if (string.Equals(resourceName, resourceEvent.Resource.Name, StringComparisons.ResourceName) && predicate(resourceEvent))
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
        var channel = Channel.CreateUnbounded<ResourceEvent>();

        void WriteToChannel(ResourceEvent resourceEvent) =>
            channel.Writer.TryWrite(resourceEvent);

        lock (_onResourceUpdatedLock)
        {
            OnResourceUpdated += WriteToChannel;
        }

        // Return the last snapshot for each resource.
        // We do this after subscribing to the event to avoid missing any updates.

        // Keep track of the versions we have seen so far to avoid duplicates.
        var versionsSeen = new Dictionary<(IResource, string), long>();

        foreach (var state in _resourceNotificationStates)
        {
            var (resource, resourceId) = state.Key;

            if (state.Value.LastSnapshot is { } snapshot)
            {
                versionsSeen[state.Key] = snapshot.Version;

                yield return new ResourceEvent(resource, resourceId, snapshot);
            }
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // Skip events that are older than the max version we have seen so far. This avoids duplicates.
                if (versionsSeen.TryGetValue((item.Resource, item.ResourceId), out var maxVersionSeen) && item.Snapshot.Version <= maxVersionSeen)
                {
                    // We can remove the version from the seen list since we have seen it already.
                    // We only care about events we have returned to the caller
                    versionsSeen.Remove((item.Resource, item.ResourceId));
                    continue;
                }

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

            // Increment the snapshot version, this is a per resource version.
            newState = newState with { Version = notificationState.GetNextVersion() };

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
                // This is all logged on a single line so that logs have a single event on a single line, which
                // makes them more easily analyzed in a text editor
                _logger.LogTrace(
                    "Version: {Version} " +
                    "Resource {Resource}/{ResourceId} update published: " +
                    "ResourceType = {ResourceType}, " +
                    "CreationTimeStamp = {CreationTimeStamp:s}, " +
                    "State = {{ Text = {StateText}, Style = {StateStyle} }}, " +
                    "IsHidden = {IsHidden}, " +
                    "HeathStatus = {HealthStatus}, " +
                    "ResourceReady = {ResourceReady}, " +
                    "ExitCode = {ExitCode}, " +
                    "Urls = {{ {Urls} }}, " +
                    "EnvironmentVariables = {{ {EnvironmentVariables} }}, " +
                    "Properties = {{ {Properties} }}, " +
                    "HealthReports = {{ {HealthReports} }}, " +
                    "Commands = {{ {Commands} }}",
                    newState.Version,
                    resource.Name,
                    resourceId,
                    newState.ResourceType,
                    newState.CreationTimeStamp,
                    newState.State?.Text,
                    newState.State?.Style,
                    newState.IsHidden,
                    newState.HealthStatus,
                    newState.ResourceReadyEvent is not null,
                    newState.ExitCode,
                    string.Join(" ", newState.Urls.Select(u => $"{u.Name} = {u.Url}")),
                    string.Join(" ", newState.EnvironmentVariables.Where(e => e.IsFromSpec).Select(e => $"{e.Name} = {e.Value}")),
                    string.Join(" ", newState.Properties.Select(p => $"{p.Name} = {Stringify(p.Value)}")),
                    string.Join(" ", newState.HealthReports.Select(p => $"{p.Name} = {Stringify(p.Status)}")),
                    string.Join(" ", newState.Commands.Select(c => $"{c.Name} ({c.DisplayName}) = {Stringify(c.State)}")));

                static string Stringify(object? o) => o switch
                {
                    IEnumerable<int> ints => string.Join(", ", ints.Select(i => i.ToString(CultureInfo.InvariantCulture))),
                    IEnumerable<string> strings => string.Join(", ", strings.Select(s => s)),
                    null => "(null)",
                    _ => o.ToString()!
                };
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
            var existingCommand = FindByName(previousState.Commands, annotation.Name);

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

        static ResourceCommandSnapshot? FindByName(ImmutableArray<ResourceCommandSnapshot> commands, string name)
        {
            for (var i = 0; i < commands.Length; i++)
            {
                if (commands[i].Name == name)
                {
                    return commands[i];
                }
            }

            return null;
        }

        static ResourceCommandSnapshot CreateCommandFromAnnotation(ResourceCommandAnnotation annotation, CustomResourceSnapshot previousState, IServiceProvider serviceProvider)
        {
            var state = annotation.UpdateState(new UpdateCommandStateContext { ResourceSnapshot = previousState, ServiceProvider = serviceProvider });

            return new ResourceCommandSnapshot(annotation.Name, state, annotation.DisplayName, annotation.DisplayDescription, annotation.Parameter, annotation.ConfirmationMessage, annotation.IconName, annotation.IconVariant, annotation.IsHighlighted);
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
                Properties = [],
                Relationships = ResourceSnapshotBuilder.BuildRelationships(resource)
            };

            previousState = previousState with
            {
                SupportsDetailedTelemetry = IsMicrosoftOpenType(resource.GetType())
            };
        }

        return previousState;
    }

    private ResourceNotificationState GetResourceNotificationState(IResource resource, string resourceId) =>
        _resourceNotificationStates.GetOrAdd((resource, resourceId), _ => new ResourceNotificationState());

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposing.Cancel();
    }

    /// <summary>
    /// The annotation that allows publishing and subscribing to changes in the state of a resource.
    /// </summary>
    private sealed class ResourceNotificationState
    {
        private long _lastVersion = 1;
        public long GetNextVersion() => _lastVersion++;
        public CustomResourceSnapshot? LastSnapshot { get; set; }
    }

    internal static bool IsMicrosoftOpenType(Type type)
    {
        var microsoftOpenPublicKey = new byte[]
        {
            0, 36, 0, 0, 4, 128, 0, 0, 148, 0, 0, 0, 6, 2, 0, 0, 0, 36, 0, 0, 82, 83, 65, 49, 0, 4, 0, 0, 1, 0, 1,
            0, 75, 134, 196, 203, 120, 84, 155, 52, 186, 182, 26, 59, 24, 0, 226, 59, 254, 181, 179, 236, 57, 0,
            116, 4, 21, 54, 167, 227, 203, 217, 127, 95, 4, 207, 15, 133, 113, 85, 168, 146, 142, 170, 41, 235, 253,
            17, 207, 187, 173, 59, 167, 14, 254, 167, 189, 163, 34, 108, 106, 141, 55, 10, 76, 211, 3, 247, 20, 72,
            107, 110, 188, 34, 89, 133, 166, 56, 71, 30, 110, 245, 113, 204, 146, 164, 97, 60, 0, 184, 250, 101,
            214, 28, 206, 224, 203, 229, 243, 99, 48, 201, 160, 31, 65, 131, 85, 159, 27, 239, 36, 204, 41, 23, 198,
            217, 19, 227, 165, 65, 51, 58, 29, 5, 217, 190, 210, 43, 56, 203
        };

        var publicKey = type.Assembly.GetName().GetPublicKey();
        return publicKey is not null && microsoftOpenPublicKey.SequenceEqual(publicKey);
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

/// <summary>
/// Options for the <see cref="ResourceNotificationService"/>.
/// </summary>
public sealed class ResourceNotificationServiceOptions
{
    /// <summary>
    /// The default behavior to use when waiting for dependencies.
    /// </summary>
    public WaitBehavior DefaultWaitBehavior { get; set; }
}
