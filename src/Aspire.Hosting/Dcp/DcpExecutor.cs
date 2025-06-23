// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ConsoleLogs;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Utils;
using Json.Patch;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Aspire.Hosting.Dcp;

internal sealed class DcpExecutor : IDcpExecutor, IConsoleLogsService, IAsyncDisposable
{
    internal const string DebugSessionPortVar = "DEBUG_SESSION_PORT";
    internal const string DefaultAspireNetworkName = "default-aspire-network";

    // Disposal of the DcpExecutor means shutting down watches and log streams,
    // and asking DCP to start the shutdown process. If we cannot complete these tasks within 10 seconds,
    // it probably means DCP crashed and there is no point trying further.
    private static readonly TimeSpan s_disposeTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<DistributedApplication> _distributedApplicationLogger;
    private readonly IKubernetesService _kubernetesService;
    private readonly IConfiguration _configuration;
    private readonly ResourceLoggerService _loggerService;
    private readonly IDcpDependencyCheckService _dcpDependencyCheckService;
    private readonly DcpNameGenerator _nameGenerator;
    private readonly ILogger<DcpExecutor> _logger;
    private readonly DistributedApplicationModel _model;
    private readonly DistributedApplicationOptions _distributedApplicationOptions;
    private readonly IDistributedApplicationEventing _distributedApplicationEventing;
    private readonly IOptions<DcpOptions> _options;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly List<AppResource> _appResources = [];
    private readonly CancellationTokenSource _shutdownCancellation = new();
    private readonly DcpExecutorEvents _executorEvents;

    private readonly DcpResourceState _resourceState;
    private readonly ResourceSnapshotBuilder _snapshotBuilder;

    private readonly Lazy<string> _hostName = new(() =>
    {
        try
        {
            return Dns.GetHostName();
        }
        catch
        {
            return "localhost";
        }
    });

    // Internal for testing.
    internal ResiliencePipeline<bool> DeleteResourceRetryPipeline { get; set; }
    internal ResiliencePipeline CreateServiceRetryPipeline { get; set; }
    internal ResiliencePipeline WatchResourceRetryPipeline { get; set; }

    private readonly ConcurrentDictionary<string, (CancellationTokenSource Cancellation, Task Task)> _logStreams = new();
    private DcpInfo? _dcpInfo;
    private Task? _resourceWatchTask;
    private int _stopped;

    private readonly record struct LogInformationEntry(string ResourceName, bool? LogsAvailable, bool? HasSubscribers);
    private readonly Channel<LogInformationEntry> _logInformationChannel = Channel.CreateUnbounded<LogInformationEntry>(
        new UnboundedChannelOptions { SingleReader = true });

    public DcpExecutor(ILogger<DcpExecutor> logger,
                       ILogger<DistributedApplication> distributedApplicationLogger,
                       DistributedApplicationModel model,
                       IKubernetesService kubernetesService,
                       IConfiguration configuration,
                       IDistributedApplicationEventing distributedApplicationEventing,
                       DistributedApplicationOptions distributedApplicationOptions,
                       IOptions<DcpOptions> options,
                       DistributedApplicationExecutionContext executionContext,
                       ResourceLoggerService loggerService,
                       IDcpDependencyCheckService dcpDependencyCheckService,
                       DcpNameGenerator nameGenerator,
                       DcpExecutorEvents executorEvents)
    {
        _distributedApplicationLogger = distributedApplicationLogger;
        _kubernetesService = kubernetesService;
        _configuration = configuration;
        _loggerService = loggerService;
        _dcpDependencyCheckService = dcpDependencyCheckService;
        _nameGenerator = nameGenerator;
        _executorEvents = executorEvents;
        _logger = logger;
        _model = model;
        _distributedApplicationEventing = distributedApplicationEventing;
        _distributedApplicationOptions = distributedApplicationOptions;
        _options = options;
        _executionContext = executionContext;
        _resourceState = new(model.Resources.ToDictionary(r => r.Name), _appResources);
        _snapshotBuilder = new(_resourceState);

        DeleteResourceRetryPipeline = DcpPipelineBuilder.BuildDeleteRetryPipeline(logger);
        CreateServiceRetryPipeline = DcpPipelineBuilder.BuildCreateServiceRetryPipeline(options.Value, logger);
        WatchResourceRetryPipeline = DcpPipelineBuilder.BuildWatchResourcePipeline(logger);
    }

    private string DefaultContainerHostName => _configuration["AppHost:ContainerHostname"] ?? _dcpInfo?.Containers?.ContainerHostName ?? "host.docker.internal";

    public async Task RunApplicationAsync(CancellationToken cancellationToken = default)
    {
        AspireEventSource.Instance.DcpModelCreationStart();

        _dcpInfo = await _dcpDependencyCheckService.GetDcpInfoAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        Debug.Assert(_dcpInfo is not null, "DCP info should not be null at this point");

        try
        {
            PrepareServices();
            PrepareContainers();
            PrepareExecutables();

            await _executorEvents.PublishAsync(new OnResourcesPreparedContext(cancellationToken)).ConfigureAwait(false);

            // Watch for changes to the resource state.
            WatchResourceChanges();

            await CreateServicesAsync(cancellationToken).ConfigureAwait(false);

            await CreateContainerNetworksAsync(cancellationToken).ConfigureAwait(false);

            await CreateContainersAndExecutablesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // This is here so hosting does not throw an exception when CTRL+C during startup.
            _logger.LogDebug("Cancellation received during application startup.");
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
        if (Interlocked.CompareExchange(ref _stopped, 1, 0) != 0)
        {
            return; // Already stopped/stop in progress.
        }

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

        try
        {
            if (_options.Value.WaitForResourceCleanup)
            {
                await _kubernetesService.CleanupResourcesAsync(cancellationToken).ConfigureAwait(false);
            }

            // The app orchestrator (represented by kubernetesService here) will perform a resource cleanup
            // (if not done already) when the app host process exits.
            // This is just a perf optimization, so we do not care that much if this call fails.
            // There is not much difference for single app run, but for tests that tend to launch multiple instances
            // of app host from the same process, the gain from programmatic orchestrator shutdown is significant
            // See https://github.com/dotnet/aspire/issues/6561 for more info.
            await _kubernetesService.StopServerAsync(Model.ResourceCleanup.Full, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore.
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Application orchestrator could not be stopped programmatically.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeCts = new CancellationTokenSource();
        disposeCts.CancelAfter(s_disposeTimeout);
        await StopAsync(disposeCts.Token).ConfigureAwait(false);
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
                    Task.Run(() => WatchKubernetesResourceAsync<Executable>((t, r) => ProcessResourceChange(t, r, _resourceState.ExecutablesMap, "Executable", (e, s) => _snapshotBuilder.ToSnapshot(e, s)))),
                    Task.Run(() => WatchKubernetesResourceAsync<Container>((t, r) => ProcessResourceChange(t, r, _resourceState.ContainersMap, "Container", (c, s) => _snapshotBuilder.ToSnapshot(c, s)))),
                    Task.Run(() => WatchKubernetesResourceAsync<Service>(ProcessServiceChange)),
                    Task.Run(() => WatchKubernetesResourceAsync<Endpoint>(ProcessEndpointChange))).ConfigureAwait(false);
            }
        });

        _loggerService.SetConsoleLogsService(this);

        var watchSubscribersTask = Task.Run(async () =>
        {
            await foreach (var subscribers in _loggerService.WatchAnySubscribersAsync(cancellationToken).ConfigureAwait(false))
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
                        if (_resourceState.ContainersMap.TryGetValue(entry.ResourceName, out var container))
                        {
                            StartLogStream(container);
                        }
                        else if (_resourceState.ExecutablesMap.TryGetValue(entry.ResourceName, out var executable))
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
                    }
                }

                resourceLogState[entry.ResourceName] = (logsAvailable, hasSubscribers);
            }
        });

        _resourceWatchTask = Task.WhenAll(watchResourcesTask, watchSubscribersTask, watchInformationChannelTask);

        async Task WatchKubernetesResourceAsync<T>(Func<WatchEventType, T, Task> handler) where T : CustomResource
        {
            try
            {
                _logger.LogDebug("Watching over DCP {ResourceType} resources.", typeof(T).Name);
                await WatchResourceRetryPipeline.ExecuteAsync(async (pipelineCancellationToken) =>
                {
                    await foreach (var (eventType, resource) in _kubernetesService.WatchAsync<T>(cancellationToken: pipelineCancellationToken).ConfigureAwait<(global::k8s.WatchEventType, T)>(false))
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
                _logger.LogCritical(ex, "Watch task over Kubernetes {ResourceType} resources terminated unexpectedly.", typeof(T).Name);
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
                _resourceState.ApplicationModel.TryGetValue(resourceName, out var appModelResource))
            {
                if (changeType == ResourceSnapshotChangeType.Delete)
                {
                    // Stop the log stream for the resource
                    if (_logStreams.TryRemove(resource.Metadata.Name, out var logStream))
                    {
                        logStream.Cancellation.Cancel();
                    }

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

                    var resourceType = GetResourceType(resource, appModelResource);
                    var status = GetResourceStatus(resource);
                    await _executorEvents.PublishAsync(new OnResourceChangedContext(_shutdownCancellation.Token, resourceType, appModelResource, resource.Metadata.Name, status, s => snapshotFactory(resource, s))).ConfigureAwait(false);

                    if (resource is Container { LogsAvailable: true } ||
                        resource is Executable { LogsAvailable: true })
                    {
                        _logInformationChannel.Writer.TryWrite(new(resource.Metadata.Name, LogsAvailable: true, HasSubscribers: null));
                    }
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
                _resourceState.ResourceAssociatedServicesMap.Remove((resourceKind, resource.Metadata.Name), out _);
            }
            else if (resource.Metadata.Annotations?.TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson) == true)
            {
                var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(servicesProducedAnnotationJson);
                if (serviceProducerAnnotations is not null)
                {
                    _resourceState.ResourceAssociatedServicesMap[(resourceKind, resource.Metadata.Name)]
                        = serviceProducerAnnotations.Select(e => e.ServiceName).ToList();
                }
            }
        }
    }

    private static string GetResourceType<T>(T resource, IResource appModelResource) where T : CustomResource
    {
        return resource switch
        {
            Container => KnownResourceTypes.Container,
            Executable => appModelResource is ProjectResource ? KnownResourceTypes.Project : KnownResourceTypes.Executable,
            _ => throw new InvalidOperationException($"Unknown resource type {resource.GetType().Name}")
        };
    }

    private static ResourceStatus GetResourceStatus(CustomResource resource)
    {
        if (resource is Container container)
        {
            if (container.Spec.Start == false && (container.Status?.State == null || container.Status?.State == ContainerState.Pending))
            {
                // If the resource is set for delay start, treat pending states as NotStarted.
                return new(KnownResourceStates.NotStarted, null, null);
            }

            return new(container.Status?.State, container.Status?.StartupTimestamp?.ToUniversalTime(), container.Status?.FinishTimestamp?.ToUniversalTime());
        }
        if (resource is Executable executable)
        {
            return new(executable.Status?.State, executable.Status?.StartupTimestamp?.ToUniversalTime(), executable.Status?.FinishTimestamp?.ToUniversalTime());
        }
        return new(null, null, null);
    }

    public async IAsyncEnumerable<IReadOnlyList<LogEntry>> GetAllLogsAsync(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<IReadOnlyList<(string, bool)>>? enumerable = null;
        if (_resourceState.ContainersMap.TryGetValue(resourceName, out var container))
        {
            enumerable = new ResourceLogSource<Container>(_logger, _kubernetesService, container, follow: false);
        }
        else if (_resourceState.ExecutablesMap.TryGetValue(resourceName, out var executable))
        {
            enumerable = new ResourceLogSource<Executable>(_logger, _kubernetesService, executable, follow: false);
        }

        if (enumerable != null)
        {
            await foreach (var batch in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var logs = new List<LogEntry>();
                foreach (var logEntry in CreateLogEntries(batch))
                {
                    logs.Add(logEntry);
                }

                yield return logs;
            }
        }
    }

    private static IEnumerable<LogEntry> CreateLogEntries(IReadOnlyList<(string, bool)> batch)
    {
        foreach (var (content, isError) in batch)
        {
            DateTime? timestamp = null;
            var resolvedContent = content;

            if (TimestampParser.TryParseConsoleTimestamp(resolvedContent, out var result))
            {
                resolvedContent = result.Value.ModifiedText;
                timestamp = result.Value.Timestamp.UtcDateTime;
            }

            yield return LogEntry.Create(timestamp, resolvedContent, content, isError);
        }
    }

    private void StartLogStream<T>(T resource) where T : CustomResource
    {
        IAsyncEnumerable<IReadOnlyList<(string, bool)>>? enumerable = resource switch
        {
            Container c when c.LogsAvailable => new ResourceLogSource<T>(_logger, _kubernetesService, resource, follow: true),
            Executable e when e.LogsAvailable => new ResourceLogSource<T>(_logger, _kubernetesService, resource, follow: true),
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
                        _logger.LogDebug("Starting log streaming for {ResourceName}.", resource.Metadata.Name);
                    }

                    // Pump the logs from the enumerable into the logger
                    var logger = _loggerService.GetInternalLogger(resource.Metadata.Name);

                    await foreach (var batch in enumerable.WithCancellation(cancellation.Token).ConfigureAwait(false))
                    {
                        foreach (var logEntry in CreateLogEntries(batch))
                        {
                            logger(logEntry);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                    _logger.LogDebug("Log streaming for {ResourceName} was cancelled.", resource.Metadata.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error streaming logs for {ResourceName}.", resource.Metadata.Name);
                }
            },
            cancellation.Token);

            return (cancellation, task);
        });
    }

    private async Task ProcessEndpointChange(WatchEventType watchEventType, Endpoint endpoint)
    {
        if (!ProcessResourceChange(_resourceState.EndpointsMap, watchEventType, endpoint))
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
        if (!ProcessResourceChange(_resourceState.ServicesMap, watchEventType, service))
        {
            return;
        }

        foreach (var ((resourceKind, resourceName), _) in _resourceState.ResourceAssociatedServicesMap.Where(e => e.Value.Contains(service.Metadata.Name)))
        {
            await TryRefreshResource(resourceKind, resourceName).ConfigureAwait(false);
        }
    }

    private async ValueTask TryRefreshResource(string resourceKind, string resourceName)
    {
        CustomResource? cr = resourceKind switch
        {
            "Container" => _resourceState.ContainersMap.TryGetValue(resourceName, out var container) ? container : null,
            "Executable" => _resourceState.ExecutablesMap.TryGetValue(resourceName, out var executable) ? executable : null,
            _ => null
        };

        if (cr is not null)
        {
            var appModelResourceName = cr.AppModelResourceName;

            if (appModelResourceName is not null &&
                _resourceState.ApplicationModel.TryGetValue(appModelResourceName, out var appModelResource))
            {
                var status = GetResourceStatus(cr);
                await _executorEvents.PublishAsync(new OnResourceChangedContext(_shutdownCancellation.Token, resourceKind, appModelResource, resourceName, status, s =>
                {
                    if (cr is Container container)
                    {
                        return _snapshotBuilder.ToSnapshot(container, s);
                    }
                    else if (cr is Executable exe)
                    {
                        return _snapshotBuilder.ToSnapshot(exe, s);
                    }
                    return s;
                })).ConfigureAwait(false);
            }
        }
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

            await CreateServiceRetryPipeline.ExecuteAsync(async (attemptCancellationToken) =>
            {
                var serviceChangeEnumerator = _kubernetesService.WatchAsync<Service>(cancellationToken: attemptCancellationToken);
                await foreach (var (evt, updated) in serviceChangeEnumerator.ConfigureAwait(false))
                {
                    if (evt == WatchEventType.Bookmark)
                    {
                        // Bookmarks do not contain any data.
                        continue;
                    }

                    var srvResource = needAddressAllocated.FirstOrDefault(sr => sr.Service.Metadata.Name == updated.Metadata.Name);
                    if (srvResource == null)
                    {
                        // This service most likely already has full address information, so it is not on needAddressAllocated list.
                        continue;
                    }

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
                var dcpSvc = await _kubernetesService.GetAsync<Service>(sar.Service.Metadata.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (dcpSvc.HasCompleteAddress)
                {
                    sar.Service.ApplyAddressInfoFrom(dcpSvc);
                }
                else
                {
                    _distributedApplicationLogger.LogWarning("Unable to allocate a network port for service '{ServiceName}'; service may be unreachable and its clients may not work properly.", sar.Service.Metadata.Name);
                }
            }

        }
        finally
        {
            AspireEventSource.Instance.DcpServicesCreationStop();
        }
    }

    private async Task CreateContainerNetworksAsync(CancellationToken cancellationToken)
    {
        var toCreate = _appResources.Where(r => r.DcpResource is ContainerNetwork);
        foreach (var containerNetwork in toCreate)
        {
            if (containerNetwork.DcpResource is ContainerNetwork cn)
            {
                await _kubernetesService.CreateAsync(cn, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CreateContainersAndExecutablesAsync(CancellationToken cancellationToken)
    {
        var toCreate = _appResources.Where(r => r.DcpResource is Container || r.DcpResource is Executable);
        AddAllocatedEndpointInfo(toCreate);

        await _executorEvents.PublishAsync(new OnEndpointsAllocatedContext(cancellationToken)).ConfigureAwait(false);

        // Fire the endpoints allocated event for all DCP managed resources with endpoints.
        foreach (var resource in toCreate.Select(r => r.ModelResource).OfType<IResourceWithEndpoints>())
        {
            var resourceEvent = new ResourceEndpointsAllocatedEvent(resource, _executionContext.ServiceProvider);
            await _distributedApplicationEventing.PublishAsync(resourceEvent, EventDispatchBehavior.NonBlockingConcurrent, cancellationToken).ConfigureAwait(false);
        }

        var containersTask = CreateContainersAsync(toCreate.Where(ar => ar.DcpResource is Container), cancellationToken);
        var executablesTask = CreateExecutablesAsync(toCreate.Where(ar => ar.DcpResource is Executable), cancellationToken);

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
                    sp.EndpointAnnotation.TargetHost.Replace("0.0.0.0", _hostName.Value).Replace("[::]", _hostName.Value).Replace("::", _hostName.Value),
                    (int)svc.AllocatedPort!,
                    containerHostAddress: appResource.ModelResource.IsContainer() ? containerHost : null,
                    targetPortExpression: $$$"""{{- portForServing "{{{svc.Metadata.Name}}}" -}}""");
            }
        }
    }

    private void PrepareServices()
    {
        var serviceProducers = _model.Resources
            .Select(r => (ModelResource: r, Endpoints: r.Annotations.OfType<EndpointAnnotation>().ToArray()))
            .Where(sp => sp.Endpoints.Any());

        // We need to ensure that Services have unique names (otherwise we cannot really distinguish between
        // services produced by different resources).
        var serviceNames = new HashSet<string>();

        foreach (var sp in serviceProducers)
        {
            var endpoints = sp.Endpoints;

            foreach (var endpoint in endpoints)
            {
                var serviceName = _nameGenerator.GetServiceName(sp.ModelResource, endpoint, endpoints.Length > 1, serviceNames);
                var svc = Service.Create(serviceName);

                if (!sp.ModelResource.SupportsProxy())
                {
                    // If the resource shouldn't be proxied, we need to enforce that on the annotation
                    endpoint.IsProxied = false;
                }

                var port = _options.Value.RandomizePorts && endpoint.IsProxied ? null : endpoint.Port;
                svc.Spec.Port = port;
                svc.Spec.Protocol = PortProtocol.FromProtocolType(endpoint.Protocol);
                svc.Spec.Address = endpoint.TargetHost switch
                {
                    "*" or "+" => "0.0.0.0",
                    _ => endpoint.TargetHost
                };
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
            EnsureRequiredAnnotations(executable);

            var exeInstance = GetDcpInstance(executable, instanceIndex: 0);
            var exePath = executable.Command;
            var exe = Executable.Create(exeInstance.Name, exePath);

            // The working directory is always relative to the app host project directory (if it exists).
            exe.Spec.WorkingDirectory = executable.WorkingDirectory;
            exe.Spec.ExecutionType = ExecutionType.Process;
            exe.Annotate(CustomResource.OtelServiceNameAnnotation, executable.Name);
            exe.Annotate(CustomResource.OtelServiceInstanceIdAnnotation, exeInstance.Suffix);
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

            EnsureRequiredAnnotations(project);

            var replicas = project.GetReplicaCount();

            for (var i = 0; i < replicas; i++)
            {
                var exeInstance = GetDcpInstance(project, instanceIndex: i);
                var exeSpec = Executable.Create(exeInstance.Name, "dotnet");
                exeSpec.Spec.WorkingDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);

                exeSpec.Annotate(CustomResource.OtelServiceNameAnnotation, project.Name);
                exeSpec.Annotate(CustomResource.OtelServiceInstanceIdAnnotation, exeInstance.Suffix);
                exeSpec.Annotate(CustomResource.ResourceNameAnnotation, project.Name);
                exeSpec.Annotate(CustomResource.ResourceReplicaCount, replicas.ToString(CultureInfo.InvariantCulture));
                exeSpec.Annotate(CustomResource.ResourceReplicaIndex, i.ToString(CultureInfo.InvariantCulture));

                SetInitialResourceState(project, exeSpec);

                var projectLaunchConfiguration = new ProjectLaunchConfiguration();
                projectLaunchConfiguration.ProjectPath = projectMetadata.ProjectPath;

                var projectArgs = new List<string>();

                if (!string.IsNullOrEmpty(_configuration[DebugSessionPortVar]))
                {
                    exeSpec.Spec.ExecutionType = ExecutionType.IDE;

                    projectLaunchConfiguration.DisableLaunchProfile = project.TryGetLastAnnotation<ExcludeLaunchProfileAnnotation>(out _);
                    if (!projectLaunchConfiguration.DisableLaunchProfile && project.TryGetLastAnnotation<LaunchProfileAnnotation>(out var lpa))
                    {
                        projectLaunchConfiguration.LaunchProfile = lpa.LaunchProfileName;
                    }
                }
                else
                {
                    exeSpec.Spec.ExecutionType = ExecutionType.Process;

                    if (_configuration.GetBool("DOTNET_WATCH") is not true)
                    {
                        projectArgs.AddRange([
                            "run",
                            "--no-build",
                            "--project",
                            projectMetadata.ProjectPath,
                        ]);
                    }
                    else
                    {
                        projectArgs.AddRange([
                            "watch",
                            "--non-interactive",
                            "--no-hot-reload",
                            "--project",
                            projectMetadata.ProjectPath
                        ]);
                    }

                    if (!string.IsNullOrEmpty(_distributedApplicationOptions.Configuration))
                    {
                        projectArgs.AddRange(new[] { "-c", _distributedApplicationOptions.Configuration });
                    }

                    // We pretty much always want to suppress the normal launch profile handling
                    // because the settings from the profile will override the ambient environment settings, which is not what we want
                    // (the ambient environment settings for service processes come from the application model
                    // and should be HIGHER priority than the launch profile settings).
                    // This means we need to apply the launch profile settings manually inside CreateExecutableAsync().
                    projectArgs.Add("--no-launch-profile");
                }

                // We want this annotation even if we are not using IDE execution; see ToSnapshot() for details.
                exeSpec.AnnotateAsObjectList(Executable.LaunchConfigurationsAnnotation, projectLaunchConfiguration);
                exeSpec.SetAnnotationAsObjectList(CustomResource.ResourceProjectArgsAnnotation, projectArgs);

                var exeAppResource = new AppResource(project, exeSpec);
                AddServicesProducedInfo(project, exeSpec, exeAppResource);
                _appResources.Add(exeAppResource);
            }
        }
    }

    private void EnsureRequiredAnnotations(IResource resource)
    {
        // Add the default lifecycle commands (start/stop/restart)
        resource.AddLifeCycleCommands();

        _nameGenerator.EnsureDcpInstancesPopulated(resource);
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

            async Task CreateResourceExecutablesAsyncCore(IResource resource, IEnumerable<AppResource> executables, CancellationToken cancellationToken)
            {
                var resourceLogger = _loggerService.GetLogger(resource);
                var resourceType = resource is ProjectResource ? KnownResourceTypes.Project : KnownResourceTypes.Executable;

                try
                {
                    // Publish snapshots built from DCP resources. Do this now to populate more values from DCP (source) to ensure they're
                    // available if the resource isn't immediately started because it's waiting or is configured for explicit start.
                    foreach (var er in executables)
                    {
                        await _executorEvents.PublishAsync(new OnResourceChangedContext(_shutdownCancellation.Token, resourceType, resource, er.DcpResourceName, new ResourceStatus(null, null, null), s => _snapshotBuilder.ToSnapshot((Executable)er.DcpResource, s))).ConfigureAwait(false);
                    }

                    await _executorEvents.PublishAsync(new OnResourceStartingContext(cancellationToken, resourceType, resource, DcpResourceName: null)).ConfigureAwait(false);

                    foreach (var er in executables)
                    {
                        if (er.ModelResource.TryGetAnnotationsOfType<ExplicitStartupAnnotation>(out _))
                        {
                            await _executorEvents.PublishAsync(new OnResourceChangedContext(cancellationToken, resourceType, resource, er.DcpResource.Metadata.Name, new ResourceStatus(KnownResourceStates.NotStarted, null, null), s => s with { State = new ResourceStateSnapshot(KnownResourceStates.NotStarted, null) })).ConfigureAwait(false);
                            continue;
                        }

                        try
                        {
                            await CreateExecutableAsync(er, resourceLogger, cancellationToken).ConfigureAwait(false);
                        }
                        catch (FailedToApplyEnvironmentException)
                        {
                            // For this exception we don't want the noise of the stack trace, we've already
                            // provided more detail where we detected the issue (e.g. envvar name). To get
                            // more diagnostic information reduce logging level for DCP log category to Debug.
                            await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, resourceType, er.ModelResource, er.DcpResource.Metadata.Name)).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // The purpose of this catch block is to ensure that if an individual executable resource fails
                            // to start that it doesn't tear down the entire app host AND that we route the error to the
                            // appropriate replica.
                            resourceLogger.LogError(ex, "Failed to create resource {ResourceName}", er.ModelResource.Name);
                            await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, resourceType, er.ModelResource, er.DcpResource.Metadata.Name)).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // The purpose of this catch block is to ensure that if an error processing the overall
                    // configuration of the executable resource files. This is different to the exception handling
                    // block above because at this stage of processing we don't necessarily have any replicas
                    // yet. For example if a dependency fails to start.
                    resourceLogger.LogError(ex, "Failed to create resource {ResourceName}", resource.Name);
                    await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, resourceType, resource, DcpResourceName: null)).ConfigureAwait(false);
                }
            }

            var tasks = new List<Task>();
            foreach (var group in executableResources.GroupBy(e => e.ModelResource))
            {
                tasks.Add(CreateResourceExecutablesAsyncCore(group.Key, group, cancellationToken));
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
        if (er.DcpResource is not Executable exe)
        {
            throw new InvalidOperationException($"Expected an Executable resource, but got {er.DcpResource.Kind} instead");
        }
        var spec = exe.Spec;

        // Don't create an args collection unless needed. A null args collection means a project run by the will use args provided by the launch profile.
        // https://github.com/dotnet/aspire/blob/main/docs/specs/IDE-execution.md#launch-profile-processing-project-launch-configuration
        spec.Args = null;

        // An executable can be restarted so args must be reset to an empty state.
        // After resetting, first apply any dotnet project related args, e.g. configuration, and then add args from the model resource.
        if (er.DcpResource.TryGetAnnotationAsObjectList<string>(CustomResource.ResourceProjectArgsAnnotation, out var projectArgs) && projectArgs.Count > 0)
        {
            spec.Args ??= [];
            spec.Args.AddRange(projectArgs);
        }

        // Get args from app host model resource.
        (var appHostArgs, var failedToApplyArgs) = await BuildArgsAsync(resourceLogger, er.ModelResource, cancellationToken).ConfigureAwait(false);

        var launchArgs = BuildLaunchArgs(er, spec, appHostArgs);

        var executableArgs = launchArgs.Where(a => !a.AnnotationOnly).Select(a => a.Value).ToList();
        if (executableArgs.Count > 0)
        {
            spec.Args ??= [];
            spec.Args.AddRange(executableArgs);
        }

        // Arg annotations are what is displayed in the dashboard.
        er.DcpResource.SetAnnotationAsObjectList(CustomResource.ResourceAppArgsAnnotation, launchArgs.Select(a => new AppLaunchArgumentAnnotation(a.Value, isSensitive: a.IsSensitive)));

        (spec.Env, var failedToApplyConfiguration) = await BuildEnvVarsAsync(resourceLogger, er.ModelResource, cancellationToken).ConfigureAwait(false);

        if (failedToApplyConfiguration || failedToApplyArgs)
        {
            throw new FailedToApplyEnvironmentException();
        }

        await _kubernetesService.CreateAsync(exe, cancellationToken).ConfigureAwait(false);
    }

    private static List<(string Value, bool IsSensitive, bool AnnotationOnly)> BuildLaunchArgs(AppResource er, ExecutableSpec spec, List<(string Value, bool IsSensitive)> appHostArgs)
    {
        // Launch args is the final list of args that are displayed in the UI and possibly added to the executable spec.
        // They're built from app host resource model args and any args in the effective launch profile.
        // Follows behavior in the IDE execution spec when in IDE execution mode:
        // https://github.com/dotnet/aspire/blob/main/docs/specs/IDE-execution.md#launch-profile-processing-project-launch-configuration
        var launchArgs = new List<(string Value, bool IsSensitive, bool AnnotationOnly)>();

        // If the executable is a project then include any command line args from the launch profile.
        if (er.ModelResource is ProjectResource project)
        {
            // Args in the launch profile is used when:
            // 1. The project is run as an executable. Launch profile args are combined with app host supplied args.
            // 2. The project is run by the IDE and no app host args are specified.
            if (spec.ExecutionType == ExecutionType.Process || (spec.ExecutionType == ExecutionType.IDE && appHostArgs.Count == 0))
            {
                // When the .NET project is launched from an IDE the launch profile args are automatically added.
                // We still want to display the args in the dashboard so only add them to the custom arg annotations.
                var annotationOnly = spec.ExecutionType == ExecutionType.IDE;

                var launchProfileArgs = GetLaunchProfileArgs(project.GetEffectiveLaunchProfile()?.LaunchProfile);
                if (launchProfileArgs.Count > 0 && appHostArgs.Count > 0)
                {
                    // If there are app host args, add a double-dash to separate them from the launch args.
                    launchProfileArgs.Insert(0, "--");
                }

                launchArgs.AddRange(launchProfileArgs.Select(a => (a, isSensitive: false, annotationOnly)));
            }
        }

        // In the situation where args are combined (process execution) the app host args are added after the launch profile args.
        launchArgs.AddRange(appHostArgs.Select(a => (a.Value, a.IsSensitive, annotationOnly: false)));

        return launchArgs;
    }

    private static List<string> GetLaunchProfileArgs(LaunchProfile? launchProfile)
    {
        if (launchProfile is not null && !string.IsNullOrWhiteSpace(launchProfile.CommandLineArgs))
        {
            return CommandLineArgsParser.Parse(launchProfile.CommandLineArgs);
        }

        return [];
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

            EnsureRequiredAnnotations(container);

            var containerObjectInstance = GetDcpInstance(container, instanceIndex: 0);
            var ctr = Container.Create(containerObjectInstance.Name, containerImageName);

            ctr.Spec.ContainerName = containerObjectInstance.Name; // Use the same name for container orchestrator (Docker, Podman) resource and DCP object name.

            if (container.GetContainerLifetimeType() == ContainerLifetime.Persistent)
            {
                ctr.Spec.Persistent = true;
            }

            if (container.TryGetContainerImagePullPolicy(out var pullPolicy))
            {
                ctr.Spec.PullPolicy = pullPolicy switch
                {
                    ImagePullPolicy.Default => null,
                    ImagePullPolicy.Always => ContainerPullPolicy.Always,
                    ImagePullPolicy.Missing => ContainerPullPolicy.Missing,
                    _ => throw new InvalidOperationException($"Unknown pull policy '{Enum.GetName(typeof(ImagePullPolicy), pullPolicy)}' for container '{container.Name}'")
                };
            }

            ctr.Annotate(CustomResource.ResourceNameAnnotation, container.Name);
            ctr.Annotate(CustomResource.OtelServiceNameAnnotation, container.Name);
            ctr.Annotate(CustomResource.OtelServiceInstanceIdAnnotation, containerObjectInstance.Suffix);
            SetInitialResourceState(container, ctr);

            ctr.Spec.Networks = new List<ContainerNetworkConnection>
            {
                new ContainerNetworkConnection
                {
                    Name = DefaultAspireNetworkName,
                    Aliases = new List<string> { container.Name },
                }
            };

            var containerAppResource = new AppResource(container, ctr);
            AddServicesProducedInfo(container, ctr, containerAppResource);
            _appResources.Add(containerAppResource);
        }
    }

    /// <summary>
    /// Gets information about the resource's DCP instance. ReplicaInstancesAnnotation is added in BeforeStartEvent.
    /// </summary>
    private static DcpInstance GetDcpInstance(IResource resource, int instanceIndex)
    {
        if (!resource.TryGetLastAnnotation<DcpInstancesAnnotation>(out var replicaAnnotation))
        {
            throw new DistributedApplicationException($"Couldn't find required {nameof(DcpInstancesAnnotation)} annotation on resource {resource.Name}.");
        }

        foreach (var instance in replicaAnnotation.Instances)
        {
            if (instance.Index == instanceIndex)
            {
                return instance;
            }
        }

        throw new DistributedApplicationException($"Couldn't find required instance ID for index {instanceIndex} on resource {resource.Name}.");
    }

    private async Task CreateContainersAsync(IEnumerable<AppResource> containerResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpContainersCreateStart();

            async Task CreateContainerAsyncCore(AppResource cr, CancellationToken cancellationToken)
            {
                var logger = _loggerService.GetLogger(cr.ModelResource);

                try
                {
                    await CreateContainerAsync(cr, logger, cancellationToken).ConfigureAwait(false);
                }
                catch (FailedToApplyEnvironmentException)
                {
                    // For this exception we don't want the noise of the stack trace, we've already
                    // provided more detail where we detected the issue (e.g. envvar name). To get
                    // more diagnostic information reduce logging level for DCP log category to Debug.
                    await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, KnownResourceTypes.Container, cr.ModelResource, cr.DcpResourceName)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create container resource {ResourceName}", cr.ModelResource.Name);
                    await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, KnownResourceTypes.Container, cr.ModelResource, cr.DcpResourceName)).ConfigureAwait(false);
                }
            }

            var tasks = new List<Task>();

            // Create a custom container network for Aspire if there are container resources
            if (containerResources.Any())
            {
                var network = ContainerNetwork.Create(DefaultAspireNetworkName);
                if (containerResources.Any(cr => cr.ModelResource.GetContainerLifetimeType() == ContainerLifetime.Persistent))
                {
                    // If we have any persistent container resources
                    network.Spec.Persistent = true;
                    // Persistent networks require a predictable name to be reused between runs.
                    // Append the same project hash suffix used for persistent container names.
                    network.Spec.NetworkName = $"{DefaultAspireNetworkName}-{_nameGenerator.GetProjectHashSuffix()}";
                }

                tasks.Add(_kubernetesService.CreateAsync(network, cancellationToken));
            }

            foreach (var cr in containerResources)
            {
                // Publish snapshot built from DCP resource. Do this now to populate more values from DCP (source) to ensure they're
                // available if the resource isn't immediately started because it's waiting or is configured for explicit start.
                await _executorEvents.PublishAsync(new OnResourceChangedContext(_shutdownCancellation.Token, KnownResourceTypes.Container, cr.ModelResource, cr.DcpResourceName, new ResourceStatus(null, null, null), s => _snapshotBuilder.ToSnapshot((Container)cr.DcpResource, s))).ConfigureAwait(false);

                if (cr.ModelResource.TryGetLastAnnotation<ExplicitStartupAnnotation>(out _))
                {
                    if (cr.DcpResource is Container container)
                    {
                        container.Spec.Start = false;
                    }
                }

                tasks.Add(CreateContainerAsyncCore(cr, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            AspireEventSource.Instance.DcpContainersCreateStop();
        }
    }

    private async Task CreateContainerAsync(AppResource cr, ILogger resourceLogger, CancellationToken cancellationToken)
    {
        await _executorEvents.PublishAsync(new OnResourceStartingContext(cancellationToken, KnownResourceTypes.Container, cr.ModelResource, cr.DcpResource.Metadata.Name)).ConfigureAwait(false);

        var dcpContainerResource = (Container)cr.DcpResource;
        var modelContainerResource = cr.ModelResource;

        await ApplyBuildArgumentsAsync(dcpContainerResource, modelContainerResource, cancellationToken).ConfigureAwait(false);

        var spec = dcpContainerResource.Spec;

        if (cr.ServicesProduced.Count > 0)
        {
            spec.Ports = BuildContainerPorts(cr);
        }

        spec.VolumeMounts = BuildContainerMounts(modelContainerResource);

        spec.CreateFiles = await BuildCreateFilesAsync(modelContainerResource, cancellationToken).ConfigureAwait(false);

        (spec.RunArgs, var failedToApplyRunArgs) = await BuildRunArgsAsync(resourceLogger, modelContainerResource, cancellationToken).ConfigureAwait(false);

        (var args, var failedToApplyArgs) = await BuildArgsAsync(resourceLogger, modelContainerResource, cancellationToken).ConfigureAwait(false);
        spec.Args = args.Select(a => a.Value).ToList();
        dcpContainerResource.SetAnnotationAsObjectList(CustomResource.ResourceAppArgsAnnotation, args.Select(a => new AppLaunchArgumentAnnotation(a.Value, isSensitive: a.IsSensitive)));

        (spec.Env, var failedToApplyConfiguration) = await BuildEnvVarsAsync(resourceLogger, modelContainerResource, cancellationToken).ConfigureAwait(false);

        if (modelContainerResource is ContainerResource containerResource)
        {
            spec.Command = containerResource.Entrypoint;
        }

        if (failedToApplyRunArgs || failedToApplyArgs || failedToApplyConfiguration)
        {
            throw new FailedToApplyEnvironmentException();
        }

        if (_dcpInfo is not null)
        {
            DcpDependencyCheck.CheckDcpInfoAndLogErrors(resourceLogger, _options.Value, _dcpInfo);
        }

        await _kubernetesService.CreateAsync(dcpContainerResource, cancellationToken).ConfigureAwait(false);
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
                    null => null,
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
        var modelResourceName = "(unknown)";
        try
        {
            modelResourceName = DcpNameGenerator.GetObjectNameForResource(modelResource, _options.Value);
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
                if (HasMultipleReplicas(appResource.DcpResource))
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

                if (HasMultipleReplicas(appResource.DcpResource) && ea.TargetPort is int)
                {
                    throw new InvalidOperationException(
                        $"Resource '{modelResourceName}' can have multiple replicas, and it uses endpoint '{ea.Name}' that has {nameof(ea.TargetPort)} property set. Each replica must have a unique port; setting {nameof(ea.TargetPort)} is not allowed.");
                }
            }

            var spAnn = new ServiceProducerAnnotation(sp.Service.Metadata.Name);
            spAnn.Address = ea.TargetHost;
            spAnn.Port = ea.TargetPort;
            dcpResource.AnnotateAsObjectList(CustomResource.ServiceProducerAnnotation, spAnn);
            appResource.ServicesProduced.Add(sp);
        }

        static bool HasMultipleReplicas(CustomResource resource)
        {
            if (resource is Executable exe && exe.Metadata.Annotations.TryGetValue(CustomResource.ResourceReplicaCount, out var value) && int.TryParse(value, CultureInfo.InvariantCulture, out var replicas) && replicas > 1)
            {
                return true;
            }
            return false;
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
                await _kubernetesService.CreateAsync(res, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            // We catch and suppress the OperationCancelledException because the user may CTRL-C
            // during start up of the resources.
            _logger.LogDebug(ex, "Cancellation during creation of resources.");
        }
    }

    /// <summary>
    /// Create a patch update using the specified resource.
    /// A copy is taken of the resource to avoid permanently changing it.
    /// </summary>
    private static V1Patch CreatePatch<T>(T obj, Action<T> change) where T : CustomResource
    {
        // This method isn't very efficient.
        // If mass or frequent patches are required then we may want to create patches manually.
        var current = JsonSerializer.SerializeToNode(obj);

        var copy = JsonSerializer.Deserialize<T>(current)!;
        change(copy);

        var changed = JsonSerializer.SerializeToNode(copy);

        var jsonPatch = current.CreatePatch(changed);
        return new V1Patch(jsonPatch, V1Patch.PatchType.JsonPatch);
    }

    public async Task StopResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping resource '{ResourceName}'...", resourceReference.DcpResourceName);

        var result = await DeleteResourceRetryPipeline.ExecuteAsync(async (resourceName, attemptCancellationToken) =>
        {
            var appResource = (AppResource)resourceReference;

            V1Patch patch;
            switch (appResource.DcpResource)
            {
                case Container c:
                    patch = CreatePatch(c, obj => obj.Spec.Stop = true);
                    await _kubernetesService.PatchAsync(c, patch, attemptCancellationToken).ConfigureAwait(false);
                    var cu = await _kubernetesService.GetAsync<Container>(c.Metadata.Name, cancellationToken: attemptCancellationToken).ConfigureAwait(false);
                    if (cu.Status?.State == ContainerState.Exited)
                    {
                        _logger.LogDebug("Container '{ResourceName}' was stopped.", resourceReference.DcpResourceName);
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug("Container '{ResourceName}' is still running; trying again to stop it...", resourceReference.DcpResourceName);
                        return false;
                    }

                case Executable e:
                    patch = CreatePatch(e, obj => obj.Spec.Stop = true);
                    await _kubernetesService.PatchAsync(e, patch, attemptCancellationToken).ConfigureAwait(false);
                    var eu = await _kubernetesService.GetAsync<Executable>(e.Metadata.Name, cancellationToken: attemptCancellationToken).ConfigureAwait(false);
                    if (eu.Status?.State == ExecutableState.Finished || eu.Status?.State == ExecutableState.Terminated)
                    {
                        _logger.LogDebug("Executable '{ResourceName}' was stopped.", resourceReference.DcpResourceName);
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug("Executable '{ResourceName}' is still running; trying again to stop it...", resourceReference.DcpResourceName);
                        return false;
                    }

                default:
                    throw new InvalidOperationException($"Unexpected resource type: {appResource.DcpResource.GetType().FullName}");
            }
        }, resourceReference.DcpResourceName, cancellationToken).ConfigureAwait(false);

        if (!result)
        {
            throw new InvalidOperationException($"Failed to stop resource '{resourceReference.DcpResourceName}'.");
        }
    }

    public IResourceReference GetResource(string resourceName)
    {
        var matchingResource = _appResources
            .Where(r => r.DcpResource is not Service)
            .SingleOrDefault(r => string.Equals(r.DcpResource.Metadata.Name, resourceName, StringComparisons.ResourceName));
        if (matchingResource == null)
        {
            throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        }

        return matchingResource;
    }

    public async Task StartResourceAsync(IResourceReference resourceReference, CancellationToken cancellationToken)
    {
        var appResource = (AppResource)resourceReference;
        var resourceType = GetResourceType(appResource.DcpResource, appResource.ModelResource);
        var resourceLogger = _loggerService.GetLogger(appResource.DcpResourceName);

        try
        {
            _logger.LogDebug("Starting {ResourceType} '{ResourceName}'.", resourceType, appResource.DcpResourceName);

            // Raise event after resource has been deleted. This is required because the event sets the status to "Starting" and resources being
            // deleted will temporarily override the status to a terminal state, such as "Exited".
            switch (appResource.DcpResource)
            {
                case Container c:
                    await EnsureResourceDeletedAsync<Container>(appResource.DcpResourceName).ConfigureAwait(false);

                    // Ensure we explicitly start the container
                    c.Spec.Start = true;

                    await _executorEvents.PublishAsync(new OnResourceStartingContext(cancellationToken, resourceType, appResource.ModelResource, appResource.DcpResourceName)).ConfigureAwait(false);
                    await CreateContainerAsync(appResource, resourceLogger, cancellationToken).ConfigureAwait(false);
                    break;
                case Executable e:
                    await EnsureResourceDeletedAsync<Executable>(appResource.DcpResourceName).ConfigureAwait(false);

                    await _executorEvents.PublishAsync(new OnResourceStartingContext(cancellationToken, resourceType, appResource.ModelResource, appResource.DcpResourceName)).ConfigureAwait(false);
                    await CreateExecutableAsync(appResource, resourceLogger, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected resource type: {appResource.DcpResource.GetType().FullName}");
            }
        }
        catch (FailedToApplyEnvironmentException)
        {
            // For this exception we don't want the noise of the stack trace, we've already
            // provided more detail where we detected the issue (e.g. envvar name). To get
            // more diagnostic information reduce logging level for DCP log category to Debug.
            await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, resourceType, appResource.ModelResource, appResource.DcpResourceName)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start resource {ResourceName}", appResource.ModelResource.Name);
            await _executorEvents.PublishAsync(new OnResourceFailedToStartContext(cancellationToken, resourceType, appResource.ModelResource, appResource.DcpResourceName)).ConfigureAwait(false);
            throw;
        }

        async Task EnsureResourceDeletedAsync<T>(string resourceName) where T : CustomResource
        {
            _logger.LogDebug("Ensuring '{ResourceName}' is deleted.", resourceName);

            var result = await DeleteResourceRetryPipeline.ExecuteAsync(async (resourceName, attemptCancellationToken) =>
            {
                string? uid = null;

                // Make deletion part of the retry loop--we have seen cases during test execution when
                // the deletion request completed with success code, but it was never "acted upon" by DCP.

                try
                {
                    var r = await _kubernetesService.DeleteAsync<T>(resourceName, cancellationToken: attemptCancellationToken).ConfigureAwait(false);
                    uid = r.Uid();

                    _logger.LogDebug("Delete request for '{ResourceName}' successfully completed. Resource to delete has UID '{Uid}'.", resourceName, uid);
                }
                catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Delete request for '{ResourceName}' returned NotFound.", resourceName);

                    // Not found means the resource is truly gone from the API server, which is our goal. Report success.
                    return true;
                }

                // Ensure resource is deleted. DeleteAsync returns before the resource is completely deleted so we must poll
                // to discover when it is safe to recreate the resource. This is required because the resources share the same name.
                // Deleting a resource might take a while (more than 10 seconds), because DCP tries to gracefully shut it down first
                // before resorting to more extreme measures.

                try
                {
                    _logger.LogDebug("Polling DCP to check if '{ResourceName}' is deleted...", resourceName);
                    var r = await _kubernetesService.GetAsync<T>(resourceName, cancellationToken: attemptCancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Get request for '{ResourceName}' returned resource with UID '{Uid}'.", resourceName, uid);

                    return false;
                }
                catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Get request for '{ResourceName}' returned NotFound.", resourceName);

                    // Success.
                    return true;
                }
            }, resourceName, cancellationToken).ConfigureAwait(false);

            if (!result)
            {
                throw new DistributedApplicationException($"Failed to delete '{resourceName}' successfully before restart.");
            }
        }
    }

    private async Task<(List<(string Value, bool IsSensitive)>, bool)> BuildArgsAsync(ILogger resourceLogger, IResource modelResource, CancellationToken cancellationToken)
    {
        var failedToApplyArgs = false;
        var args = new List<(string Value, bool IsSensitive)>();

        await modelResource.ProcessArgumentValuesAsync(
            _executionContext,
            (unprocessed, value, ex, isSensitive) =>
            {
                if (ex is not null)
                {
                    failedToApplyArgs = true;

                    resourceLogger.LogCritical(ex, "Failed to apply argument value '{ArgKey}'. A dependency may have failed to start.", ex.Data["ArgKey"]);
                    _logger.LogDebug(ex, "Failed to apply argument value '{ArgKey}' to '{ResourceName}'. A dependency may have failed to start.", ex.Data["ArgKey"], modelResource.Name);
                }
                else if (value is { } argument)
                {
                    args.Add((argument, isSensitive));
                }
            },
            resourceLogger,
            DefaultContainerHostName,
            cancellationToken).ConfigureAwait(false);

        return (args, failedToApplyArgs);
    }

    private async Task<List<ContainerCreateFileSystem>> BuildCreateFilesAsync(IResource modelResource, CancellationToken cancellationToken)
    {
        var createFiles = new List<ContainerCreateFileSystem>();

        if (modelResource.TryGetAnnotationsOfType<ContainerFileSystemCallbackAnnotation>(out var createFileAnnotations))
        {
            foreach (var a in createFileAnnotations)
            {
                var entries = await a.Callback(
                    new()
                    {
                        Model = modelResource,
                        ServiceProvider = _executionContext.ServiceProvider
                    },
                    cancellationToken).ConfigureAwait(false);

                createFiles.Add(new ContainerCreateFileSystem
                {
                    Destination = a.DestinationPath,
                    DefaultOwner = a.DefaultOwner,
                    DefaultGroup = a.DefaultGroup,
                    Umask = (int?)a.Umask,
                    Entries = entries.Select(e => e.ToContainerFileSystemEntry()).ToList(),
                });
            }
        }

        return createFiles;
    }

    private async Task<(List<EnvVar>, bool)> BuildEnvVarsAsync(ILogger resourceLogger, IResource modelResource, CancellationToken cancellationToken)
    {
        var failedToApplyConfiguration = false;
        var env = new List<EnvVar>();

        await modelResource.ProcessEnvironmentVariableValuesAsync(
            _executionContext,
            (key, unprocessed, value, ex) =>
            {
                if (ex is not null)
                {
                    failedToApplyConfiguration = true;
                    resourceLogger.LogCritical(ex, "Failed to apply environment variable '{Name}'. A dependency may have failed to start.", key);
                    _logger.LogDebug(ex, "Failed to apply environment variable '{Name}' to '{ResourceName}'. A dependency may have failed to start.", key, modelResource.Name);
                }
                else if (value is string s)
                {
                    env.Add(new EnvVar { Name = key, Value = s });
                }
            },
            resourceLogger,
            DefaultContainerHostName,
            cancellationToken).ConfigureAwait(false);

        return (env, failedToApplyConfiguration);
    }

    private async Task<(List<string>, bool)> BuildRunArgsAsync(ILogger resourceLogger, IResource modelResource, CancellationToken cancellationToken)
    {
        var failedToApplyArgs = false;
        var runArgs = new List<string>();

        await modelResource.ProcessContainerRuntimeArgValues(
            (a, ex) =>
            {
                if (ex is not null)
                {
                    failedToApplyArgs = true;
                    resourceLogger.LogCritical(ex, "Failed to apply argument value '{ArgKey}'. A dependency may have failed to start.", a);
                    _logger.LogDebug(ex, "Failed to apply argument value '{ArgKey}' to '{ResourceName}'. A dependency may have failed to start.", a, modelResource.Name);
                }
                else if (a is string s)
                {
                    runArgs.Add(s);
                }
            },
            resourceLogger,
            DefaultContainerHostName,
            cancellationToken).ConfigureAwait(false);

        return (runArgs, failedToApplyArgs);
    }

    private static List<ContainerPortSpec> BuildContainerPorts(AppResource cr)
    {
        var ports = new List<ContainerPortSpec>();

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
                    portSpec.Protocol = PortProtocol.TCP;
                    break;
                case ProtocolType.Udp:
                    portSpec.Protocol = PortProtocol.UDP;
                    break;
            }

            if (sp.EndpointAnnotation.TargetHost != "localhost")
            {
                portSpec.HostIP = sp.EndpointAnnotation.TargetHost;
            }

            ports.Add(portSpec);
        }

        return ports;
    }

    private static List<VolumeMount> BuildContainerMounts(IResource container)
    {
        var volumeMounts = new List<VolumeMount>();

        if (container.TryGetContainerMounts(out var containerMounts))
        {
            foreach (var mount in containerMounts)
            {
                var volumeSpec = new VolumeMount
                {
                    Source = mount.Source,
                    Target = mount.Target,
                    Type = mount.Type == ContainerMountType.BindMount ? VolumeMountType.Bind : VolumeMountType.Volume,
                    IsReadOnly = mount.IsReadOnly
                };

                volumeMounts.Add(volumeSpec);
            }
        }

        return volumeMounts;
    }
}
