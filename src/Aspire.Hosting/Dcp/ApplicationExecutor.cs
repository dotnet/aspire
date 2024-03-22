// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
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
    public ServiceProducerAnnotation DcpServiceProducerAnnotation { get; }

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
        DcpServiceProducerAnnotation = new(service.Metadata.Name);
    }
}

internal sealed class ApplicationExecutor(ILogger<ApplicationExecutor> logger,
                                          ILogger<DistributedApplication> distributedApplicationLogger,
                                          DistributedApplicationModel model,
                                          DistributedApplicationOptions distributedApplicationOptions,
                                          IKubernetesService kubernetesService,
                                          IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
                                          IConfiguration configuration,
                                          IOptions<DcpOptions> options,
                                          IDashboardEndpointProvider dashboardEndpointProvider,
                                          DistributedApplicationExecutionContext executionContext,
                                          ResourceNotificationService notificationService,
                                          ResourceLoggerService loggerService,
                                          IDcpDependencyCheckService dcpDependencyCheckService)
{
    private const string DebugSessionPortVar = "DEBUG_SESSION_PORT";

    private readonly ILogger<ApplicationExecutor> _logger = logger;
    private readonly DistributedApplicationModel _model = model;
    private readonly Dictionary<string, IResource> _applicationModel = model.Resources.ToDictionary(r => r.Name);
    private readonly ILookup<IResource?, IResourceWithParent> _parentChildLookup = GetParentChildLookup(model);
    private readonly IDistributedApplicationLifecycleHook[] _lifecycleHooks = lifecycleHooks.ToArray();
    private readonly IOptions<DcpOptions> _options = options;
    private readonly IDashboardEndpointProvider _dashboardEndpointProvider = dashboardEndpointProvider;
    private readonly DistributedApplicationExecutionContext _executionContext = executionContext;
    private readonly List<AppResource> _appResources = [];

    private readonly ConcurrentDictionary<string, Container> _containersMap = [];
    private readonly ConcurrentDictionary<string, Executable> _executablesMap = [];
    private readonly ConcurrentDictionary<string, Service> _servicesMap = [];
    private readonly ConcurrentDictionary<string, Endpoint> _endpointsMap = [];
    private readonly ConcurrentDictionary<(string, string), List<string>> _resourceAssociatedServicesMap = [];
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _logStreams = new();
    private readonly ConcurrentDictionary<IResource, bool> _hiddenResources = new();
    private DcpInfo? _dcpInfo;

    private string DefaultContainerHostName => configuration["AppHost:ContainerHostname"] ?? _dcpInfo?.Containers?.ContainerHostName ?? "host.docker.internal";

    public async Task RunApplicationAsync(CancellationToken cancellationToken = default)
    {
        AspireEventSource.Instance.DcpModelCreationStart();

        _dcpInfo = await dcpDependencyCheckService.GetDcpInfoAsync(cancellationToken).ConfigureAwait(false);

        Debug.Assert(_dcpInfo is not null, "DCP info should not be null at this point");

        try
        {
            if (!distributedApplicationOptions.DisableDashboard)
            {
                if (_model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is not { } dashboardResource)
                {
                    // No dashboard is specified, so start one.
                    await StartDashboardAsDcpExecutableAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    ConfigureAspireDashboardResource(dashboardResource);
                }
            }
            PrepareServices();
            PrepareContainers();
            PrepareExecutables();

            await PublishResourcesWithInitialStateAsync().ConfigureAwait(false);

            // Watch for changes to the resource state.
            WatchResourceChanges(cancellationToken);

            await CreateServicesAsync(cancellationToken).ConfigureAwait(false);

            await CreateContainersAndExecutablesAsync(cancellationToken).ConfigureAwait(false);

            foreach (var lifecycleHook in _lifecycleHooks)
            {
                await lifecycleHook.AfterResourcesCreatedAsync(_model, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            AspireEventSource.Instance.DcpModelCreationStop();
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

    private void WatchResourceChanges(CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(1);

        Task.Run(
            async () =>
            {
                using (semaphore)
                {
                    await Task.WhenAll(
                        Task.Run(() => WatchKubernetesResource<Executable>((t, r) => ProcessResourceChange(t, r, _executablesMap, "Executable", ToSnapshot)), cancellationToken),
                        Task.Run(() => WatchKubernetesResource<Container>((t, r) => ProcessResourceChange(t, r, _containersMap, "Container", ToSnapshot)), cancellationToken),
                        Task.Run(() => WatchKubernetesResource<Service>(ProcessServiceChange), cancellationToken),
                        Task.Run(() => WatchKubernetesResource<Endpoint>(ProcessEndpointChange), cancellationToken)).ConfigureAwait(false);
                }
            },
            cancellationToken);

        async Task WatchKubernetesResource<T>(Func<WatchEventType, T, Task> handler) where T : CustomResource
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
                await pipeline.ExecuteAsync(async (pipelineCancellationToken) =>
                {
                    _logger.LogDebug("Starting watch over DCP {ResourceType} resources", typeof(T).Name);

                    await foreach (var (eventType, resource) in kubernetesService.WatchAsync<T>(cancellationToken: pipelineCancellationToken))
                    {
                        await semaphore.WaitAsync(pipelineCancellationToken).ConfigureAwait(false);

                        try
                        {
                            await handler(eventType, resource).ConfigureAwait(false);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogCritical(ex, "Watch task over kubernetes {ResourceType} resources terminated unexpectedly. Check to ensure dcpd process is running.", typeof(T).Name);
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
                    if (_logStreams.TryRemove(resource.Metadata.Name, out var cts))
                    {
                        cts.Cancel();
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

                    if (_hiddenResources.TryAdd(appModelResource, true))
                    {
                        // Hide the application model resource because we have the DCP resource
                        await notificationService.PublishUpdateAsync(appModelResource, s => s with { State = "Hidden" }).ConfigureAwait(false);
                    }

                    // Notifications are associated with the application model resource, so we need to update with that context
                    await notificationService.PublishUpdateAsync(appModelResource, resource.Metadata.Name, s => snapshotFactory(resource, s)).ConfigureAwait(false);

                    StartLogStream(resource);
                }

                // Update all child resources of containers
                if (resource is Container c && c.Status?.State is string state)
                {
                    await SetChildResourceStateAsync(appModelResource, state).ConfigureAwait(false);
                }
            }
            else
            {
                // No application model resource found for the DCP resource. This should only happen for the dashboard.
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
            var cts = new CancellationTokenSource();

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

                    await foreach (var batch in enumerable.WithCancellation(cts.Token))
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
                finally
                {
                    // Complete the log stream
                    loggerService.Complete(resource.Metadata.Name);
                }
            },
            cts.Token);

            return cts;
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

        return previous with
        {
            ResourceType = KnownResourceTypes.Container,
            State = container.Status?.State,
            // Map a container exit code of -1 (unknown) to null
            ExitCode = container.Status?.ExitCode is null or Conventions.UnknownExitCode ? null : container.Status.ExitCode,
            Properties = [
                (KnownProperties.Container.Image, container.Spec.Image),
                (KnownProperties.Container.Id, containerId),
                (KnownProperties.Container.Command, container.Spec.Command),
                (KnownProperties.Container.Args, container.Status?.EffectiveArgs ?? []),
                (KnownProperties.Container.Ports, GetPorts()),
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
        if (executable.TryGetProjectLaunchConfiguration(out var projectLaunchConfiguration))
        {
            projectPath = projectLaunchConfiguration.ProjectPath;
        }
        else
        {
#pragma warning disable CS0612 // CSharpProjectPathAnnotation is obsolete; remove in Aspire Preview 6
            executable.Metadata.Annotations?.TryGetValue(Executable.CSharpProjectPathAnnotation, out projectPath);
#pragma warning restore CS0612
        }

        var urls = GetUrls(executable);

        var environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env);

        if (projectPath is not null)
        {
            return previous with
            {
                ResourceType = KnownResourceTypes.Project,
                State = executable.Status?.State,
                ExitCode = executable.Status?.ExitCode,
                Properties = [
                    (KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                    (KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                    (KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []),
                    (KnownProperties.Executable.Pid, executable.Status?.ProcessId),
                    (KnownProperties.Project.Path, projectPath)
                ],
                EnvironmentVariables = environment,
                CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
                Urls = urls
            };
        }

        return previous with
        {
            ResourceType = KnownResourceTypes.Executable,
            State = executable.Status?.State,
            ExitCode = executable.Status?.ExitCode,
            Properties = [
                (KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                (KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                (KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []),
                (KnownProperties.Executable.Pid, executable.Status?.ProcessId)
            ],
            EnvironmentVariables = environment,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            Urls = urls
        };
    }

    private ImmutableArray<(string Name, string Url, bool IsInternal)> GetUrls(CustomResource resource)
    {
        var name = resource.Metadata.Name;

        var urls = ImmutableArray.CreateBuilder<(string Name, string Url, bool IsInternal)>();

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
                    p.GetEffectiveLaunchProfile() is LaunchProfile profile &&
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

                        urls.Add(new(ep.EndpointName, url, false));
                    }
                }
                else
                {
                    if (ep.IsAllocated)
                    {
                        urls.Add(new(ep.EndpointName, ep.Url, false));
                    }
                }

                if (ep.EndpointAnnotation.IsProxied)
                {
                    var endpointString = $"{ep.Scheme}://{endpoint.Spec.Address}:{endpoint.Spec.Port}";
                    urls.Add(new($"{ep.EndpointName}-listen-port", endpointString, true));
                }
            }
        }

        return urls.ToImmutable();
    }

    private static ImmutableArray<(string Name, string Value, bool IsFromSpec)> GetEnvironmentVariables(List<EnvVar>? effectiveSource, List<EnvVar>? specSource)
    {
        if (effectiveSource is null or { Count: 0 })
        {
            return [];
        }

        var environment = ImmutableArray.CreateBuilder<(string Name, string Value, bool IsFromSpec)>(effectiveSource.Count);

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

    private void ConfigureAspireDashboardResource(IResource dashboardResource)
    {
        // Don't publish the resource to the manifest.
        dashboardResource.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        // Remove endpoint annotations because we are directly configuring
        // the dashboard app (it doesn't go through the proxy!).
        var endpointAnnotations = dashboardResource.Annotations.OfType<EndpointAnnotation>().ToList();
        foreach (var endpointAnnotation in endpointAnnotations)
        {
            dashboardResource.Annotations.Remove(endpointAnnotation);
        }

        dashboardResource.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
        {
            if (configuration["ASPNETCORE_URLS"] is not { } appHostApplicationUrl)
            {
                throw new DistributedApplicationException("Failed to configure dashboard resource because ASPNETCORE_URLS environment variable was not set.");
            }

            if (configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] is not { } otlpEndpointUrl)
            {
                throw new DistributedApplicationException("Failed to configure dashboard resource because DOTNET_DASHBOARD_OTLP_ENDPOINT_URL environment variable was not set.");
            }

            // Grab the resource service URL. We need to inject this into the resource.

            var grpcEndpointUrl = await _dashboardEndpointProvider.GetResourceServiceUriAsync(context.CancellationToken).ConfigureAwait(false);

            context.EnvironmentVariables["ASPNETCORE_URLS"] = appHostApplicationUrl;
            context.EnvironmentVariables["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"] = grpcEndpointUrl;
            context.EnvironmentVariables["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpEndpointUrl;

            if (configuration["AppHost:OtlpApiKey"] is { } otlpApiKey)
            {
                context.EnvironmentVariables["DOTNET_DASHBOARD_OTLP_AUTH_MODE"] = "ApiKey"; // Matches value in OtlpAuthMode enum.
                context.EnvironmentVariables["DOTNET_DASHBOARD_OTLP_API_KEY"] = otlpApiKey;
            }
            else
            {
                context.EnvironmentVariables["DOTNET_DASHBOARD_OTLP_AUTH_MODE"] = "None"; // Matches value in OtlpAuthMode enum.
            }
        }));
    }

    private async Task StartDashboardAsDcpExecutableAsync(CancellationToken cancellationToken = default)
    {
        if (!distributedApplicationOptions.DashboardEnabled)
        {
            // The dashboard is disabled. Do nothing.
            return;
        }

        if (_options.Value.DashboardPath is not { } dashboardPath)
        {
            throw new DistributedApplicationException("Dashboard path empty or file does not exist.");
        }

        var fullyQualifiedDashboardPath = Path.GetFullPath(dashboardPath);
        var dashboardWorkingDirectory = Path.GetDirectoryName(fullyQualifiedDashboardPath);

        var dashboardExecutableSpec = new ExecutableSpec
        {
            ExecutionType = ExecutionType.Process,
            WorkingDirectory = dashboardWorkingDirectory
        };

        if (string.Equals(".dll", Path.GetExtension(fullyQualifiedDashboardPath), StringComparison.OrdinalIgnoreCase))
        {
            // The dashboard path is a DLL, so run it with `dotnet <dll>`
            dashboardExecutableSpec.ExecutablePath = "dotnet";
            dashboardExecutableSpec.Args = [fullyQualifiedDashboardPath];
        }
        else
        {
            // Assume the dashboard path is directly executable
            dashboardExecutableSpec.ExecutablePath = fullyQualifiedDashboardPath;
        }

        var grpcEndpointUrl = await _dashboardEndpointProvider.GetResourceServiceUriAsync(cancellationToken).ConfigureAwait(false);

        // Matches DashboardWebApplication.DashboardUrlDefaultValue
        const string defaultDashboardUrl = "http://localhost:18888";

        var otlpEndpointUrl = configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"];
        var dashboardUrls = configuration["ASPNETCORE_URLS"] ?? defaultDashboardUrl;
        var aspnetcoreEnvironment = configuration["ASPNETCORE_ENVIRONMENT"];

        dashboardExecutableSpec.Env =
        [
            new()
            {
                Name = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL",
                Value = grpcEndpointUrl
            },
            new()
            {
                Name = "ASPNETCORE_URLS",
                Value = dashboardUrls
            },
            new()
            {
                Name = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL",
                Value = otlpEndpointUrl
            },
            new()
            {
                Name = "ASPNETCORE_ENVIRONMENT",
                Value = aspnetcoreEnvironment
            }
        ];

        if (configuration["AppHost:OtlpApiKey"] is { } otlpApiKey)
        {
            dashboardExecutableSpec.Env.AddRange([
                new()
                {
                    Name = "DOTNET_DASHBOARD_OTLP_API_KEY",
                    Value = otlpApiKey
                },
                new()
                {
                    Name = "DOTNET_DASHBOARD_OTLP_AUTH_MODE",
                    Value = "ApiKey" // Matches value in OtlpAuthMode enum.
                }
            ]);
        }
        else
        {
            dashboardExecutableSpec.Env.AddRange([
                new()
                {
                    Name = "DOTNET_DASHBOARD_OTLP_AUTH_MODE",
                    Value = "None" // Matches value in OtlpAuthMode enum.
                }
            ]);
        }

        var dashboardExecutable = new Executable(dashboardExecutableSpec)
        {
            Metadata = { Name = KnownResourceNames.AspireDashboard }
        };

        await kubernetesService.CreateAsync(dashboardExecutable, cancellationToken).ConfigureAwait(false);
        PrintDashboardUrls(dashboardUrls);
    }

    private void PrintDashboardUrls(string delimitedUrlList)
    {
        if (StringUtils.TryGetUriFromDelimitedString(delimitedUrlList, ";", out var firstDashboardUrl))
        {
            distributedApplicationLogger.LogInformation("Now listening on: {DashboardUrl}", firstDashboardUrl.ToString().TrimEnd('/'));
        }
    }

    private async Task CreateServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AspireEventSource.Instance.DcpServicesCreationStart();

            var needAddressAllocated = _appResources.OfType<ServiceAppResource>().Where(sr => !sr.Service.HasCompleteAddress).ToList();

            await CreateResourcesAsync<Service>(cancellationToken).ConfigureAwait(false);

            if (needAddressAllocated.Count == 0)
            {
                // No need to wait for any updates to Service objects from the orchestrator.
                return;
            }

            // We do not specify the initial list version, so the watcher will give us all updates to Service objects.
            IAsyncEnumerable<(WatchEventType, Service)> serviceChangeEnumerator = kubernetesService.WatchAsync<Service>(cancellationToken: cancellationToken);
            await foreach (var (evt, updated) in serviceChangeEnumerator)
            {
                if (evt == WatchEventType.Bookmark) { continue; } // Bookmarks do not contain any data.

                var srvResource = needAddressAllocated.Where(sr => sr.Service.Metadata.Name == updated.Metadata.Name).FirstOrDefault();
                if (srvResource == null) { continue; } // This service most likely already has full address information, so it is not on needAddressAllocated list.

                if (updated.HasCompleteAddress || updated.Spec.AddressAllocationMode == AddressAllocationModes.Proxyless)
                {
                    srvResource.Service.ApplyAddressInfoFrom(updated);
                    needAddressAllocated.Remove(srvResource);
                }

                if (needAddressAllocated.Count == 0)
                {
                    return; // We are done
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
                    containerHostAddress: appResource.ModelResource.IsContainer() ? containerHost : null);
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

                var port = _options.Value.RandomizePorts is true && endpoint.IsProxied ? null : endpoint.Port;
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
            var exeName = GetObjectNameForResource(executable);
            var exePath = executable.Command;
            var exe = Executable.Create(exeName, exePath);

            // The working directory is always relative to the app host project directory (if it exists).
            exe.Spec.WorkingDirectory = executable.WorkingDirectory;
            exe.Spec.ExecutionType = ExecutionType.Process;
            exe.Annotate(CustomResource.OtelServiceNameAnnotation, exe.Metadata.Name);
            exe.Annotate(CustomResource.ResourceNameAnnotation, executable.Name);

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
            annotationHolder.Annotate(CustomResource.ResourceNameAnnotation, project.Name);

            var projectLaunchConfiguration = new ProjectLaunchConfiguration();
            projectLaunchConfiguration.ProjectPath = projectMetadata.ProjectPath;

            if (!string.IsNullOrEmpty(configuration[DebugSessionPortVar]))
            {
                exeSpec.ExecutionType = ExecutionType.IDE;

                if (_dcpInfo?.Version?.CompareTo(DcpVersion.MinimumVersionIdeProtocolV1) >= 0)
                {
                    projectLaunchConfiguration.DisableLaunchProfile = project.TryGetLastAnnotation<ExcludeLaunchProfileAnnotation>(out _);
                    if (project.TryGetLastAnnotation<LaunchProfileAnnotation>(out var lpa))
                    {
                        projectLaunchConfiguration.LaunchProfile = lpa.LaunchProfileName;
                    }
                }
                else
                {
#pragma warning disable CS0612 // These annotations are obsolete; remove in Aspire Preview 6
                    annotationHolder.Annotate(Executable.CSharpProjectPathAnnotation, projectMetadata.ProjectPath);

                    // ExcludeLaunchProfileAnnotation takes precedence over LaunchProfileAnnotation.
                    if (project.TryGetLastAnnotation<ExcludeLaunchProfileAnnotation>(out _))
                    {
                        annotationHolder.Annotate(Executable.CSharpDisableLaunchProfileAnnotation, "true");
                    }
                    else if (project.TryGetLastAnnotation<LaunchProfileAnnotation>(out var lpa))
                    {
                        annotationHolder.Annotate(Executable.CSharpLaunchProfileAnnotation, lpa.LaunchProfileName);
                    }
#pragma warning restore CS0612
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

                // We pretty much always want to suppress the normal launch profile handling
                // because the settings from the profile will override the ambient environment settings, which is not what we want
                // (the ambient environment settings for service processes come from the application model
                // and should be HIGHER priority than the launch profile settings).
                // This means we need to apply the launch profile settings manually--the invocation parameters here,
                // and the environment variables/application URLs inside CreateExecutableAsync().
                exeSpec.Args.Add("--no-launch-profile");

                var launchProfileName = project.SelectLaunchProfileName();
                if (!string.IsNullOrEmpty(launchProfileName))
                {
                    var launchProfile = project.GetEffectiveLaunchProfile();
                    if (launchProfile is not null && !string.IsNullOrWhiteSpace(launchProfile.CommandLineArgs))
                    {
                        var cmdArgs = launchProfile.CommandLineArgs.Split((string?)null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (cmdArgs is not null && cmdArgs.Length > 0)
                        {
                            exeSpec.Args.Add("--");
                            exeSpec.Args.AddRange(cmdArgs);
                        }
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

    private Task CreateExecutablesAsync(IEnumerable<AppResource> executableResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpExecutablesCreateStart();

            // Hoisting the aspire-dashboard resource if it exists to the top of
            // the list so we start it first.
            var sortedExecutableResources = executableResources.ToList();
            var (dashboardIndex, dashboardAppResource) = sortedExecutableResources.IndexOf(static r => StringComparers.ResourceName.Equals(r.ModelResource.Name, KnownResourceNames.AspireDashboard));

            if (dashboardIndex > 0)
            {
                sortedExecutableResources.RemoveAt(dashboardIndex);
                sortedExecutableResources.Insert(0, dashboardAppResource);
            }

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
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create resource {ResourceName}", cr.ModelResource.Name);

                    await notificationService.PublishUpdateAsync(cr.ModelResource, s => s with { State = "FailedToStart" }).ConfigureAwait(false);
                }
            }

            var tasks = new List<Task>();
            foreach (var er in sortedExecutableResources)
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
        }

        var config = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(_executionContext, config, cancellationToken)
        {
            Logger = resourceLogger
        };

        // Need to apply configuration settings manually; see PrepareExecutables() for details.
        if (er.ModelResource is ProjectResource project && project.SelectLaunchProfileName() is { } launchProfileName && project.GetLaunchSettings() is { } launchSettings)
        {
            ApplyLaunchProfile(er, config, launchProfileName, launchSettings);
        }
        else
        {
            if (er.ServicesProduced.Count > 0)
            {
                if (er.ModelResource is ProjectResource)
                {
                    var urls = er.ServicesProduced.Where(IsUnspecifiedHttpService).Select(sar =>
                    {
                        var url = sar.EndpointAnnotation.UriScheme + "://localhost:{{- portForServing \"" + sar.Service.Metadata.Name + "\" -}}";
                        return url;
                    });

                    // REVIEW: Should we assume ASP.NET Core?
                    // We're going to use http and https urls as ASPNETCORE_URLS
                    config["ASPNETCORE_URLS"] = string.Join(";", urls);
                }

                InjectPortEnvVars(er, config);
            }
        }

        if (er.ModelResource.TryGetEnvironmentVariables(out var envVarAnnotations))
        {
            foreach (var ann in envVarAnnotations)
            {
                await ann.Callback(context).ConfigureAwait(false);
            }
        }

        spec.Env = [];
        foreach (var c in config)
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

        await createResource().ConfigureAwait(false);

        // NOTE: This check is only necessary for the inner loop in the dotnet/aspire repo. When
        //       running in the dotnet/aspire repo we will normally launch the dashboard via
        //       AddProject<T>. When doing this we make sure that the dashboard is running.
        if (!distributedApplicationOptions.DisableDashboard && er.ModelResource.Name.Equals(KnownResourceNames.AspireDashboard, StringComparisons.ResourceName))
        {
            // We just check the HTTP endpoint because this will prove that the
            // dashboard is listening and is ready to process requests.
            if (configuration["ASPNETCORE_URLS"] is not { } dashboardUrls)
            {
                throw new DistributedApplicationException("Cannot check dashboard availability since ASPNETCORE_URLS environment variable not set.");
            }

            PrintDashboardUrls(dashboardUrls);
        }
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

    private static void ApplyLaunchProfile(AppResource executableResource, Dictionary<string, object> config, string launchProfileName, LaunchSettings launchSettings)
    {
        // Populate DOTNET_LAUNCH_PROFILE environment variable for consistency with "dotnet run" and "dotnet watch".
        config.Add("DOTNET_LAUNCH_PROFILE", launchProfileName);

        var launchProfile = launchSettings.Profiles[launchProfileName];
        if (!string.IsNullOrWhiteSpace(launchProfile.ApplicationUrl))
        {
            if (executableResource.DcpResource is ExecutableReplicaSet)
            {
                var urls = executableResource.ServicesProduced.Where(IsUnspecifiedHttpService).Select(sar =>
                {
                    var url = sar.EndpointAnnotation.UriScheme + "://localhost:{{- portForServing \"" + sar.Service.Metadata.Name + "\" -}}";
                    return url;
                });

                config.Add("ASPNETCORE_URLS", string.Join(";", urls));
            }
            else
            {
                config.Add("ASPNETCORE_URLS", launchProfile.ApplicationUrl);
            }

            InjectPortEnvVars(executableResource, config);
        }

        foreach (var envVar in launchProfile.EnvironmentVariables)
        {
            string value = Environment.ExpandEnvironmentVariables(envVar.Value);
            config[envVar.Key] = value;
        }
    }

    private static void InjectPortEnvVars(AppResource executableResource, Dictionary<string, object> config)
    {
        ServiceAppResource? httpsServiceAppResource = null;
        // Inject environment variables for services produced by this executable.
        foreach (var serviceProduced in executableResource.ServicesProduced)
        {
            var name = serviceProduced.Service.Metadata.Name;
            var envVar = serviceProduced.EndpointAnnotation.EnvironmentVariable;

            if (envVar is not null)
            {
                config.Add(envVar, $"{{{{- portForServing \"{name}\" }}}}");
            }

            if (httpsServiceAppResource is null && serviceProduced.EndpointAnnotation.UriScheme == "https")
            {
                httpsServiceAppResource = serviceProduced;
            }
        }

        // REVIEW: If you run as an executable, we don't know that you're an ASP.NET Core application so we don't want to
        // inject ASPNETCORE_HTTPS_PORT.
        if (executableResource.ModelResource is ProjectResource)
        {
            // Add the environment variable for the HTTPS port if we have an HTTPS service. This will make sure the
            // HTTPS redirection middleware avoids redirecting to the internal port.
            if (httpsServiceAppResource is not null)
            {
                config.Add("ASPNETCORE_HTTPS_PORT", $"{{{{- portFor \"{httpsServiceAppResource.Service.Metadata.Name}\" }}}}");
            }
        }
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

            var ctr = Container.Create(GetObjectNameForResource(container), containerImageName);

            ctr.Annotate(CustomResource.ResourceNameAnnotation, container.Name);
            ctr.Annotate(CustomResource.OtelServiceNameAnnotation, container.Name);

            if (container.TryGetContainerMounts(out var containerMounts))
            {
                ctr.Spec.VolumeMounts = [];

                foreach (var mount in containerMounts)
                {
                    var isBindMount = mount.Type == ContainerMountType.BindMount;
                    var resolvedSource = mount.Source;
                    if (isBindMount)
                    {
                        // Source is only optional for creating anonymous volume mounts.
                        if (mount.Source == null)
                        {
                            throw new InvalidDataException($"Bind mount for container '{container.Name}' is missing required source.");
                        }

                        if (!Path.IsPathRooted(mount.Source))
                        {
                            resolvedSource = Path.GetFullPath(mount.Source);
                        }
                    }

                    var volumeSpec = new VolumeMount
                    {
                        Source = resolvedSource,
                        Target = mount.Target,
                        Type = isBindMount ? VolumeMountType.Bind : VolumeMountType.Volume,
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
                        (KnownProperties.Container.Image, cr.ModelResource.TryGetContainerImageName(out var imageName) ? imageName : ""),
                   ],
                    ResourceType = KnownResourceTypes.Container
                })
                .ConfigureAwait(false);

                await SetChildResourceStateAsync(cr.ModelResource, "Starting").ConfigureAwait(false);

                try
                {
                    await CreateContainerAsync(cr, logger, cancellationToken).ConfigureAwait(false);
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

        var config = new Dictionary<string, object>();

        dcpContainerResource.Spec.Env = [];

        if (cr.ServicesProduced.Count > 0)
        {
            dcpContainerResource.Spec.Ports = new();

            foreach (var sp in cr.ServicesProduced)
            {
                var portSpec = new ContainerPortSpec()
                {
                    ContainerPort = sp.DcpServiceProducerAnnotation.Port,
                };

                if (!sp.EndpointAnnotation.IsProxied)
                {
                    // When DCP isn't proxying the container we need to set the host port that the containers internal port will be mapped to
                    portSpec.HostPort = sp.EndpointAnnotation.Port;
                }

                if (!string.IsNullOrEmpty(sp.DcpServiceProducerAnnotation.Address))
                {
                    portSpec.HostIP = sp.DcpServiceProducerAnnotation.Address;
                }

                switch (sp.EndpointAnnotation.Protocol)
                {
                    case ProtocolType.Tcp:
                        portSpec.Protocol = PortProtocol.TCP; break;
                    case ProtocolType.Udp:
                        portSpec.Protocol = PortProtocol.UDP; break;
                }

                dcpContainerResource.Spec.Ports.Add(portSpec);

                var name = sp.Service.Metadata.Name;
                var envVar = sp.EndpointAnnotation.EnvironmentVariable;

                if (envVar is not null)
                {
                    config.Add(envVar, $"{{{{- portForServing \"{name}\" }}}}");
                }
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

        foreach (var kvp in config)
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
        }

        if (modelContainerResource is ContainerResource containerResource)
        {
            dcpContainerResource.Spec.Command = containerResource.Entrypoint;
        }

        await kubernetesService.CreateAsync(dcpContainerResource, cancellationToken).ConfigureAwait(false);
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
            // Projects/Executables have their ports auto-allocated; the port specified by the EndpointAnnotation
            // is applied to the Service objects and used by clients.
            // Containers use the port from the EndpointAnnotation directly.

            if (modelResource.IsContainer())
            {
                if (sp.EndpointAnnotation.ContainerPort is null)
                {
                    throw new InvalidOperationException($"The endpoint for container resource {modelResourceName} must specify the ContainerPort");
                }

                sp.DcpServiceProducerAnnotation.Port = sp.EndpointAnnotation.ContainerPort;
            }
            else if (!sp.EndpointAnnotation.IsProxied)
            {
                if (appResource.DcpResource is ExecutableReplicaSet ers && ers.Spec.Replicas > 1)
                {
                    throw new InvalidOperationException($"'{modelResourceName}' specifies multiple replicas and at least one proxyless endpoint. These features do not work together.");
                }

                // DCP will not allocate a port for this proxyless service
                // so we need to specify what the port is so DCP is aware of it.
                sp.DcpServiceProducerAnnotation.Port = sp.EndpointAnnotation.Port;
            }

            dcpResource.AnnotateAsObjectList(CustomResource.ServiceProducerAnnotation, sp.DcpServiceProducerAnnotation);
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
                (false, true) => $"{s}_{localSuffix}",
                (true, false) => $"{s}_{globalSuffix}",
                (false, false) => $"{s}_{localSuffix}_{globalSuffix}"
            };
        return maybeWithSuffix(resource.Name, suffix, _options.Value.ResourceNameSuffix);
    }

    private static string GenerateUniqueServiceName(HashSet<string> serviceNames, string candidateName)
    {
        int suffix = 1;
        string uniqueName = candidateName;

        while (!serviceNames.Add(uniqueName))
        {
            uniqueName = $"{candidateName}_{suffix}";
            suffix++;
            if (suffix == 100)
            {
                // Should never happen, but we do not want to ever get into a infinite loop situation either.
                throw new ArgumentException($"Could not generate a unique name for service '{candidateName}'");
            }
        }

        return uniqueName;
    }

    // Returns true if this resource represents an HTTP service endpoint which does not specify an environment variable for the endpoint.
    // This is used to decide whether the endpoint should be propagated via the ASPNETCORE_URLS environment variable.
    private static bool IsUnspecifiedHttpService(ServiceAppResource serviceAppResource)
    {
        return serviceAppResource.EndpointAnnotation is
        {
            UriScheme: "http" or "https",
            EnvironmentVariable: null or { Length: 0 }
        };
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
