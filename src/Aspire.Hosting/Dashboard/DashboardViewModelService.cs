// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s;

namespace Aspire.Hosting.Dashboard;

public class DashboardViewModelService : IDashboardViewModelService, IDisposable
{
    private readonly DistributedApplicationModel _applicationModel;
    private readonly KubernetesService _kubernetesService;

    public DashboardViewModelService(DistributedApplicationModel applicationModel)
    {
        _applicationModel = applicationModel;
        _kubernetesService = new KubernetesService();
    }

    public async Task<List<ExecutableViewModel>> GetExecutablesAsync()
    {
        var executables = await _kubernetesService.ListAsync<Executable>().ConfigureAwait(false);
        return executables
            .Where(executable => executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == false)
            .Select(executable =>
            {
                var model = new ExecutableViewModel()
                {
                    Name = executable.Metadata.Name,
                    NamespacedName = new(executable.Metadata.Name, null),
                    CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
                    ExecutablePath = executable.Spec.ExecutablePath,
                    State = executable.Status?.State,
                    LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile)
                };

                if (executable.Status?.EffectiveEnv is not null)
                {
                    FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv);
                }

                return model;
            })
            .OrderBy(e => e.Name)
            .ToList();
    }

    public async Task<List<ProjectViewModel>> GetProjectsAsync()
    {
        var executables = await _kubernetesService.ListAsync<Executable>().ConfigureAwait(false);

        var endpoints = await _kubernetesService.ListAsync<Endpoint>().ConfigureAwait(false);

        return executables
            .Where(executable => executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == true)
            .Select(executable =>
            {
                var expectedEndpointCount = 0;
                if (executable.Metadata?.Annotations?.TryGetValue(Executable.ServiceProducerAnnotation, out var annotationJson) == true)
                {
                    var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(annotationJson);
                    if (serviceProducerAnnotations is not null)
                    {
                        expectedEndpointCount = serviceProducerAnnotations.Length;
                    }
                }

                var model = new ProjectViewModel
                {
                    Name = executable.Metadata!.Name,
                    NamespacedName = new(executable.Metadata.Name, null),
                    CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
                    ProjectPath = executable.Metadata?.Annotations?[Executable.CSharpProjectPathAnnotation] ?? "",
                    State = executable.Status?.State,
                    LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
                    ExpectedEndpointCount = expectedEndpointCount
                };

                if (_applicationModel.TryGetProjectWithPath(model.ProjectPath, out var project) && project.TryGetAllocatedEndPoints(out var allocatedEndpoints))
                {
                    model.Addresses.AddRange(allocatedEndpoints.Select(ep => ep.UriString));
                }

                model.Endpoints.AddRange(endpoints
                    .Where(ep => ep.Metadata.OwnerReferences.Any(or => or.Kind == executable.Kind && or.Name == executable.Metadata?.Name))
                    .Select(ep =>
                    {
                        // CONSIDER: a more robust way to store application protocol information in DCP model
                        string scheme = "http://";
                        if (ep.Spec.ServiceName?.EndsWith("https") is true)
                        {
                            scheme = "https://";
                        }

                        return new ServiceEndpoint($"{scheme}{ep.Spec.Address}:{ep.Spec.Port}", ep.Spec.ServiceName ?? "");
                    })
                );

                if (executable.Status?.EffectiveEnv is not null)
                {
                    FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv);
                }

                return model;
            })
            .OrderBy(m => m.Name)
            .ToList();
    }

    public async Task<List<ContainerViewModel>> GetContainersAsync()
    {
        var containers = await _kubernetesService.ListAsync<Container>().ConfigureAwait(false);

        return containers
            .Select(container =>
            {
                var model = new ContainerViewModel
                {
                    Name = container.Metadata.Name,
                    NamespacedName = new(container.Metadata.Name, null),
                    ContainerID = container.Status?.ContainerID,
                    CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
                    Image = container.Spec.Image!,
                    LogSource = new DockerContainerLogSource(container.Status!.ContainerID!),
                    State = container.Status?.State
                };

                if (container.Spec.Ports != null)
                {
                    foreach (var port in container.Spec.Ports)
                    {
                        if (port.ContainerPort != null)
                        {
                            model.Ports.Add(port.ContainerPort.Value);
                        }
                    }
                }

                if (container.Spec.Env is not null)
                {
                    FillEnvironmentVariables(model.Environment, container.Spec.Env);
                }

                return model;
            })
            .OrderBy(e => e.Name)
            .ToList();
    }

    public async IAsyncEnumerable<ComponentChanged<ContainerViewModel>> WatchContainersAsync(
        IEnumerable<NamespacedName>? existingContainers = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (watchEventType, container) in _kubernetesService.WatchAsync<Container>(existingObjects: existingContainers, cancellationToken: cancellationToken))
        {
            var objectChangeType = ToObjectChangeType(watchEventType);
            if (objectChangeType == ObjectChangeType.Other)
            {
                continue;
            }

            var containerViewModel = new ContainerViewModel
            {
                Name = container.Metadata.Name,
                NamespacedName = new(container.Metadata.Name, null),
                ContainerID = container.Status?.ContainerID,
                CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
                Image = container.Spec.Image!,
                LogSource = new DockerContainerLogSource(container.Status!.ContainerID!),
                State = container.Status?.State
            };

            if (container.Spec.Ports != null)
            {
                foreach (var port in container.Spec.Ports)
                {
                    if (port.ContainerPort != null)
                    {
                        containerViewModel.Ports.Add(port.ContainerPort.Value);
                    }
                }
            }

            if (container.Spec.Env is not null)
            {
                FillEnvironmentVariables(containerViewModel.Environment, container.Spec.Env);
            }

            yield return new ComponentChanged<ContainerViewModel>(objectChangeType, containerViewModel);
        }
    }

    public async IAsyncEnumerable<ComponentChanged<ExecutableViewModel>> WatchExecutablesAsync(
        IEnumerable<NamespacedName>? existingExecutables = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (watchEventType, executable) in _kubernetesService.WatchAsync<Executable>(existingObjects: existingExecutables, cancellationToken: cancellationToken))
        {
            var objectChangeType = ToObjectChangeType(watchEventType);
            if (objectChangeType == ObjectChangeType.Other)
            {
                continue;
            }

            if (executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == true)
            {
                continue;
            }

            var executableViewModel = new ExecutableViewModel
            {
                Name = executable.Metadata.Name,
                NamespacedName = new(executable.Metadata.Name, null),
                CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
                ExecutablePath = executable.Spec.ExecutablePath,
                State = executable.Status?.State,
                LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile)
            };

            if (executable.Status?.EffectiveEnv is not null)
            {
                FillEnvironmentVariables(executableViewModel.Environment, executable.Status.EffectiveEnv);
            }

            yield return new ComponentChanged<ExecutableViewModel>(objectChangeType, executableViewModel);
        }
    }

    public async IAsyncEnumerable<ComponentChanged<ProjectViewModel>> WatchProjectsAsync(
        IEnumerable<NamespacedName>? existingProjects = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (watchEventType, executable) in _kubernetesService.WatchAsync<Executable>(existingObjects: existingProjects, cancellationToken: cancellationToken))
        {
            var objectChangeType = ToObjectChangeType(watchEventType);
            if (objectChangeType == ObjectChangeType.Other)
            {
                continue;
            }

            if (executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) != true)
            {
                continue;
            }

            var expectedEndpointCount = 0;
            if (executable.Metadata?.Annotations?.TryGetValue(Executable.ServiceProducerAnnotation, out var annotationJson) == true)
            {
                var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(annotationJson);
                if (serviceProducerAnnotations is not null)
                {
                    expectedEndpointCount = serviceProducerAnnotations.Length;
                }
            }

            var projectViewModel = new ProjectViewModel
            {
                Name = executable.Metadata!.Name,
                NamespacedName = new(executable.Metadata.Name, null),
                CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
                ProjectPath = executable.Metadata?.Annotations?[Executable.CSharpProjectPathAnnotation] ?? "",
                State = executable.Status?.State,
                LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
                ExpectedEndpointCount = expectedEndpointCount
            };

            if (_applicationModel.TryGetProjectWithPath(projectViewModel.ProjectPath, out var project) && project.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                projectViewModel.Addresses.AddRange(allocatedEndpoints.Select(ep => ep.UriString));
            }

            projectViewModel.Endpoints.AddRange((await _kubernetesService.ListAsync<Endpoint>(cancellationToken: cancellationToken).ConfigureAwait(true))
                .Where(ep => ep.Metadata.OwnerReferences.Any(or => or.Kind == executable.Kind && or.Name == executable.Metadata?.Name))
                .Select(ep =>
                {
                    // CONSIDER: a more robust way to store application protocol information in DCP model
                    string scheme = "http://";
                    if (ep.Spec.ServiceName?.EndsWith("https") is true)
                    {
                        scheme = "https://";
                    }

                    return new ServiceEndpoint($"{scheme}{ep.Spec.Address}:{ep.Spec.Port}", ep.Spec.ServiceName ?? "");
                }
            )
            );

            if (executable.Status?.EffectiveEnv is not null)
            {
                FillEnvironmentVariables(projectViewModel.Environment, executable.Status.EffectiveEnv);
            }

            yield return new ComponentChanged<ProjectViewModel>(objectChangeType, projectViewModel);
        }
    }

    public void Dispose()
    {
        _kubernetesService.Dispose();
    }

    private static ObjectChangeType ToObjectChangeType(WatchEventType watchEventType)
        => watchEventType switch
        {
            WatchEventType.Added => ObjectChangeType.Added,
            WatchEventType.Modified => ObjectChangeType.Modified,
            WatchEventType.Deleted => ObjectChangeType.Deleted,
            _ => ObjectChangeType.Other
        };

    private static void FillEnvironmentVariables(List<EnvironmentVariableViewModel> target, List<EnvVar> source)
    {
        foreach (var env in source)
        {
            if (env.Name is not null)
            {
                target.Add(new()
                {
                    Name = env.Name,
                    Value = env.Value
                });
            }
        }

        target.Sort((v1, v2) => string.Compare(v1.Name, v2.Name));
    }
}
