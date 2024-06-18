// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Aspire.Hosting.Dcp;

internal class AppResource
{
    public IResource ModelResource { get; }
    public CustomResource DcpResource { get; }
    public virtual List<ServiceAppResource> ServicesProduced { get; } = [];
    public virtual List<ServiceAppResource> ServicesConsumed { get; } = [];

    public AppResource(IResource modelResource, CustomResource dcpResource)
    {
        ModelResource = modelResource;
        DcpResource = dcpResource;
    }
}

internal sealed class ServiceAppResource : AppResource
{
    public Service Service => (Service)DcpResource;
    public EndpointAnnotation EndpointAnnotation { get; }

    public override List<ServiceAppResource> ServicesProduced
    {
        get { throw new InvalidOperationException("Service resources do not produce any services"); }
    }
    public override List<ServiceAppResource> ServicesConsumed
    {
        get { throw new InvalidOperationException("Service resources do not consume any services"); }
    }

    public ServiceAppResource(IResource modelResource, Service service, EndpointAnnotation sba) : base(modelResource, service)
    {
        EndpointAnnotation = sba;
    }
}

internal sealed class ApplicationExecutor(ILogger<ApplicationExecutor> logger,
                                          ILogger<DistributedApplication> distributedApplicationLogger,
                                          DistributedApplicationModel model,
                                          IKubernetesService kubernetesService,
                                          IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
                                          IConfiguration configuration,
                                          DistributedApplicationOptions distributedApplicationOptions,
                                          IOptions<DcpOptions> options,
                                          DistributedApplicationExecutionContext executionContext,
                                          ResourceNotificationService notificationService,
                                          ResourceLoggerService loggerService,
                                          IDcpDependencyCheckService dcpDependencyCheckService)
{
    private const string DebugSessionPortVar = "DEBUG_SESSION_PORT";
    private const int RandomNameSuffixLength = 8;

    private readonly ILogger<ApplicationExecutor> _logger = logger;
    private readonly DistributedApplicationModel _model = model;
    private readonly Dictionary<string, IResource> _applicationModel = model.Resources.ToDictionary(r => r.Name);
    private readonly ILookup<IResource?, IResourceWithParent> _parentChildLookup = GetParentChildLookup(model);
    private readonly IDistributedApplicationLifecycleHook[] _lifecycleHooks = lifecycleHooks.ToArray();
    private readonly DistributedApplicationOptions _distributedApplicationOptions = distributedApplicationOptions;
    private readonly IOptions<DcpOptions> _options = options;
    private readonly DistributedApplicationExecutionContext _executionContext = executionContext;
    private readonly List<AppResource> _appResources = [];
    private readonly CancellationTokenSource _shutdownCancellation = new();

    private readonly ConcurrentDictionary<string, Container> _containersMap = [];
    private readonly ConcurrentDictionary<string, Executable> _executablesMap = [];
    private readonly ConcurrentDictionary<string, Service> _servicesMap = [];
    private readonly ConcurrentDictionary<string, Endpoint> _endpointsMap = [];
    private readonly ConcurrentDictionary<(string, string), List<string>> _resourceAssociatedServicesMap = [];
    private readonly ConcurrentDictionary<string, (CancellationTokenSource Cancellation, Task Task)> _logStreams = new();
    private readonly ConcurrentDictionary<IResource, bool> _hiddenResources = new();
    private DcpInfo? _dcpInfo;
    private Task? _resourceWatchTask;

    private readonly record struct LogInformationEntry(string ResourceName, bool? LogsAvailable, bool? HasSubscribers);
    private readonly Channel<LogInformationEntry> _logInformationChannel = Channel.CreateUnbounded<LogInformationEntry>(
        new UnboundedChannelOptions { SingleReader = true });

    private string DefaultContainerHostName => configuration["AppHost:ContainerHostname"] ?? _dcpInfo?.Containers?.ContainerHostName ?? "host.docker.internal";

    public async Task RunApplicationAsync(CancellationToken cancellationToken = default)
    {
        AspireEventSource.Instance.DcpModelCreationStart();

        _dcpInfo = await dcpDependencyCheckService.GetDcpInfoAsync(cancellationToken).ConfigureAwait(false);

        Debug.Assert(_dcpInfo is not null, "DCP info should not be null at this point");

        try
        {
            PrepareServices();
            PrepareContainers();
            PrepareExecutables();

            await PublishResourcesWithInitialStateAsync().ConfigureAwait(false);

            // Watch for changes to the resource state.
            WatchResourceChanges();

            await CreateServicesAsync(cancellationToken).ConfigureAwait(false);

            await CreateContainersAndExecutablesAsync(cancellationToken).ConfigureAwait(false);

            foreach (var lifecycleHook in _lifecycleHooks)
            {
                await lifecycleHook.AfterResourcesCreatedAsync(_model, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            _shutdownCancellation.Cancel();
            throw;
        }
        finally
        {
            AspireEventSource.Instance.DcpModelCreationStop();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCancellation.Cancel();
        var tasks = new List<Task>();
        if (_resourceWatchTask is { } resourceTask)
        {
            tasks.Add(resourceTask);
        }

        foreach (var (_, (cancellation, logTask)) in _logStreams)
        {
            cancellation.Cancel();
            tasks.Add(logTask);
        }

        try
        {
            await Task.WhenAll(tasks).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "One or more monitoring tasks terminated with an error.");
        }
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

    // Sets the state of the resource's children
    async Task SetChildResourceStateAsync(IResource resource, string state)
    {
        foreach (var child in _parentChildLookup[resource])
        {
            await notificationService.PublishUpdateAsync(child, s => s with
            {
                State = state
            })
            .ConfigureAwait(false);
        }
    }

    private async Task PublishResourcesWithInitialStateAsync()
    {
        // Publish the initial state of the resources that have a snapshot annotation.
        foreach (var resource in _model.Resources)
        {
            await notificationService.PublishUpdateAsync(resource, s => s).ConfigureAwait(false);
        }
    }

    private void WatchResourceChanges()
    {
        var outputSemaphore = new SemaphoreSlim(1);

        var cancellationToken = _shutdownCancellation.Token;
        var watchResourcesTask = Task.Run(async () =>
        {
            using (outputSemaphore)
            {
                await Task.WhenAll(
                    Task.Run(() => WatchKubernetesResourceAsync<Executable>((t, r) => ProcessResourceChange(t, r, _executablesMap, "Executable", ToSnapshot))),
                    Task.Run(() => WatchKubernetesResourceAsync<Container>((t, r) => ProcessResourceChange(t, r, _containersMap, "Container", ToSnapshot))),
                    Task.Run(() => WatchKubernetesResourceAsync<Service>(ProcessServiceChange)),
                    Task.Run(() => WatchKubernetesResourceAsync<Endpoint>(ProcessEndpointChange))).ConfigureAwait(false);
            }
        });

        var watchSubscribersTask = Task.Run(async () =>
        {
            await foreach (var subscribers in loggerService.WatchAnySubscribersAsync(cancellationToken).ConfigureAwait(false))
            {
                _logInformationChannel.Writer.TryWrite(new(subscribers.Name, LogsAvailable: null, subscribers.AnySubscribers));
            }
        });

        // Listen to the "log information channel" - which contains updates when resources have logs available and when they have subscribers.
        // A resource needs both logs available and subscribers before it starts streaming its logs.
        // We only want to start the log stream for resources when they have subscribers.
        // And when there are no more subscribers, we want to stop the stream.
        var watchInformationChannelTask = Task.Run(async () =>
        {
            var resourceLogState = new Dictionary<string, (bool logsAvailable, bool hasSubscribers)>();

            await foreach (var entry in _logInformationChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var logsAvailable = false;
                var hasSubscribers = false;
                if (resourceLogState.TryGetValue(entry.ResourceName, out (bool, bool) stateEntry))
                {
                    (logsAvailable, hasSubscribers) = stateEntry;
                }

                // LogsAvailable can only go from false => true. Once it is true, it can never go back to false.
                Debug.Assert(!entry.LogsAvailable.HasValue || entry.LogsAvailable.Value, "entry.LogsAvailable should never be 'false'");

                logsAvailable = entry.LogsAvailable ?? logsAvailable;
                hasSubscribers = entry.HasSubscribers ?? hasSubscribers;

                if (logsAvailable)
                {
                    if (hasSubscribers)
                    {
                        if (_containersMap.TryGetValue(entry.ResourceName, out var container))
                        {
                            StartLogStream(container);
                        }
                        else if (_executablesMap.TryGetValue(entry.ResourceName, out var executable))
                        {
                            StartLogStream(executable);
                        }
                    }
                    else
                    {
                        if (_logStreams.TryRemove(entry.ResourceName, out var logStream))
                        {
                            logStream.Cancellation.Cancel();
                        }

                        if (_containersMap.TryGetValue(entry.ResourceName, out var _) ||
                            _executablesMap.TryGetValue(entry.ResourceName, out var _))
                        {
                            // Clear out the backlog for containers and executables after the last subscriber leaves.
                            // When a new subscriber is added, the full log will be replayed.
                            loggerService.ClearBacklog(entry.ResourceName);
                        }
                    }
                }

                resourceLogState[entry.ResourceName] = (logsAvailable, hasSubscribers);
            }
        });

        _resourceWatchTask = Task.WhenAll(watchResourcesTask, watchSubscribersTask, watchInformationChannelTask);

        async Task WatchKubernetesResourceAsync<T>(Func<WatchEventType, T, Task> handler) where T : CustomResource
        {
            var retryUntilCancelled = new RetryStrategyOptions()
            {
                ShouldHandle = new PredicateBuilder().HandleInner<EndOfStreamException>(),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = int.MaxValue,
                UseJitter = true,
                MaxDelay = TimeSpan.FromSeconds(30),
                OnRetry = (retry) =>
                {
                    _logger.LogDebug(
                        retry.Outcome.Exception,
                        "Long poll watch operation was ended by server after {LongPollDurationInMs} milliseconds (iteration {Iteration}).",
                        retry.Duration.TotalMilliseconds,
                        retry.AttemptNumber
                        );
                    return ValueTask.CompletedTask;
                }
            };

            var pipeline = new ResiliencePipelineBuilder().AddRetry(retryUntilCancelled).Build();

            try
            {
                _logger.LogDebug("Watching over DCP {ResourceType} resources.", typeof(T).Name);
                await pipeline.ExecuteAsync(async (pipelineCancellationToken) =>
                {
                    await foreach (var (eventType, resource) in kubernetesService.WatchAsync<T>(cancellationToken: pipelineCancellationToken).ConfigureAwait<(global::k8s.WatchEventType, T)>(false))
                    {
                        await outputSemaphore.WaitAsync(pipelineCancellationToken).ConfigureAwait(false);

                        try
                        {
                            await handler(eventType, resource).ConfigureAwait(false);
                        }
                        finally
                        {
                            outputSemaphore.Release();
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutdown requested.
                _logger.LogDebug("Cancellation received while watching {ResourceType} resources.", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Watch task over Kubernetes {ResourceType} resources terminated unexpectedly. Check to ensure dcpd process is running.", typeof(T).Name);
            }
            finally
            {
                _logger.LogDebug("Stopped watching {ResourceType} resources.", typeof(T).Name);
            }
        }
    }

    private async Task ProcessResourceChange<T>(WatchEventType watchEventType, T resource, ConcurrentDictionary<string, T> resourceByName, string resourceKind, Func<T, CustomResourceSnapshot, CustomResourceSnapshot> snapshotFactory) where T : CustomResource
    {
        if (ProcessResourceChange(resourceByName, watchEventType, resource))
        {
            UpdateAssociatedServicesMap();

            var changeType = watchEventType switch
            {
                WatchEventType.Added or WatchEventType.Modified => ResourceSnapshotChangeType.Upsert,
                WatchEventType.Deleted => ResourceSnapshotChangeType.Delete,
                _ => throw new System.ComponentModel.InvalidEnumArgumentException($"Cannot convert {nameof(WatchEventType)} with value {watchEventType} into enum of type {nameof(ResourceSnapshotChangeType)}.")
            };

            // Find the associated application model resource and update it.
            var resourceName = resource.AppModelResourceName;

            if (resourceName is not null &&
                _applicationModel.TryGetValue(resourceName, out var appModelResource))
            {
                if (changeType == ResourceSnapshotChangeType.Delete)
                {
                    // Stop the log stream for the resource
                    if (_logStreams.TryRemove(resource.Metadata.Name, out var logStream))
                    {
                        logStream.Cancellation.Cancel();
                    }

                    // Complete the log stream
                    loggerService.Complete(resource.Metadata.Name);

                    // TODO: Handle resource deletion
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Deleting application model resource {ResourceName} with {ResourceKind} resource {ResourceName}", appModelResource.Name, resourceKind, resource.Metadata.Name);
                    }
                }
                else
                {
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Updating application model resource {ResourceName} with {ResourceKind} resource {ResourceName}", appModelResource.Name, resourceKind, resource.Metadata.Name);
                    }

                    if (_hiddenResources.TryAdd(appModelResource, true))
                    {
                        // Hide the application model resource because we have the DCP resource
                        await notificationService.PublishUpdateAsync(appModelResource, s => s with { State = "Hidden" }).ConfigureAwait(false);
                    }

                    // Notifications are associated with the application model resource, so we need to update with that context
                    await notificationService.PublishUpdateAsync(appModelResource, resource.Metadata.Name, s => snapshotFactory(resource, s)).ConfigureAwait(false);

                    if (resource is Container { LogsAvailable: true } ||
                        resource is Executable { LogsAvailable: true })
                    {
                        _logInformationChannel.Writer.TryWrite(new(resource.Metadata.Name, LogsAvailable: true, HasSubscribers: null));
                    }
                }

                // Update all child resources of containers
                if (resource is Container c && c.Status?.State is string state)
                {
                    await SetChildResourceStateAsync(appModelResource, state).ConfigureAwait(false);
                }
            }
            else
            {
                // No application model resource found for the DCP resource.
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("No application model resource found for {ResourceKind} resource {ResourceName}", resourceKind, resource.Metadata.Name);
                }
            }
        }

        void UpdateAssociatedServicesMap()
        {
            // We keep track of associated services for the resource
            // So whenever we get the service we can figure out if the service can generate endpoint for the resource
            if (watchEventType == WatchEventType.Deleted)
            {
                _resourceAssociatedServicesMap.Remove((resourceKind, resource.Metadata.Name), out _);
            }
            else if (resource.Metadata.Annotations?.TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson) == true)
            {
                var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(servicesProducedAnnotationJson);
                if (serviceProducerAnnotations is not null)
                {
                    _resourceAssociatedServicesMap[(resourceKind, resource.Metadata.Name)]
                        = serviceProducerAnnotations.Select(e => e.ServiceName).ToList();
                }
            }
        }
    }

    private void StartLogStream<T>(T resource) where T : CustomResource
    {
        IAsyncEnumerable<IReadOnlyList<(string, bool)>>? enumerable = resource switch
        {
            Container c when c.LogsAvailable => new ResourceLogSource<T>(_logger, kubernetesService, resource),
            Executable e when e.LogsAvailable => new ResourceLogSource<T>(_logger, kubernetesService, resource),
            _ => null
        };

        // No way to get logs for this resource as yet
        if (enumerable is null)
        {
            return;
        }

        // This does not run concurrently for the same resource so we can safely use GetOrAdd without
        // creating multiple log streams.
        _logStreams.GetOrAdd(resource.Metadata.Name, (_) =>
        {
            var cancellation = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Starting log streaming for {ResourceName}", resource.Metadata.Name);
                    }

                    // Pump the logs from the enumerable into the logger
                    var logger = loggerService.GetLogger(resource.Metadata.Name);

                    await foreach (var batch in enumerable.WithCancellation(cancellation.Token).ConfigureAwait(false))
                    {
                        foreach (var (content, isError) in batch)
                        {
                            var level = isError ? LogLevel.Error : LogLevel.Information;
                            logger.Log(level, 0, content, null, (s, _) => s);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                    _logger.LogDebug("Log streaming for {ResourceName} was cancelled", resource.Metadata.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error streaming logs for {ResourceName}", resource.Metadata.Name);
                }
            },
            cancellation.Token);

            return (cancellation, task);
        });
    }

    private async Task ProcessEndpointChange(WatchEventType watchEventType, Endpoint endpoint)
    {
        if (!ProcessResourceChange(_endpointsMap, watchEventType, endpoint))
        {
            return;
        }

        if (endpoint.Metadata.OwnerReferences is null)
        {
            return;
        }

        foreach (var ownerReference in endpoint.Metadata.OwnerReferences)
        {
            await TryRefreshResource(ownerReference.Kind, ownerReference.Name).ConfigureAwait(false);
        }
    }

    private async Task ProcessServiceChange(WatchEventType watchEventType, Service service)
    {
        if (!ProcessResourceChange(_servicesMap, watchEventType, service))
        {
            return;
        }

        foreach (var ((resourceKind, resourceName), _) in _resourceAssociatedServicesMap.Where(e => e.Value.Contains(service.Metadata.Name)))
        {
            await TryRefreshResource(resourceKind, resourceName).ConfigureAwait(false);
        }
    }

    private async ValueTask TryRefreshResource(string resourceKind, string resourceName)
    {
        CustomResource? cr = resourceKind switch
        {
            "Container" => _containersMap.TryGetValue(resourceName, out var container) ? container : null,
            "Executable" => _executablesMap.TryGetValue(resourceName, out var executable) ? executable : null,
            _ => null
        };

        if (cr is not null)
        {
            var appModelResourceName = cr.AppModelResourceName;

            if (appModelResourceName is not null &&
                _applicationModel.TryGetValue(appModelResourceName, out var appModelResource))
            {
                await notificationService.PublishUpdateAsync(appModelResource, cr.Metadata.Name, s =>
                {
                    if (cr is Container container)
                    {
                        return ToSnapshot(container, s);
                    }
                    else if (cr is Executable exe)
                    {
                        return ToSnapshot(exe, s);
                    }
                    return s;
                })
                .ConfigureAwait(false);
            }
        }
    }

    private CustomResourceSnapshot ToSnapshot(Container container, CustomResourceSnapshot previous)
    {
        var containerId = container.Status?.ContainerId;
        var urls = GetUrls(container);

        var environment = GetEnvironmentVariables(container.Status?.EffectiveEnv ?? container.Spec.Env, container.Spec.Env);
        var state = container.AppModelInitialState == KnownResourceStates.Hidden ? KnownResourceStates.Hidden : container.Status?.State;

        return previous with
        {
            ResourceType = KnownResourceTypes.Container,
            State = state,
            // Map a container exit code of -1 (unknown) to null
            ExitCode = container.Status?.ExitCode is null or Conventions.UnknownExitCode ? null : container.Status.ExitCode,
            Properties = [
                new(KnownProperties.Container.Image, container.Spec.Image),
                new(KnownProperties.Container.Id, containerId),
                new(KnownProperties.Container.Command, container.Spec.Command),
                new(KnownProperties.Container.Args, container.Status?.EffectiveArgs ?? []),
                new(KnownProperties.Container.Ports, GetPorts()),
                new(KnownProperties.Resource.Uid, container.Metadata.Uid),
            ],
            EnvironmentVariables = environment,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Urls = urls
        };

        ImmutableArray<int> GetPorts()
        {
            if (container.Spec.Ports is null)
            {
                return [];
            }

            var ports = ImmutableArray.CreateBuilder<int>();
            foreach (var port in container.Spec.Ports)
            {
                if (port.ContainerPort != null)
                {
                    ports.Add(port.ContainerPort.Value);
                }
            }
            return ports.ToImmutable();
        }
    }

    private CustomResourceSnapshot ToSnapshot(Executable executable, CustomResourceSnapshot previous)
    {
        string? projectPath = null;

        if (executable.AppModelResourceName is not null &&
            _applicationModel.TryGetValue(executable.AppModelResourceName, out var appModelResource))
        {
            projectPath = appModelResource is ProjectResource p ? p.GetProjectMetadata().ProjectPath : null;
        }

        var state = executable.AppModelInitialState is "Hidden" ? "Hidden" : executable.Status?.State;

        var urls = GetUrls(executable);

        var environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env);

        if (projectPath is not null)
        {
            return previous with
            {
                ResourceType = KnownResourceTypes.Project,
                State = state,
                ExitCode = executable.Status?.ExitCode,
                Properties = [
                    new(KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                    new(KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                    new(KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []),
                    new(KnownProperties.Executable.Pid, executable.Status?.ProcessId),
                    new(KnownProperties.Project.Path, projectPath),
                    new(KnownProperties.Resource.Uid, executable.Metadata.Uid)
                ],
                EnvironmentVariables = environment,
                CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
                Urls = urls
            };
        }

        return previous with
        {
            ResourceType = KnownResourceTypes.Executable,
            State = state,
            ExitCode = executable.Status?.ExitCode,
            Properties = [
                new(KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                new(KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                new(KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []),
                new(KnownProperties.Executable.Pid, executable.Status?.ProcessId),
                new(KnownProperties.Resource.Uid, executable.Metadata.Uid)
            ],
            EnvironmentVariables = environment,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            Urls = urls
        };
    }

    private ImmutableArray<UrlSnapshot> GetUrls(CustomResource resource)
    {
        var name = resource.Metadata.Name;

        var urls = ImmutableArray.CreateBuilder<UrlSnapshot>();

        foreach (var (_, endpoint) in _endpointsMap)
        {
            if (endpoint.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == name) != true)
            {
                continue;
            }

            if (endpoint.Spec.ServiceName is not null &&
                _servicesMap.TryGetValue(endpoint.Spec.ServiceName, out var service) &&
                service.AppModelResourceName is string resourceName &&
                _applicationModel.TryGetValue(resourceName, out var appModelResource) &&
                appModelResource is IResourceWithEndpoints resourceWithEndpoints &&
                service.EndpointName is string endpointName)
            {
                var ep = resourceWithEndpoints.GetEndpoint(endpointName);

                if (ep.EndpointAnnotation.FromLaunchProfile &&
                    appModelResource is ProjectResource p &&
                    p.GetEffectiveLaunchProfile()?.LaunchProfile is LaunchProfile profile &&
                    profile.LaunchUrl is string launchUrl)
                {
                    // Concat the launch url from the launch profile to the urls with IsFromLaunchProfile set to true

                    string CombineUrls(string url, string launchUrl)
                    {
                        if (!launchUrl.Contains("://"))
                        {
                            // This is relative URL
                            url += $"/{launchUrl}";
                        }
                        else
                        {
                            // For absolute URL we need to update the port value if possible
                            if (profile.ApplicationUrl is string applicationUrl
                                && launchUrl.StartsWith(applicationUrl))
                            {
                                url = launchUrl.Replace(applicationUrl, url);
                            }
                        }

                        return url;
                    }

                    if (ep.IsAllocated)
                    {
                        var url = CombineUrls(ep.Url, launchUrl);

                        urls.Add(new(Name: ep.EndpointName, Url: url, IsInternal: false));
                    }
                }
                else
                {
                    if (ep.IsAllocated)
                    {
                        urls.Add(new(Name: ep.EndpointName, Url: ep.Url, IsInternal: false));
                    }
                }

                if (ep.EndpointAnnotation.IsProxied)
                {
                    var endpointString = $"{ep.Scheme}://{endpoint.Spec.Address}:{endpoint.Spec.Port}";
                    urls.Add(new(Name: $"{ep.EndpointName} target port", Url: endpointString, IsInternal: true));
                }
            }
        }

        return urls.ToImmutable();
    }

    private static ImmutableArray<EnvironmentVariableSnapshot> GetEnvironmentVariables(List<EnvVar>? effectiveSource, List<EnvVar>? specSource)
    {
        if (effectiveSource is null or { Count: 0 })
        {
            return [];
        }

        var environment = ImmutableArray.CreateBuilder<EnvironmentVariableSnapshot>(effectiveSource.Count);

        foreach (var env in effectiveSource)
        {
            if (env.Name is not null)
            {
                var isFromSpec = specSource?.Any(e => string.Equals(e.Name, env.Name, StringComparison.Ordinal)) is true or null;

                environment.Add(new(env.Name, env.Value ?? "", isFromSpec));
            }
        }

        environment.Sort((v1, v2) => string.Compare(v1.Name, v2.Name, StringComparison.Ordinal));

        return environment.ToImmutable();
    }

    private static bool ProcessResourceChange<T>(ConcurrentDictionary<string, T> map, WatchEventType watchEventType, T resource)
            where T : CustomResource
    {
        switch (watchEventType)
        {
            case WatchEventType.Added:
                map.TryAdd(resource.Metadata.Name, resource);
                break;

            case WatchEventType.Modified:
                map[resource.Metadata.Name] = resource;
                break;

            case WatchEventType.Deleted:
                map.Remove(resource.Metadata.Name, out _);
                break;

            default:
                return false;
        }

        return true;
    }

    private async Task CreateServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AspireEventSource.Instance.DcpServicesCreationStart();

            var needAddressAllocated = _appResources.OfType<ServiceAppResource>()
                .Where(sr => !sr.Service.HasCompleteAddress && sr.Service.Spec.AddressAllocationMode != AddressAllocationModes.Proxyless)
                .ToList();

            await CreateResourcesAsync<Service>(cancellationToken).ConfigureAwait(false);

            if (needAddressAllocated.Count == 0)
            {
                // No need to wait for any updates to Service objects from the orchestrator.
                return;
            }

            var withTimeout = new TimeoutStrategyOptions()
            {
                Timeout = _options.Value.ServiceStartupWatchTimeout
            };

            var tryTwice = new RetryStrategyOptions()
            {
                BackoffType = DelayBackoffType.Constant,
                MaxDelay = TimeSpan.FromSeconds(1),
                UseJitter = true,
                MaxRetryAttempts = 1,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnRetry = (retry) =>
                {
                    _logger.LogDebug(
                        retry.Outcome.Exception,
                        "Watching for service port allocation ended with an error after {WatchDurationMs} (iteration {Iteration})",
                        retry.Duration.TotalMilliseconds,
                        retry.AttemptNumber
                    );
                    return ValueTask.CompletedTask;
                }
            };

            var execution = new ResiliencePipelineBuilder().AddRetry(tryTwice).AddTimeout(withTimeout).Build();

            await execution.ExecuteAsync(async (attemptCancellationToken) =>
            {
                IAsyncEnumerable<(WatchEventType, Service)> serviceChangeEnumerator = kubernetesService.WatchAsync<Service>(cancellationToken: attemptCancellationToken);
                await foreach (var (evt, updated) in serviceChangeEnumerator.ConfigureAwait(false))
                {
                    if (evt == WatchEventType.Bookmark) { continue; } // Bookmarks do not contain any data.

                    var srvResource = needAddressAllocated.FirstOrDefault(sr => sr.Service.Metadata.Name == updated.Metadata.Name);
                    if (srvResource == null) { continue; } // This service most likely already has full address information, so it is not on needAddressAllocated list.

                    if (updated.HasCompleteAddress)
                    {
                        srvResource.Service.ApplyAddressInfoFrom(updated);
                        needAddressAllocated.Remove(srvResource);
                    }

                    if (needAddressAllocated.Count == 0)
                    {
                        return; // We are done
                    }
                }
            }, cancellationToken).ConfigureAwait(false);

            // If there are still services that need address allocated, try a final direct query in case the watch missed some updates.
            foreach (var sar in needAddressAllocated)
            {
                var dcpSvc = await kubernetesService.GetAsync<Service>(sar.Service.Metadata.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (dcpSvc.HasCompleteAddress)
                {
                    sar.Service.ApplyAddressInfoFrom(dcpSvc);
                }
                else
                {
                    distributedApplicationLogger.LogWarning("Unable to allocate a network port for service '{ServiceName}'; service may be unreachable and its clients may not work properly.", sar.Service.Metadata.Name);
                }
            }

        }
        finally
        {
            AspireEventSource.Instance.DcpServicesCreationStop();
        }
    }

    private async Task CreateContainersAndExecutablesAsync(CancellationToken cancellationToken)
    {
        var toCreate = _appResources.Where(r => r.DcpResource is Container || r.DcpResource is Executable || r.DcpResource is ExecutableReplicaSet);
        AddAllocatedEndpointInfo(toCreate);

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(_model, cancellationToken).ConfigureAwait(false);
        }

        var containersTask = CreateContainersAsync(toCreate.Where(ar => ar.DcpResource is Container), cancellationToken);
        var executablesTask = CreateExecutablesAsync(toCreate.Where(ar => ar.DcpResource is Executable || ar.DcpResource is ExecutableReplicaSet), cancellationToken);

        await Task.WhenAll(containersTask, executablesTask).ConfigureAwait(false);
    }

    private void AddAllocatedEndpointInfo(IEnumerable<AppResource> resources)
    {
        var containerHost = DefaultContainerHostName;

        foreach (var appResource in resources)
        {
            foreach (var sp in appResource.ServicesProduced)
            {
                var svc = (Service)sp.DcpResource;

                if (!svc.HasCompleteAddress && sp.EndpointAnnotation.IsProxied)
                {
                    // This should never happen; if it does, we have a bug without a workaround for th the user.
                    throw new InvalidDataException($"Service {svc.Metadata.Name} should have valid address at this point");
                }

                if (!sp.EndpointAnnotation.IsProxied && svc.AllocatedPort is null)
                {
                    throw new InvalidOperationException($"Service '{svc.Metadata.Name}' needs to specify a port for endpoint '{sp.EndpointAnnotation.Name}' since it isn't using a proxy.");
                }

                sp.EndpointAnnotation.AllocatedEndpoint = new AllocatedEndpoint(
                    sp.EndpointAnnotation,
                    sp.EndpointAnnotation.IsProxied ? svc.AllocatedAddress! : "localhost",
                    (int)svc.AllocatedPort!,
                    containerHostAddress: appResource.ModelResource.IsContainer() ? containerHost : null,
                    targetPortExpression: $$$"""{{- portForServing "{{{svc.Metadata.Name}}}" -}}""");
            }
        }
    }

    private void PrepareServices()
    {
        var serviceProducers = _model.Resources
            .Select(r => (ModelResource: r, Endpoints: r.Annotations.OfType<EndpointAnnotation>()))
            .Where(sp => sp.Endpoints.Any());

        // We need to ensure that Services have unique names (otherwise we cannot really distinguish between
        // services produced by different resources).
        HashSet<string> serviceNames = [];

        foreach (var sp in serviceProducers)
        {
            var endpoints = sp.Endpoints.ToArray();

            foreach (var endpoint in endpoints)
            {
                var candidateServiceName = endpoints.Length == 1
                    ? GetObjectNameForResource(sp.ModelResource)
                    : GetObjectNameForResource(sp.ModelResource, endpoint.Name);

                var uniqueServiceName = GenerateUniqueServiceName(serviceNames, candidateServiceName);
                var svc = Service.Create(uniqueServiceName);

                var port = _options.Value.RandomizePorts && endpoint.IsProxied ? null : endpoint.Port;
                svc.Spec.Port = port;
                svc.Spec.Protocol = PortProtocol.FromProtocolType(endpoint.Protocol);
                svc.Spec.AddressAllocationMode = endpoint.IsProxied ? AddressAllocationModes.Localhost : AddressAllocationModes.Proxyless;

                // So we can associate the service with the resource that produced it and the endpoint it represents.
                svc.Annotate(CustomResource.ResourceNameAnnotation, sp.ModelResource.Name);
                svc.Annotate(CustomResource.EndpointNameAnnotation, endpoint.Name);

                _appResources.Add(new ServiceAppResource(sp.ModelResource, svc, endpoint));
            }
        }
    }

    private void PrepareExecutables()
    {
        PrepareProjectExecutables();
        PreparePlainExecutables();
    }

    private void PreparePlainExecutables()
    {
        var modelExecutableResources = _model.GetExecutableResources();

        foreach (var executable in modelExecutableResources)
        {
            var nameSuffix = GetRandomNameSuffix();
            var exeName = GetObjectNameForResource(executable, nameSuffix);
            var exePath = executable.Command;
            var exe = Executable.Create(exeName, exePath);

            // The working directory is always relative to the app host project directory (if it exists).
            exe.Spec.WorkingDirectory = executable.WorkingDirectory;
            exe.Spec.ExecutionType = ExecutionType.Process;
            exe.Annotate(CustomResource.OtelServiceNameAnnotation, executable.Name);
            exe.Annotate(CustomResource.OtelServiceInstanceIdAnnotation, nameSuffix);
            exe.Annotate(CustomResource.ResourceNameAnnotation, executable.Name);
            SetInitialResourceState(executable, exe);

            var exeAppResource = new AppResource(executable, exe);
            AddServicesProducedInfo(executable, exe, exeAppResource);
            _appResources.Add(exeAppResource);
        }
    }

    private void PrepareProjectExecutables()
    {
        var modelProjectResources = _model.GetProjectResources();

        foreach (var project in modelProjectResources)
        {
            if (!project.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
            {
                throw new InvalidOperationException("A project resource is missing required metadata"); // Should never happen.
            }

            int replicas = project.GetReplicaCount();

            var ers = ExecutableReplicaSet.Create(GetObjectNameForResource(project), replicas, "dotnet");
            var exeSpec = ers.Spec.Template.Spec;
            exeSpec.WorkingDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);

            IAnnotationHolder annotationHolder = ers.Spec.Template;
            annotationHolder.Annotate(CustomResource.OtelServiceNameAnnotation, ers.Metadata.Name);
            // The OTEL service instance ID annotation will be generated and applied automatically by DCP.
            annotationHolder.Annotate(CustomResource.ResourceNameAnnotation, project.Name);

            SetInitialResourceState(project, annotationHolder);

            var projectLaunchConfiguration = new ProjectLaunchConfiguration();
            projectLaunchConfiguration.ProjectPath = projectMetadata.ProjectPath;

            if (!string.IsNullOrEmpty(configuration[DebugSessionPortVar]))
            {
                exeSpec.ExecutionType = ExecutionType.IDE;

                projectLaunchConfiguration.DisableLaunchProfile = project.TryGetLastAnnotation<ExcludeLaunchProfileAnnotation>(out _);
                if (!projectLaunchConfiguration.DisableLaunchProfile && project.TryGetLastAnnotation<LaunchProfileAnnotation>(out var lpa))
                {
                    projectLaunchConfiguration.LaunchProfile = lpa.LaunchProfileName;
                }
            }
            else
            {
                exeSpec.ExecutionType = ExecutionType.Process;
                if (configuration.GetBool("DOTNET_WATCH") is not true)
                {
                    exeSpec.Args = [
                        "run",
                        "--no-build",
                        "--project",
                        projectMetadata.ProjectPath,
                    ];
                }
                else
                {
                    exeSpec.Args = [
                        "watch",
                        "--non-interactive",
                        "--no-hot-reload",
                        "--project",
                        projectMetadata.ProjectPath
                    ];
                }

                if (!string.IsNullOrEmpty(_distributedApplicationOptions.Configuration))
                {
                    exeSpec.Args.AddRange(new [] {"-c", _distributedApplicationOptions.Configuration});
                }

                // We pretty much always want to suppress the normal launch profile handling
                // because the settings from the profile will override the ambient environment settings, which is not what we want
                // (the ambient environment settings for service processes come from the application model
                // and should be HIGHER priority than the launch profile settings).
                // This means we need to apply the launch profile settings manually--the invocation parameters here,
                // and the environment variables/application URLs inside CreateExecutableAsync().
                exeSpec.Args.Add("--no-launch-profile");

                var launchProfile = project.GetEffectiveLaunchProfile()?.LaunchProfile;
                if (launchProfile is not null && !string.IsNullOrWhiteSpace(launchProfile.CommandLineArgs))
                {
                    var cmdArgs = CommandLineArgsParser.Parse(launchProfile.CommandLineArgs);
                    if (cmdArgs.Count > 0)
                    {
                        exeSpec.Args.Add("--");
                        exeSpec.Args.AddRange(cmdArgs);
                    }
                }
            }

            // We want this annotation even if we are not using IDE execution; see ToSnapshot() for details.
            annotationHolder.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, projectLaunchConfiguration);

            var exeAppResource = new AppResource(project, ers);
            AddServicesProducedInfo(project, annotationHolder, exeAppResource);
            _appResources.Add(exeAppResource);
        }
    }

    private static void SetInitialResourceState(IResource resource, IAnnotationHolder annotationHolder)
    {
        // Store the initial state of the resource
        if (resource.TryGetLastAnnotation<ResourceSnapshotAnnotation>(out var initial) &&
            initial.InitialSnapshot.State?.Text is string state && !string.IsNullOrEmpty(state))
        {
            annotationHolder.Annotate(CustomResource.ResourceStateAnnotation, state);
        }
    }

    private Task CreateExecutablesAsync(IEnumerable<AppResource> executableResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpExecutablesCreateStart();

            async Task CreateExecutableAsyncCore(AppResource cr, CancellationToken cancellationToken)
            {
                var logger = loggerService.GetLogger(cr.ModelResource);

                await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with
                {
                    ResourceType = cr.ModelResource is ProjectResource ? KnownResourceTypes.Project : KnownResourceTypes.Executable,
                    Properties = [],
                    State = "Starting"
                })
                .ConfigureAwait(false);

                try
                {
                    await CreateExecutableAsync(cr, logger, cancellationToken).ConfigureAwait(false);
                }
                catch (FailedToApplyEnvironmentException)
                {
                    // For this exception we don't want the noise of the stack trace, we've already
                    // provided more detail where we detected the issue (e.g. envvar name). To get
                    // more diagnostic information reduce logging level for DCP log category to Debug.
                    await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with { State = "FailedToStart" }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create resource {ResourceName}", cr.ModelResource.Name);

                    await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with { State = "FailedToStart" }).ConfigureAwait(false);
                }
            }

            var tasks = new List<Task>();
            foreach (var er in executableResources)
            {
                tasks.Add(CreateExecutableAsyncCore(er, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }
        finally
        {
            AspireEventSource.Instance.DcpExecutablesCreateStop();
        }
    }

    private async Task CreateExecutableAsync(AppResource er, ILogger resourceLogger, CancellationToken cancellationToken)
    {
        ExecutableSpec spec;
        Func<Task<CustomResource>> createResource;

        switch (er.DcpResource)
        {
            case Executable exe:
                spec = exe.Spec;
                createResource = async () => await kubernetesService.CreateAsync(exe, cancellationToken).ConfigureAwait(false);
                break;
            case ExecutableReplicaSet ers:
                spec = ers.Spec.Template.Spec;
                createResource = async () => await kubernetesService.CreateAsync(ers, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Expected an Executable-like resource, but got {er.DcpResource.Kind} instead");
        }

        bool failedToApplyArgs = false;
        spec.Args ??= [];

        if (er.ModelResource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var exeArgsCallbacks))
        {
            var args = new List<object>();
            var commandLineContext = new CommandLineArgsCallbackContext(args, cancellationToken);

            foreach (var exeArgsCallback in exeArgsCallbacks)
            {
                await exeArgsCallback.Callback(commandLineContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                try
                {
                    var value = arg switch
                    {
                        string s => s,
                        IValueProvider valueProvider => await GetValue(key: null, valueProvider, resourceLogger, isContainer: false, cancellationToken).ConfigureAwait(false),
                        null => null,
                        _ => throw new InvalidOperationException($"Unexpected value for {arg}")
                    };

                    if (value is not null)
                    {
                        spec.Args.Add(value);
                    }
                }
                catch (Exception ex)
                {
                    resourceLogger.LogCritical("Failed to apply arguments. A dependency may have failed to start.");
                    _logger.LogDebug(ex, "Failed to apply arguments. A dependency may have failed to start.");
                    failedToApplyArgs = true;
                }
            }
        }

        var config = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(_executionContext, config, cancellationToken)
        {
            Logger = resourceLogger
        };

        if (er.ModelResource.TryGetEnvironmentVariables(out var envVarAnnotations))
        {
            foreach (var ann in envVarAnnotations)
            {
                await ann.Callback(context).ConfigureAwait(false);
            }
        }

        bool failedToApplyConfiguration = false;
        spec.Env = [];
        foreach (var c in config)
        {
            try
            {
                var value = c.Value switch
                {
                    string s => s,
                    IValueProvider valueProvider => await GetValue(c.Key, valueProvider, resourceLogger, isContainer: false, cancellationToken).ConfigureAwait(false),
                    null => null,
                    _ => throw new InvalidOperationException($"Unexpected value for environment variable \"{c.Key}\".")
                };

                if (value is not null)
                {
                    spec.Env.Add(new EnvVar { Name = c.Key, Value = value });
                }
            }
            catch (Exception ex)
            {
                resourceLogger.LogCritical("Failed to apply configuration value '{ConfigKey}'. A dependency may have failed to start.", c.Key);
                _logger.LogDebug(ex, "Failed to apply configuration value '{ConfigKey}'. A dependency may have failed to start.", c.Key);
                failedToApplyConfiguration = true;
            }
        }

        if (failedToApplyConfiguration || failedToApplyArgs)
        {
            throw new FailedToApplyEnvironmentException();
        }

        await createResource().ConfigureAwait(false);
    }

    private async Task<string?> GetValue(string? key, IValueProvider valueProvider, ILogger logger, bool isContainer, CancellationToken cancellationToken)
    {
        var task = valueProvider.GetValueAsync(cancellationToken);

        if (!task.IsCompleted)
        {
            if (valueProvider is IResource resource)
            {
                if (key is null)
                {
                    logger.LogInformation("Waiting for value from resource '{ResourceName}'", resource.Name);
                }
                else
                {
                    logger.LogInformation("Waiting for value for environment variable value '{Name}' from resource '{ResourceName}'", key, resource.Name);
                }
            }
            else if (valueProvider is ConnectionStringReference { Resource: var cs })
            {
                logger.LogInformation("Waiting for value for connection string from resource '{ResourceName}'", cs.Name);
            }
            else
            {
                if (key is null)
                {
                    logger.LogInformation("Waiting for value from {ValueProvider}.", valueProvider.ToString());
                }
                else
                {
                    logger.LogInformation("Waiting for value for environment variable value '{Name}' from {ValueProvider}.", key, valueProvider.ToString());
                }
            }
        }

        var value = await task.ConfigureAwait(false);

        if (value is not null && isContainer && valueProvider is ConnectionStringReference or EndpointReference or HostUrl)
        {
            // If the value is a connection string or endpoint reference, we need to replace localhost with the container host.
            return ReplaceLocalhostWithContainerHost(value);
        }

        return value;
    }

    private void PrepareContainers()
    {
        var modelContainerResources = _model.GetContainerResources();

        foreach (var container in modelContainerResources)
        {
            if (!container.TryGetContainerImageName(out var containerImageName))
            {
                // This should never happen! In order to get into this loop we need
                // to have the annotation, if we don't have the annotation by the time
                // we get here someone is doing something wrong.
                throw new InvalidOperationException();
            }

            var nameSuffix = GetRandomNameSuffix();
            var containerObjectName = GetObjectNameForResource(container, nameSuffix);
            var ctr = Container.Create(containerObjectName, containerImageName);

            ctr.Spec.ContainerName = containerObjectName; // Use the same name for container orchestrator (Docker, Podman) resource and DCP object name.
            ctr.Annotate(CustomResource.ResourceNameAnnotation, container.Name);
            ctr.Annotate(CustomResource.OtelServiceNameAnnotation, container.Name);
            ctr.Annotate(CustomResource.OtelServiceInstanceIdAnnotation, nameSuffix);
            SetInitialResourceState(container, ctr);

            if (container.TryGetContainerMounts(out var containerMounts))
            {
                ctr.Spec.VolumeMounts = [];

                foreach (var mount in containerMounts)
                {
                    var volumeSpec = new VolumeMount
                    {
                        Source = mount.Source,
                        Target = mount.Target,
                        Type = mount.Type == ContainerMountType.BindMount ? VolumeMountType.Bind : VolumeMountType.Volume,
                        IsReadOnly = mount.IsReadOnly
                    };

                    ctr.Spec.VolumeMounts.Add(volumeSpec);
                }
            }

            var containerAppResource = new AppResource(container, ctr);
            AddServicesProducedInfo(container, ctr, containerAppResource);
            _appResources.Add(containerAppResource);
        }
    }

    private Task CreateContainersAsync(IEnumerable<AppResource> containerResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpContainersCreateStart();

            async Task CreateContainerAsyncCore(AppResource cr, CancellationToken cancellationToken)
            {
                var logger = loggerService.GetLogger(cr.ModelResource);

                await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with
                {
                    State = "Starting",
                    Properties = [
                        new(KnownProperties.Container.Image, cr.ModelResource.TryGetContainerImageName(out var imageName) ? imageName : ""),
                   ],
                    ResourceType = KnownResourceTypes.Container
                })
                .ConfigureAwait(false);

                await SetChildResourceStateAsync(cr.ModelResource, "Starting").ConfigureAwait(false);

                try
                {
                    await CreateContainerAsync(cr, logger, cancellationToken).ConfigureAwait(false);
                }
                catch (FailedToApplyEnvironmentException)
                {
                    // For this exception we don't want the noise of the stack trace, we've already
                    // provided more detail where we detected the issue (e.g. envvar name). To get
                    // more diagnostic information reduce logging level for DCP log category to Debug.
                    await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with { State = "FailedToStart" }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create container resource {ResourceName}", cr.ModelResource.Name);

                    await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with { State = "FailedToStart" }).ConfigureAwait(false);

                    await SetChildResourceStateAsync(cr.ModelResource, "FailedToStart").ConfigureAwait(false);
                }
            }

            var tasks = new List<Task>();
            foreach (var cr in containerResources)
            {
                tasks.Add(CreateContainerAsyncCore(cr, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }
        finally
        {
            AspireEventSource.Instance.DcpContainersCreateStop();
        }
    }

    private async Task CreateContainerAsync(AppResource cr, ILogger resourceLogger, CancellationToken cancellationToken)
    {
        var dcpContainerResource = (Container)cr.DcpResource;
        var modelContainerResource = cr.ModelResource;

        await ApplyBuildArgumentsAsync(dcpContainerResource, modelContainerResource, cancellationToken).ConfigureAwait(false);

        var config = new Dictionary<string, object>();

        dcpContainerResource.Spec.Env = [];

        if (cr.ServicesProduced.Count > 0)
        {
            dcpContainerResource.Spec.Ports = new();

            foreach (var sp in cr.ServicesProduced)
            {
                var ea = sp.EndpointAnnotation;

                var portSpec = new ContainerPortSpec()
                {
                    ContainerPort = ea.TargetPort,
                };

                if (!ea.IsProxied && ea.Port is int)
                {
                    portSpec.HostPort = ea.Port;
                }

                switch (sp.EndpointAnnotation.Protocol)
                {
                    case ProtocolType.Tcp:
                        portSpec.Protocol = PortProtocol.TCP; break;
                    case ProtocolType.Udp:
                        portSpec.Protocol = PortProtocol.UDP; break;
                }

                dcpContainerResource.Spec.Ports.Add(portSpec);
            }
        }

        if (modelContainerResource.TryGetEnvironmentVariables(out var containerEnvironmentVariables))
        {
            var context = new EnvironmentCallbackContext(_executionContext, config, cancellationToken);

            foreach (var v in containerEnvironmentVariables)
            {
                await v.Callback(context).ConfigureAwait(false);
            }
        }

        bool failedToApplyConfiguration = false;
        foreach (var kvp in config)
        {
            try
            {
                var value = kvp.Value switch
                {
                    string s => s,
                    IValueProvider valueProvider => await GetValue(kvp.Key, valueProvider, resourceLogger, isContainer: true, cancellationToken).ConfigureAwait(false),
                    null => null,
                    _ => throw new InvalidOperationException($"Unexpected value for environment variable \"{kvp.Key}\".")
                };

                if (value is not null)
                {
                    dcpContainerResource.Spec.Env.Add(new EnvVar { Name = kvp.Key, Value = value });
                }
            }
            catch (Exception ex)
            {
                resourceLogger.LogCritical("Failed to apply configuration value '{ConfigKey}'. A dependency may have failed to start.", kvp.Key);
                _logger.LogDebug(ex, "Failed to apply configuration value '{ConfigKey}'. A dependency may have failed to start.", kvp.Key);
                failedToApplyConfiguration = true;
            }
        }

        // Apply optional extra arguments to the container run command.
        if (modelContainerResource.TryGetAnnotationsOfType<ContainerRuntimeArgsCallbackAnnotation>(out var runArgsCallback))
        {
            dcpContainerResource.Spec.RunArgs ??= [];

            var args = new List<object>();

            var containerRunArgsContext = new ContainerRuntimeArgsCallbackContext(args, cancellationToken);

            foreach (var callback in runArgsCallback)
            {
                await callback.Callback(containerRunArgsContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                var value = arg switch
                {
                    string s => s,
                    IValueProvider valueProvider => await GetValue(key: null, valueProvider, resourceLogger, isContainer: true, cancellationToken).ConfigureAwait(false),
                    null => null,
                    _ => throw new InvalidOperationException($"Unexpected value for {arg}")
                };

                if (value is not null)
                {
                    dcpContainerResource.Spec.RunArgs.Add(value);
                }
            }
        }

        var failedToApplyArgs = false;
        if (modelContainerResource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallback))
        {
            dcpContainerResource.Spec.Args ??= [];

            var args = new List<object>();

            var commandLineArgsContext = new CommandLineArgsCallbackContext(args, cancellationToken);

            foreach (var callback in argsCallback)
            {
                await callback.Callback(commandLineArgsContext).ConfigureAwait(false);
            }

            foreach (var arg in args)
            {
                try
                {
                    var value = arg switch
                    {
                        string s => s,
                        IValueProvider valueProvider => await GetValue(key: null, valueProvider, resourceLogger, isContainer: true, cancellationToken).ConfigureAwait(false),
                        null => null,
                        _ => throw new InvalidOperationException($"Unexpected value for {arg}")
                    };

                    if (value is not null)
                    {
                        dcpContainerResource.Spec.Args.Add(value);
                    }
                }
                catch (Exception ex)
                {
                    resourceLogger.LogCritical("Failed to apply container arguments '{ConfigKey}'. A dependency may have failed to start.", arg);
                    _logger.LogDebug(ex, "Failed to apply container arguments '{ConfigKey}'. A dependency may have failed to start.", arg);
                    failedToApplyArgs = true;
                }
            }
        }

        if (modelContainerResource is ContainerResource containerResource)
        {
            dcpContainerResource.Spec.Command = containerResource.Entrypoint;
        }

        if (failedToApplyArgs || failedToApplyConfiguration)
        {
            throw new FailedToApplyEnvironmentException();
        }

        await kubernetesService.CreateAsync(dcpContainerResource, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ApplyBuildArgumentsAsync(Container dcpContainerResource, IResource modelContainerResource, CancellationToken cancellationToken)
    {
        if (modelContainerResource.Annotations.OfType<DockerfileBuildAnnotation>().SingleOrDefault() is { } dockerfileBuildAnnotation)
        {
            var dcpBuildArgs = new List<EnvVar>();

            foreach (var buildArgument in dockerfileBuildAnnotation.BuildArguments)
            {
                var valueString = buildArgument.Value switch
                {
                    string stringValue => stringValue,
                    IValueProvider valueProvider => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
                    bool boolValue => boolValue ? "true" : "false",
                    _ => buildArgument.Value.ToString()
                };

                var dcpBuildArg = new EnvVar()
                {
                    Name = buildArgument.Key,
                    Value = valueString
                };

                dcpBuildArgs.Add(dcpBuildArg);
            }

            dcpContainerResource.Spec.Build = new()
            {
                Context = dockerfileBuildAnnotation.ContextPath,
                Dockerfile = dockerfileBuildAnnotation.DockerfilePath,
                Stage = dockerfileBuildAnnotation.Stage,
                Args = dcpBuildArgs
            };

            var dcpBuildSecrets = new List<BuildContextSecret>();

            foreach (var buildSecret in dockerfileBuildAnnotation.BuildSecrets)
            {
                var valueString = buildSecret.Value switch
                {
                    FileInfo filePath => filePath.FullName,
                    IValueProvider valueProvider => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
                    _ => throw new InvalidOperationException("Build secret can only be a parameter or a file.")
                };

                if (buildSecret.Value is FileInfo)
                {
                    var dcpBuildSecret = new BuildContextSecret
                    {
                        Id = buildSecret.Key,
                        Type = "file",
                        Source = valueString
                    };
                    dcpBuildSecrets.Add(dcpBuildSecret);
                }
                else
                {
                    var dcpBuildSecret = new BuildContextSecret
                    {
                      Id = buildSecret.Key,
                      Type = "env",
                      Value = valueString
                    };
                    dcpBuildSecrets.Add(dcpBuildSecret);
                }
            }

            dcpContainerResource.Spec.Build = new()
            {
                Context = dockerfileBuildAnnotation.ContextPath,
                Dockerfile = dockerfileBuildAnnotation.DockerfilePath,
                Stage = dockerfileBuildAnnotation.Stage,
                Args = dcpBuildArgs,
                Secrets = dcpBuildSecrets
            };
        }
    }

    private void AddServicesProducedInfo(IResource modelResource, IAnnotationHolder dcpResource, AppResource appResource)
    {
        string modelResourceName = "(unknown)";
        try
        {
            modelResourceName = GetObjectNameForResource(modelResource);
        }
        catch { } // For error messages only, OK to fall back to (unknown)

        var servicesProduced = _appResources.OfType<ServiceAppResource>().Where(r => r.ModelResource == modelResource);
        foreach (var sp in servicesProduced)
        {
            var ea = sp.EndpointAnnotation;

            if (modelResource.IsContainer())
            {
                if (ea.TargetPort is null)
                {
                    throw new InvalidOperationException($"The endpoint '{ea.Name}' for container resource '{modelResourceName}' must specify the {nameof(EndpointAnnotation.TargetPort)} value");
                }
            }
            else if (!ea.IsProxied)
            {
                if (appResource.DcpResource is ExecutableReplicaSet ers && ers.Spec.Replicas > 1)
                {
                    throw new InvalidOperationException($"Resource '{modelResourceName}' uses multiple replicas and a proxy-less endpoint '{ea.Name}'. These features do not work together.");
                }

                if (ea.Port is int && ea.Port != ea.TargetPort)
                {
                    throw new InvalidOperationException($"The endpoint '{ea.Name}' for resource '{modelResourceName}' is not using a proxy, and it has a value of {nameof(EndpointAnnotation.Port)} property that is different from the value of {nameof(EndpointAnnotation.TargetPort)} property. For proxy-less endpoints they must match.");
                }
            }
            else
            {
                Debug.Assert(ea.IsProxied);

                if (ea.TargetPort is int && ea.Port is int && ea.TargetPort == ea.Port)
                {
                    throw new InvalidOperationException(
                        $"The endpoint '{ea.Name}' for resource '{modelResourceName}' requested a proxy ({nameof(ea.IsProxied)} is true). Non-container resources cannot be proxied when both {nameof(ea.TargetPort)} and {nameof(ea.Port)} are specified with the same value.");
                }

                if (appResource.DcpResource is ExecutableReplicaSet && ea.TargetPort is int)
                {
                    throw new InvalidOperationException(
                        $"Resource '{modelResourceName}' can have multiple replicas, and it uses endpoint '{ea.Name}' that has {nameof(ea.TargetPort)} property set. Each replica must have a unique port; setting {nameof(ea.TargetPort)} is not allowed.");
                }
            }

            var spAnn = new ServiceProducerAnnotation(sp.Service.Metadata.Name);
            spAnn.Port = ea.TargetPort;
            dcpResource.AnnotateAsObjectList(CustomResource.ServiceProducerAnnotation, spAnn);
            appResource.ServicesProduced.Add(sp);
        }
    }

    private async Task CreateResourcesAsync<RT>(CancellationToken cancellationToken) where RT : CustomResource
    {
        try
        {
            var resourcesToCreate = _appResources.Select(r => r.DcpResource).OfType<RT>();
            if (!resourcesToCreate.Any())
            {
                return;
            }

            // CONSIDER batched creation
            foreach (var res in resourcesToCreate)
            {
                await kubernetesService.CreateAsync(res, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            // We catch and suppress the OperationCancelledException because the user may CTRL-C
            // during start up of the resources.
            _logger.LogDebug(ex, "Cancellation during creation of resources.");
        }
    }

    private string GetObjectNameForResource(IResource resource, string suffix = "")
    {
        static string maybeWithSuffix(string s, string localSuffix, string? globalSuffix)
            => (string.IsNullOrWhiteSpace(localSuffix), string.IsNullOrWhiteSpace(globalSuffix)) switch
            {
                (true, true) => s,
                (false, true) => $"{s}-{localSuffix}",
                (true, false) => $"{s}-{globalSuffix}",
                (false, false) => $"{s}-{localSuffix}-{globalSuffix}"
            };
        return maybeWithSuffix(resource.Name, suffix, _options.Value.ResourceNameSuffix);
    }

    private static string GenerateUniqueServiceName(HashSet<string> serviceNames, string candidateName)
    {
        int suffix = 1;
        string uniqueName = candidateName;

        while (!serviceNames.Add(uniqueName))
        {
            uniqueName = $"{candidateName}-{suffix}";
            suffix++;
            if (suffix == 100)
            {
                // Should never happen, but we do not want to ever get into a infinite loop situation either.
                throw new ArgumentException($"Could not generate a unique name for service '{candidateName}'");
            }
        }

        return uniqueName;
    }

    private static string GetRandomNameSuffix()
    {
        // RandomNameSuffixLength of lowercase characters
        var suffix = PasswordGenerator.Generate(RandomNameSuffixLength, true, false, false, false, RandomNameSuffixLength, 0, 0, 0);
        return suffix;
    }

    public async Task DeleteResourcesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AspireEventSource.Instance.DcpModelCleanupStart();
            await DeleteResourcesAsync<ExecutableReplicaSet>("project", cancellationToken).ConfigureAwait(false);
            await DeleteResourcesAsync<Executable>("project", cancellationToken).ConfigureAwait(false);
            await DeleteResourcesAsync<Container>("container", cancellationToken).ConfigureAwait(false);
            await DeleteResourcesAsync<Service>("service", cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected
            _logger.LogDebug("Cancellation received while deleting resources.");
        }
        finally
        {
            AspireEventSource.Instance.DcpModelCleanupStop();
            _appResources.Clear();
        }
    }

    private async Task DeleteResourcesAsync<RT>(string resourceType, CancellationToken cancellationToken) where RT : CustomResource
    {
        var resourcesToDelete = _appResources.Select(r => r.DcpResource).OfType<RT>();
        if (!resourcesToDelete.Any())
        {
            return;
        }

        foreach (var res in resourcesToDelete)
        {
            try
            {
                await kubernetesService.DeleteAsync<RT>(res.Metadata.Name, res.Metadata.NamespaceProperty, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Could not stop {ResourceType} '{ResourceName}'.", resourceType, res.Metadata.Name);
            }
        }
    }

    private string ReplaceLocalhostWithContainerHost(string value)
    {
        // https://stackoverflow.com/a/43541732/45091

        // This configuration value is a workaround for the fact that host.docker.internal is not available on Linux by default.
        var hostName = DefaultContainerHostName;

        return value.Replace("localhost", hostName, StringComparison.OrdinalIgnoreCase)
                    .Replace("127.0.0.1", hostName)
                    .Replace("[::1]", hostName);
    }
}
