// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Pulls data about resources from DCP's kubernetes API. Streams updates to consumers.
/// </summary>
/// <remarks>
/// DCP data is obtained from <see cref="KubernetesService"/>. <see cref="DistributedApplicationModel"/>
/// is also used for mapping some project data.
/// </remarks>
internal sealed class DcpDataSource
{
    private readonly IKubernetesService _kubernetesService;
    private readonly IReadOnlyDictionary<string, IResource> _applicationModel;
    private readonly ConcurrentDictionary<string, ResourceSnapshot> _placeHolderResources = [];
    private readonly Func<ResourceSnapshot, ResourceSnapshotChangeType, ValueTask> _onResourceChanged;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, Container> _containersMap = [];
    private readonly ConcurrentDictionary<string, Executable> _executablesMap = [];
    private readonly ConcurrentDictionary<string, Service> _servicesMap = [];
    private readonly ConcurrentDictionary<string, Endpoint> _endpointsMap = [];
    private readonly ConcurrentDictionary<(string, string), List<string>> _resourceAssociatedServicesMap = [];

    public DcpDataSource(
        IKubernetesService kubernetesService,
        IReadOnlyDictionary<string, IResource> applicationModel,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        Func<ResourceSnapshot, ResourceSnapshotChangeType, ValueTask> onResourceChanged,
        CancellationToken cancellationToken)
    {
        _kubernetesService = kubernetesService;
        _applicationModel = applicationModel;
        _onResourceChanged = onResourceChanged;

        _logger = loggerFactory.CreateLogger<DcpDataSource>();

        var semaphore = new SemaphoreSlim(1);

        Task.Run(
            async () =>
            {
                // Show all resources initially and allow updates from DCP (for the relevant resources)
                foreach (var (_, resource) in _applicationModel)
                {
                    await ProcessInitialResourceAsync(resource, cancellationToken).ConfigureAwait(false);
                }

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
            try
            {
                await foreach (var (eventType, resource) in _kubernetesService.WatchAsync<T>(cancellationToken: cancellationToken))
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        if (IsFilteredResource(resource))
                        {
                            continue;
                        }

                        await handler(eventType, resource).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Watch task over kubernetes {ResourceType} resources terminated", typeof(T).Name);
            }
        }

        bool IsFilteredResource<T>(T resource) where T : CustomResource
        {
            // We filter out any resources that start with aspire-dashboard (there are services as well as executables).
            if (resource.Metadata.Name.StartsWith(KnownResourceNames.AspireDashboard, StringComparisons.ResourceName))
            {
                return configuration.GetBool("DOTNET_ASPIRE_SHOW_DASHBOARD_RESOURCES") is not true;
            }

            return false;
        }
    }

    private async Task ProcessInitialResourceAsync(IResource resource, CancellationToken cancellationToken)
    {
        // If the resource is a redirect, we want to create a snapshot for the resource it redirects to.
        if (resource.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var redirectAnnotation))
        {
            resource = redirectAnnotation.Resource;
        }

        if (resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var containerImageAnnotation))
        {
            var snapshot = new ContainerSnapshot
            {
                Name = resource.Name,
                DisplayName = resource.Name,
                Uid = resource.Name,
                CreationTimeStamp = DateTime.UtcNow,
                Image = containerImageAnnotation.Image,
                State = "Starting",
                ContainerId = null,
                ExitCode = null,
                ExpectedEndpointsCount = null,
                Environment = [],
                Endpoints = [],
                Services = [],
                Command = null,
                Args = [],
                Ports = []
            };

            _placeHolderResources.TryAdd(resource.Name, snapshot);

            await _onResourceChanged(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        }
        else if (resource is ProjectResource p)
        {
            var snapshot = new ProjectSnapshot
            {
                Name = p.Name,
                DisplayName = p.Name,
                Uid = p.Name,
                CreationTimeStamp = DateTime.UtcNow,
                ProjectPath = p.GetProjectMetadata().ProjectPath,
                State = "Starting",
                ExpectedEndpointsCount = null,
                Environment = [],
                Endpoints = [],
                Services = [],
                ExecutablePath = null,
                ExitCode = null,
                Arguments = null,
                ProcessId = null,
                StdErrFile = null,
                StdOutFile = null,
                WorkingDirectory = null
            };

            _placeHolderResources.TryAdd(resource.Name, snapshot);

            await _onResourceChanged(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        }
        else if (resource is ExecutableResource exe)
        {
            var snapshot = new ExecutableSnapshot
            {
                Name = exe.Name,
                DisplayName = exe.Name,
                Uid = exe.Name,
                CreationTimeStamp = DateTime.UtcNow,
                ExecutablePath = exe.Command,
                WorkingDirectory = exe.WorkingDirectory,
                Arguments = null,
                State = "Starting",
                ExitCode = null,
                StdOutFile = null,
                StdErrFile = null,
                ProcessId = null,
                ExpectedEndpointsCount = null,
                Environment = [],
                Endpoints = [],
                Services = []
            };

            _placeHolderResources.TryAdd(resource.Name, snapshot);

            await _onResourceChanged(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        }
        else if (resource.TryGetLastAnnotation<ResourceUpdatesAnnotation>(out var resourceUpdates))
        {
            // We have a dashboard annotation, so we want to create a snapshot for the resource
            // and update data immediately. We also want to watch for changes to the dashboard state.
            var state = resourceUpdates.GetInitialSnapshot();
            var creationTimestamp = DateTime.UtcNow;

            var snapshot = CreateResourceSnapshot(resource, creationTimestamp, state);

            await _onResourceChanged(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                await foreach (var state in resourceUpdates.WatchAsync().WithCancellation(cancellationToken))
                {
                    try
                    {
                        var snapshot = CreateResourceSnapshot(resource, creationTimestamp, state);

                        await _onResourceChanged(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error updating resource snapshot for {Name}", resource.Name);
                    }
                }

            }, cancellationToken);
        }
    }

    private static GenericResourceSnapshot CreateResourceSnapshot(IResource resource, DateTime creationTimestamp, CustomResourceSnapshot dashboardState)
    {
        ImmutableArray<EnvironmentVariableSnapshot> environmentVariables = [..
            dashboardState.EnviromentVariables.Select(e => new EnvironmentVariableSnapshot(e.Name, e.Value, false))];

        ImmutableArray<EndpointSnapshot> endpoints = [..
            dashboardState.Urls.Select(u => new EndpointSnapshot(u, u))];

        return new GenericResourceSnapshot(dashboardState)
        {
            Uid = resource.Name,
            CreationTimeStamp = creationTimestamp,
            Name = resource.Name,
            DisplayName = resource.Name,
            Endpoints = endpoints,
            Environment = environmentVariables,
            ExitCode = null,
            ExpectedEndpointsCount = endpoints.Length,
            Services = [],
            State = dashboardState.State ?? "Running"
        };
    }

    private async Task ProcessResourceChange<T>(WatchEventType watchEventType, T resource, ConcurrentDictionary<string, T> resourceByName, string resourceKind, Func<T, ResourceSnapshot> snapshotFactory) where T : CustomResource
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

            var snapshot = snapshotFactory(resource);

            await _onResourceChanged(snapshot, changeType).ConfigureAwait(false);

            // Remove the placeholder resource if it exists since we're getting an update about the real resource
            // from DCP.
            string? resourceName = null;
            resource.Metadata.Annotations?.TryGetValue(Executable.ResourceNameAnnotation, out resourceName);

            if (resourceName is not null && _placeHolderResources.TryRemove(resourceName, out var placeHolder))
            {
                await _onResourceChanged(placeHolder, ResourceSnapshotChangeType.Delete).ConfigureAwait(false);
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
        ResourceSnapshot? snapshot = resourceKind switch
        {
            "Container" => _containersMap.TryGetValue(resourceName, out var container) ? ToSnapshot(container) : null,
            "Executable" => _executablesMap.TryGetValue(resourceName, out var executable) ? ToSnapshot(executable) : null,
            _ => null
        };

        if (snapshot is not null)
        {
            await _onResourceChanged(snapshot, ResourceSnapshotChangeType.Upsert).ConfigureAwait(false);
        }
    }

    private ContainerSnapshot ToSnapshot(Container container)
    {
        var containerId = container.Status?.ContainerId;
        var (endpoints, services) = GetEndpointsAndServices(container, "Container");

        var environment = GetEnvironmentVariables(container.Status?.EffectiveEnv ?? container.Spec.Env, container.Spec.Env);

        return new ContainerSnapshot
        {
            Name = container.Metadata.Name,
            DisplayName = container.Metadata.Name,
            Uid = container.Metadata.Uid,
            ContainerId = containerId,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Image = container.Spec.Image!,
            State = container.Status?.State,
            // Map a container exit code of -1 (unknown) to null
            ExitCode = container.Status?.ExitCode is null or Conventions.UnknownExitCode ? null : container.Status.ExitCode,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(container),
            Environment = environment,
            Endpoints = endpoints,
            Services = services,
            Command = container.Spec.Command,
            Args = container.Status?.EffectiveArgs?.ToImmutableArray() ?? [],
            Ports = GetPorts()
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

    private ExecutableSnapshot ToSnapshot(Executable executable)
    {
        string? projectPath = null;
        executable.Metadata.Annotations?.TryGetValue(Executable.CSharpProjectPathAnnotation, out projectPath);

        var (endpoints, services) = GetEndpointsAndServices(executable, "Executable", projectPath);

        if (projectPath is not null)
        {
            // This executable represents a C# project, so we create a slightly different type here
            // that captures the project's path, making it more convenient for consumers to work with
            // the project.
            return new ProjectSnapshot
            {
                Name = executable.Metadata.Name,
                DisplayName = GetDisplayName(executable),
                Uid = executable.Metadata.Uid,
                CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
                ExecutablePath = executable.Spec.ExecutablePath,
                WorkingDirectory = executable.Spec.WorkingDirectory,
                Arguments = executable.Status?.EffectiveArgs?.ToImmutableArray() ?? [],
                ProjectPath = projectPath,
                State = executable.Status?.State,
                ExitCode = executable.Status?.ExitCode,
                StdOutFile = executable.Status?.StdOutFile,
                StdErrFile = executable.Status?.StdErrFile,
                ProcessId = executable.Status?.ProcessId,
                ExpectedEndpointsCount = GetExpectedEndpointsCount(executable),
                Environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env),
                Endpoints = endpoints,
                Services = services
            };
        }

        return new ExecutableSnapshot
        {
            Name = executable.Metadata.Name,
            DisplayName = GetDisplayName(executable),
            Uid = executable.Metadata.Uid,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            ExecutablePath = executable.Spec.ExecutablePath,
            WorkingDirectory = executable.Spec.WorkingDirectory,
            Arguments = executable.Status?.EffectiveArgs?.ToImmutableArray() ?? [],
            State = executable.Status?.State,
            ExitCode = executable.Status?.ExitCode,
            StdOutFile = executable.Status?.StdOutFile,
            StdErrFile = executable.Status?.StdErrFile,
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(executable),
            Environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env),
            Endpoints = endpoints,
            Services = services
        };

        static string GetDisplayName(Executable executable)
        {
            var displayName = executable.Metadata.Name;
            var replicaSetOwner = executable.Metadata.OwnerReferences?.FirstOrDefault(
                or => or.Kind == Dcp.Model.Dcp.ExecutableReplicaSetKind
            );
            if (replicaSetOwner is not null && displayName.Length > 3)
            {
                var lastHyphenIndex = displayName.LastIndexOf('-');
                if (lastHyphenIndex > 0 && lastHyphenIndex < displayName.Length - 1)
                {
                    // Strip the replica ID from the name.
                    displayName = displayName[..lastHyphenIndex];
                }
            }
            return displayName;
        }
    }

    private (ImmutableArray<EndpointSnapshot> Endpoints, ImmutableArray<ResourceServiceSnapshot> Services) GetEndpointsAndServices(
        CustomResource resource,
        string resourceKind,
        string? projectPath = null)
    {
        var endpoints = ImmutableArray.CreateBuilder<EndpointSnapshot>();
        var services = ImmutableArray.CreateBuilder<ResourceServiceSnapshot>();
        var name = resource.Metadata.Name;
        string? resourceName = null;
        resource.Metadata.Annotations?.TryGetValue(Executable.ResourceNameAnnotation, out resourceName);

        foreach (var endpoint in _endpointsMap.Values)
        {
            if (endpoint.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == name) != true)
            {
                continue;
            }

            if (endpoint.Spec.ServiceName is not null
                && _servicesMap.TryGetValue(endpoint.Spec.ServiceName, out var service)
                && service?.UsesHttpProtocol(out var uriScheme) == true)
            {
                var endpointString = $"{uriScheme}://{endpoint.Spec.Address}:{endpoint.Spec.Port}";
                var proxyUrlString = $"{uriScheme}://{service.AllocatedAddress}:{service.AllocatedPort}";

                // For project look into launch profile to append launch url
                if (projectPath is not null
                    && resourceName is not null
                    && _applicationModel.TryGetValue(resourceName, out var appModelResource)
                    && appModelResource is ProjectResource project
                    && project.GetEffectiveLaunchProfile() is LaunchProfile launchProfile
                    && launchProfile.LaunchUrl is string launchUrl)
                {
                    if (!launchUrl.Contains("://"))
                    {
                        // This is relative URL
                        endpointString += $"/{launchUrl}";
                        proxyUrlString += $"/{launchUrl}";
                    }
                    else
                    {
                        // For absolute URL we need to update the port value if possible
                        if (launchProfile.ApplicationUrl is string applicationUrl
                            && launchUrl.StartsWith(applicationUrl))
                        {
                            endpointString = launchUrl.Replace(applicationUrl, endpointString);
                            proxyUrlString = launchUrl;
                        }
                    }

                    // If we cannot process launchUrl then we just show endpoint string
                }

                endpoints.Add(new(endpointString, proxyUrlString));
            }
        }

        if (_resourceAssociatedServicesMap.TryGetValue((resourceKind, name), out var resourceServiceMappings))
        {
            foreach (var serviceName in resourceServiceMappings)
            {
                if (_servicesMap.TryGetValue(serviceName, out var service))
                {
                    services.Add(new ResourceServiceSnapshot(service.Metadata.Name, service.AllocatedAddress, service.AllocatedPort));
                }
            }
        }

        return (endpoints.ToImmutable(), services.ToImmutable());
    }

    private int? GetExpectedEndpointsCount(CustomResource resource)
    {
        var expectedCount = 0;
        if (resource.Metadata.Annotations?.TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson) == true)
        {
            var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(servicesProducedAnnotationJson);
            if (serviceProducerAnnotations is not null)
            {
                foreach (var serviceProducer in serviceProducerAnnotations)
                {
                    if (!_servicesMap.TryGetValue(serviceProducer.ServiceName, out var service))
                    {
                        // We don't have matching service so we cannot compute endpoint count completely
                        // So we return null indicating that it is unknown.
                        // Dashboard should show this as Starting
                        return null;
                    }

                    if (service.UsesHttpProtocol(out _))
                    {
                        expectedCount++;
                    }
                }
            }
        }

        return expectedCount;
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

                environment.Add(new(env.Name, env.Value, isFromSpec));
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
}
