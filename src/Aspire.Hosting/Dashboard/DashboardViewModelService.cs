// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s;
using Microsoft.Extensions.Hosting;
using NamespacedName = Aspire.Dashboard.Model.NamespacedName;

namespace Aspire.Hosting.Dashboard;

public class DashboardViewModelService : IDashboardViewModelService, IDisposable
{
    private const string AppHostSuffix = ".AppHost";

    private readonly DistributedApplicationModel _applicationModel;
    private readonly string _applicationName;
    private readonly KubernetesService _kubernetesService = new();

    public DashboardViewModelService(DistributedApplicationModel applicationModel, IHostEnvironment hostEnvironment)
    {
        _applicationModel = applicationModel;
        _applicationName = ComputeApplicationName(hostEnvironment.ApplicationName);
    }

    public string ApplicationName => _applicationName;

    public async Task<List<ContainerViewModel>> GetContainersAsync()
    {
        var containers = await _kubernetesService.ListAsync<Container>().ConfigureAwait(false);

        return containers.Select(ConvertToContainerViewModel).OrderBy(e => e.Name).ToList();
    }

    public async Task<List<ExecutableViewModel>> GetExecutablesAsync()
    {
        var executables = await _kubernetesService.ListAsync<Executable>().ConfigureAwait(false);
        return executables
            .Where(executable => executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == false)
            .Select(ConvertToExecutableViewModel).OrderBy(e => e.Name).ToList();
    }

    public async Task<List<ProjectViewModel>> GetProjectsAsync()
    {
        var executables = await _kubernetesService.ListAsync<Executable>().ConfigureAwait(false);

        var endpoints = await _kubernetesService.ListAsync<Endpoint>().ConfigureAwait(false);

        return executables
            .Where(executable => executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == true)
            .Select(executable => ConvertToProjectViewModel(executable, endpoints)).OrderBy(m => m.Name).ToList();
    }

    public async IAsyncEnumerable<ResourceChanged<ContainerViewModel>> WatchContainersAsync(
        IEnumerable<NamespacedName>? existingContainers = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var existingObjects = existingContainers?.Select(ec => new Dcp.Model.NamespacedName(ec.Name, ec.Namespace));
        await foreach (var (watchEventType, container) in _kubernetesService.WatchAsync<Container>(existingObjects: existingObjects, cancellationToken: cancellationToken))
        {
            var objectChangeType = ToObjectChangeType(watchEventType);
            if (objectChangeType == ObjectChangeType.Other)
            {
                continue;
            }

            var containerViewModel = ConvertToContainerViewModel(container);

            yield return new ResourceChanged<ContainerViewModel>(objectChangeType, containerViewModel);
        }
    }

    public async IAsyncEnumerable<ResourceChanged<ExecutableViewModel>> WatchExecutablesAsync(
        IEnumerable<NamespacedName>? existingExecutables = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var existingObjects = existingExecutables?.Select(ec => new Dcp.Model.NamespacedName(ec.Name, ec.Namespace));
        await foreach (var (watchEventType, executable) in _kubernetesService.WatchAsync<Executable>(existingObjects: existingObjects, cancellationToken: cancellationToken))
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

            var executableViewModel = ConvertToExecutableViewModel(executable);

            yield return new ResourceChanged<ExecutableViewModel>(objectChangeType, executableViewModel);
        }
    }

    public async IAsyncEnumerable<ResourceChanged<ProjectViewModel>> WatchProjectsAsync(
        IEnumerable<NamespacedName>? existingProjects = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var existingObjects = existingProjects?.Select(ec => new Dcp.Model.NamespacedName(ec.Name, ec.Namespace));
        await foreach (var (watchEventType, executable) in _kubernetesService.WatchAsync<Executable>(existingObjects: existingObjects, cancellationToken: cancellationToken))
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

            var endpoints = await _kubernetesService.ListAsync<Endpoint>(cancellationToken: cancellationToken).ConfigureAwait(false);
            var projectViewModel = ConvertToProjectViewModel(executable, endpoints);

            yield return new ResourceChanged<ProjectViewModel>(objectChangeType, projectViewModel);
        }
    }

    private static ContainerViewModel ConvertToContainerViewModel(Container container)
    {
        var model = new ContainerViewModel
        {
            Name = container.Metadata.Name,
            NamespacedName = new(container.Metadata.Name, null),
            ContainerId = container.Status?.ContainerId,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Image = container.Spec.Image!,
            LogSource = new DockerContainerLogSource(container.Status!.ContainerId!),
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
            FillEnvironmentVariables(model.Environment, container.Spec.Env, container.Spec.Env);
        }

        return model;
    }

    private static ExecutableViewModel ConvertToExecutableViewModel(Executable executable)
    {
        var model = new ExecutableViewModel()
        {
            Name = executable.Metadata.Name,
            NamespacedName = new(executable.Metadata.Name, null),
            CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
            ExecutablePath = executable.Spec.ExecutablePath,
            WorkingDirectory = executable.Spec.WorkingDirectory,
            Arguments = executable.Spec.Args,
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile)
        };

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }

        return model;
    }

    private ProjectViewModel ConvertToProjectViewModel(Executable executable, List<Endpoint> endpoints)
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
            ExpectedEndpointsCount = expectedEndpointCount
        };

        model.Endpoints.AddRange(endpoints
            .Where(ep => ep.Metadata.OwnerReferences.Any(or => or.Kind == executable.Kind && or.Name == executable.Metadata?.Name))
            .Select(ep =>
            {
                var builder = new StringBuilder();
                // CONSIDER: a more robust way to store application protocol information in DCP model
                if (ep.Spec.ServiceName?.EndsWith("https") is true)
                {
                    builder.Append("https://");
                }
                else
                {
                    builder.Append("http://");
                }

                builder.Append(ep.Spec.Address);
                builder.Append(':');
                builder.Append(ep.Spec.Port);

                if (_applicationModel.TryGetProjectWithPath(model.ProjectPath, out var project)
                    && project.GetEffectiveLaunchProfile() is LaunchProfile launchProfile
                    && launchProfile.LaunchUrl is string launchUrl)
                {
                    builder.Append('/');
                    builder.Append(launchUrl);
                }

                return builder.ToString();
            })
        );

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }

        return model;
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

    private static void FillEnvironmentVariables(List<EnvironmentVariableViewModel> target, List<EnvVar> effectiveSource, List<EnvVar>? specSource)
    {
        foreach (var env in effectiveSource)
        {
            if (env.Name is not null)
            {
                target.Add(new()
                {
                    Name = env.Name,
                    Value = env.Value,
                    FromSpec = specSource?.Any(e => string.Equals(e.Name, env.Name, StringComparison.Ordinal)) == true
                });
            }
        }

        target.Sort((v1, v2) => string.Compare(v1.Name, v2.Name));
    }

    private static string ComputeApplicationName(string applicationName)
    {
        if (applicationName.EndsWith(AppHostSuffix, StringComparison.OrdinalIgnoreCase))
        {
            applicationName = applicationName[..^AppHostSuffix.Length];
        }

        return applicationName;
    }
}
