// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Pulls data about resources from DCP's kubernetes API. Streams updates to consumers.
/// </summary>
internal sealed class DcpDataSource
{
    private readonly KubernetesService _kubernetesService;
    private readonly DistributedApplicationModel _applicationModel;
    private readonly Func<ResourceViewModel, ObjectChangeType, ValueTask> _onResourceChanged;
    private readonly ILogger _logger;

    private readonly Dictionary<string, Container> _containersMap = [];
    private readonly Dictionary<string, Executable> _executablesMap = [];
    private readonly Dictionary<string, Service> _servicesMap = [];
    private readonly Dictionary<string, Endpoint> _endpointsMap = [];
    private readonly Dictionary<(ResourceKind, string), List<string>> _resourceAssociatedServicesMap = [];

    public DcpDataSource(
        KubernetesService kubernetesService,
        DistributedApplicationModel applicationModel,
        ILoggerFactory loggerFactory,
        Func<ResourceViewModel, ObjectChangeType, ValueTask> onResourceChanged,
        CancellationToken cancellationToken)
    {
        _kubernetesService = kubernetesService;
        _applicationModel = applicationModel;
        _onResourceChanged = onResourceChanged;

        _logger = loggerFactory.CreateLogger<ResourceService>();

        var semaphore = new SemaphoreSlim(1);

        Task.Run(
            async () =>
            {
                using (semaphore)
                {
                    await Task.WhenAll(
                        Task.Run(() => WatchKubernetesResource<Executable>(ProcessExecutableChange), cancellationToken),
                        Task.Run(() => WatchKubernetesResource<Service>(ProcessServiceChange), cancellationToken),
                        Task.Run(() => WatchKubernetesResource<Endpoint>(ProcessEndpointChange), cancellationToken),
                        Task.Run(() => WatchKubernetesResource<Container>(ProcessContainerChange), cancellationToken)).ConfigureAwait(false);
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
                _logger.LogError(ex, "Watch task over kubernetes resource of type: {resourceType} terminated", typeof(T).Name);
            }
        }
    }

    private async Task ProcessContainerChange(WatchEventType watchEventType, Container container)
    {
        if (!ProcessResourceChange(_containersMap, watchEventType, container))
        {
            return;
        }

        UpdateAssociatedServicesMap(ResourceKind.Container, watchEventType, container);

        var objectChangeType = ToObjectChangeType(watchEventType);
        var containerViewModel = ConvertToContainerViewModel(container);

        await _onResourceChanged(containerViewModel, objectChangeType).ConfigureAwait(false);
    }

    private async Task ProcessExecutableChange(WatchEventType watchEventType, Executable executable)
    {
        if (executable.IsCSharpProject())
        {
            await ProcessProjectChange(watchEventType, executable).ConfigureAwait(false);
            return;
        }

        if (!ProcessResourceChange(_executablesMap, watchEventType, executable))
        {
            return;
        }

        UpdateAssociatedServicesMap(ResourceKind.Executable, watchEventType, executable);

        var objectChangeType = ToObjectChangeType(watchEventType);
        var executableViewModel = ConvertToExecutableViewModel(executable);

        await _onResourceChanged(executableViewModel, objectChangeType).ConfigureAwait(false);
    }

    private async Task ProcessProjectChange(WatchEventType watchEventType, Executable executable)
    {
        if (!ProcessResourceChange(_executablesMap, watchEventType, executable))
        {
            return;
        }

        UpdateAssociatedServicesMap(ResourceKind.Executable, watchEventType, executable);

        var objectChangeType = ToObjectChangeType(watchEventType);
        var projectViewModel = ConvertToProjectViewModel(executable);

        await _onResourceChanged(projectViewModel, objectChangeType).ConfigureAwait(false);
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
            // Find better way for this string switch
            switch (ownerReference.Kind)
            {
                case "Container":
                    if (_containersMap.TryGetValue(ownerReference.Name, out var container))
                    {
                        var containerViewModel = ConvertToContainerViewModel(container);

                        await _onResourceChanged(containerViewModel, ObjectChangeType.Upsert).ConfigureAwait(false);
                    }
                    break;

                case "Executable":
                    if (_executablesMap.TryGetValue(ownerReference.Name, out var executable))
                    {
                        if (executable.IsCSharpProject())
                        {
                            // Project
                            var projectViewModel = ConvertToProjectViewModel(executable);

                            await _onResourceChanged(projectViewModel, ObjectChangeType.Upsert).ConfigureAwait(false);
                        }
                        else
                        {
                            // Executable
                            var executableViewModel = ConvertToExecutableViewModel(executable);

                            await _onResourceChanged(executableViewModel, ObjectChangeType.Upsert).ConfigureAwait(false);
                        }
                    }
                    break;
            }
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
            switch (resourceKind)
            {
                case ResourceKind.Container:
                    if (_containersMap.TryGetValue(resourceName, out var container))
                    {
                        var containerViewModel = ConvertToContainerViewModel(container);

                        await _onResourceChanged(containerViewModel, ObjectChangeType.Upsert).ConfigureAwait(false);
                    }
                    break;

                case ResourceKind.Executable:
                    if (_executablesMap.TryGetValue(resourceName, out var executable))
                    {
                        if (executable.IsCSharpProject())
                        {
                            // Project
                            var projectViewModel = ConvertToProjectViewModel(executable);

                            await _onResourceChanged(projectViewModel, ObjectChangeType.Upsert).ConfigureAwait(false);
                        }
                        else
                        {
                            // Executable
                            var executableViewModel = ConvertToExecutableViewModel(executable);

                            await _onResourceChanged(executableViewModel, ObjectChangeType.Upsert).ConfigureAwait(false);
                        }
                    }
                    break;
            }
        }
    }

    private ContainerViewModel ConvertToContainerViewModel(Container container)
    {
        var containerId = container.Status?.ContainerId;
        var (endpoints, services) = GetEndpointsAndServices(container, ResourceKind.Container);

        var environment = GetEnvironmentVariables(container.Status?.EffectiveEnv ?? container.Spec.Env, container.Spec.Env);

        var model = new ContainerViewModel
        {
            Name = container.Metadata.Name,
            DisplayName = container.Metadata.Name,
            Uid = container.Metadata.Uid,
            ContainerId = containerId,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Image = container.Spec.Image!,
            LogSource = new DockerContainerLogSource(containerId!),
            State = container.Status?.State,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(container),
            Environment = environment,
            Endpoints = endpoints,
            Services = services,
            Command = container.Spec.Command,
            Args = container.Spec.Args?.ToImmutableArray() ?? [],
            Ports = GetPorts()
        };

        return model;

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

    private ExecutableViewModel ConvertToExecutableViewModel(Executable executable)
    {
        var (endpoints, services) = GetEndpointsAndServices(executable, ResourceKind.Executable);

        var model = new ExecutableViewModel
        {
            Name = executable.Metadata.Name,
            DisplayName = ComputeExecutableDisplayName(executable),
            Uid = executable.Metadata.Uid,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            ExecutablePath = executable.Spec.ExecutablePath,
            WorkingDirectory = executable.Spec.WorkingDirectory,
            Arguments = executable.Spec.Args?.ToImmutableArray(),
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(executable),
            Environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env),
            Endpoints = endpoints,
            Services = services
        };

        return model;
    }

    private ProjectViewModel ConvertToProjectViewModel(Executable executable)
    {
        var projectPath = executable.Metadata.Annotations?[Executable.CSharpProjectPathAnnotation] ?? "";
        var (endpoints, services) = GetEndpointsAndServices(executable, ResourceKind.Executable, projectPath);

        var model = new ProjectViewModel
        {
            Name = executable.Metadata.Name,
            DisplayName = ComputeExecutableDisplayName(executable),
            Uid = executable.Metadata.Uid,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            ProjectPath = projectPath,
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(executable),
            Environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env),
            Endpoints = endpoints,
            Services = services
        };

        return model;
    }

    private (ImmutableArray<string> Endpoints, ImmutableArray<ResourceServiceSnapshot> Services) GetEndpointsAndServices(
        CustomResource resource,
        ResourceKind resourceKind,
        string? projectPath = null)
    {
        var endpoints = ImmutableArray.CreateBuilder<string>();
        var services = ImmutableArray.CreateBuilder<ResourceServiceSnapshot>();
        var name = resource.Metadata.Name;

        foreach (var endpoint in _endpointsMap.Values)
        {
            if (endpoint.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == name) != true)
            {
                continue;
            }

            var matchingService = _servicesMap.Values.SingleOrDefault(s => s.Metadata.Name == endpoint.Spec.ServiceName);
            if (matchingService?.UsesHttpProtocol(out var uriScheme) == true)
            {
                var endpointString = $"{uriScheme}://{endpoint.Spec.Address}:{endpoint.Spec.Port}";

                // For project look into launch profile to append launch url
                if (projectPath is not null
                    && _applicationModel.TryGetProjectWithPath(name, projectPath, out var project)
                    && project.GetEffectiveLaunchProfile() is LaunchProfile launchProfile
                    && launchProfile.LaunchUrl is string launchUrl)
                {
                    if (!launchUrl.Contains("://"))
                    {
                        // This is relative URL
                        endpointString += $"/{launchUrl}";
                    }
                    else
                    {
                        // For absolute URL we need to update the port value if possible
                        if (launchProfile.ApplicationUrl is string applicationUrl
                            && launchUrl.StartsWith(applicationUrl))
                        {
                            endpointString = launchUrl.Replace(applicationUrl, endpointString);
                        }
                    }

                    // If we cannot process launchUrl then we just show endpoint string
                }

                endpoints.Add(endpointString);
            }
        }

        if (_resourceAssociatedServicesMap.TryGetValue((resourceKind, name), out var resourceServiceMappings))
        {
            foreach (var serviceName in resourceServiceMappings)
            {
                if (_servicesMap.TryGetValue(name, out var service))
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

    private static ImmutableArray<EnvironmentVariableViewModel> GetEnvironmentVariables(List<EnvVar>? effectiveSource, List<EnvVar>? specSource)
    {
        if (effectiveSource is null or { Count: 0 })
        {
            return [];
        }

        var environment = ImmutableArray.CreateBuilder<EnvironmentVariableViewModel>(effectiveSource.Count);

        foreach (var env in effectiveSource)
        {
            if (env.Name is not null)
            {
                environment.Add(new()
                {
                    Name = env.Name,
                    Value = env.Value,
                    FromSpec = specSource?.Any(e => string.Equals(e.Name, env.Name, StringComparison.Ordinal)) is true or null
                });
            }
        }

        environment.Sort((v1, v2) => string.Compare(v1.Name, v2.Name, StringComparison.Ordinal));

        return environment.ToImmutable();
    }

    private void UpdateAssociatedServicesMap(ResourceKind resourceKind, WatchEventType watchEventType, CustomResource resource)
    {
        // We keep track of associated services for the resource
        // So whenever we get the service we can figure out if the service can generate endpoint for the resource
        if (watchEventType == WatchEventType.Deleted)
        {
            _resourceAssociatedServicesMap.Remove((resourceKind, resource.Metadata.Name));
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

    private static bool ProcessResourceChange<T>(Dictionary<string, T> map, WatchEventType watchEventType, T resource)
            where T : CustomResource
    {
        switch (watchEventType)
        {
            case WatchEventType.Added:
                map.Add(resource.Metadata.Name, resource);
                break;

            case WatchEventType.Modified:
                map[resource.Metadata.Name] = resource;
                break;

            case WatchEventType.Deleted:
                map.Remove(resource.Metadata.Name);
                break;

            default:
                return false;
        }

        return true;
    }

    private static ObjectChangeType ToObjectChangeType(WatchEventType watchEventType)
    {
        return watchEventType switch
        {
            WatchEventType.Added or WatchEventType.Modified => ObjectChangeType.Upsert,
            WatchEventType.Deleted => ObjectChangeType.Deleted,
            _ => ObjectChangeType.Other
        };
    }

    private static string ComputeExecutableDisplayName(Executable executable)
    {
        var displayName = executable.Metadata.Name;
        var replicaSetOwner = executable.Metadata.OwnerReferences?.FirstOrDefault(
            or => or.Kind == Dcp.Model.Dcp.ExecutableReplicaSetKind
        );
        if (replicaSetOwner is not null && displayName.Length > 3)
        {
            var nameParts = displayName.Split('-');
            if (nameParts.Length == 2 && nameParts[0].Length > 0 && nameParts[1].Length > 0)
            {
                // Strip the replica ID from the name.
                displayName = nameParts[0];
            }
        }
        return displayName;
    }

    private enum ResourceKind
    {
        Container,
        Executable
    }
}
