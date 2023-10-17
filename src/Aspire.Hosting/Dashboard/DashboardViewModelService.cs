// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Hosting;
using NamespacedName = Aspire.Dashboard.Model.NamespacedName;

namespace Aspire.Hosting.Dashboard;

public partial class DashboardViewModelService : IDashboardViewModelService, IDisposable
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
        var endpoints = await _kubernetesService.ListAsync<Endpoint>().ConfigureAwait(false);
        var services = await _kubernetesService.ListAsync<Service>().ConfigureAwait(false);

        var results = containers.Select(e => ConvertToContainerViewModel(_applicationModel, services, endpoints, e))
            .OrderBy(e => e.Name).ToList();

        await Task.WhenAll(results.Select(FillEnvironmentVariablesFromDocker)).ConfigureAwait(false);

        return results;
    }

    public async Task<List<ExecutableViewModel>> GetExecutablesAsync()
    {
        var executables = await _kubernetesService.ListAsync<Executable>().ConfigureAwait(false);
        var endpoints = await _kubernetesService.ListAsync<Endpoint>().ConfigureAwait(false);
        var services = await _kubernetesService.ListAsync<Service>().ConfigureAwait(false);

        return executables.Where(executable => executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == false)
            .Select(e => ConvertToExecutableViewModel(_applicationModel, services, endpoints, e))
            .ToList();
    }

    public async Task<List<ProjectViewModel>> GetProjectsAsync()
    {
        var executables = await _kubernetesService.ListAsync<Executable>().ConfigureAwait(false);
        var endpoints = await _kubernetesService.ListAsync<Endpoint>().ConfigureAwait(false);
        var services = await _kubernetesService.ListAsync<Service>().ConfigureAwait(false);

        return executables.Where(executable => executable.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == true)
            .Select(e => ConvertToProjectViewModel(_applicationModel, services, endpoints, e))
            .ToList();
    }

    public async IAsyncEnumerable<ResourceChanged<ContainerViewModel>> WatchContainersAsync(
        IEnumerable<NamespacedName>? existingContainers = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var watcher = new ViewModelWatcher<Container, ContainerViewModel>(
            _kubernetesService,
            _applicationModel,
            existingContainers,
            c => true,
            ConvertToContainerViewModel);

        await foreach (var tuple in watcher.WithCancellation(cancellationToken))
        {
            yield return tuple;
        }
    }

    public async IAsyncEnumerable<ResourceChanged<ExecutableViewModel>> WatchExecutablesAsync(
        IEnumerable<NamespacedName>? existingExecutables = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var watcher = new ViewModelWatcher<Executable, ExecutableViewModel>(
            _kubernetesService,
            _applicationModel,
            existingExecutables,
            e => e.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == false,
            ConvertToExecutableViewModel);

        await foreach (var tuple in watcher.WithCancellation(cancellationToken))
        {
            yield return tuple;
        }
    }

    public async IAsyncEnumerable<ResourceChanged<ProjectViewModel>> WatchProjectsAsync(
        IEnumerable<NamespacedName>? existingProjects = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var watcher = new ViewModelWatcher<Executable, ProjectViewModel>(
            _kubernetesService,
            _applicationModel,
            existingProjects,
            e => e.Metadata.Annotations?.ContainsKey(Executable.CSharpProjectPathAnnotation) == true,
            ConvertToProjectViewModel);

        await foreach (var tuple in watcher.WithCancellation(cancellationToken))
        {
            yield return tuple;
        }
    }

    private static ContainerViewModel ConvertToContainerViewModel(
        DistributedApplicationModel applicationModel, IEnumerable<Service> services, IEnumerable<Endpoint> endpoints, Container container)
    {
        var model = new ContainerViewModel
        {
            Name = container.Metadata.Name,
            NamespacedName = new(container.Metadata.Name, null),
            ContainerId = container.Status?.ContainerId,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Image = container.Spec.Image!,
            LogSource = new DockerContainerLogSource(container.Status!.ContainerId!),
            State = container.Status?.State,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(services, container)
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

        FillEndpoints(applicationModel, services, endpoints, container, model);

        if (container.Spec.Env is not null)
        {
            FillEnvironmentVariables(model.Environment, container.Spec.Env, container.Spec.Env);
        }

        return model;
    }

    private static async Task FillEnvironmentVariablesFromDocker(ContainerViewModel containerViewModel)
    {
        if (containerViewModel.State is not null
            && containerViewModel.ContainerId is not null)
        {
            IAsyncDisposable? processDisposable = null;
            try
            {
                Task<ProcessResult> task;
                var outputStringBuilder = new StringBuilder();
                var spec = new ProcessSpec(FileUtil.FindFullPathFromPath("docker"))
                {
                    Arguments = $"container inspect --format=\"{{{{json .Config.Env}}}}\" {containerViewModel.ContainerId}",
                    OnOutputData = s => outputStringBuilder.Append(s),
                    KillEntireProcessTree = false,
                    ThrowOnNonZeroReturnCode = false
                };

                (task, processDisposable) = ProcessUtil.Run(spec);

                var exitCode = (await task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false)).ExitCode;
                if (exitCode == 0)
                {
                    var jsonArray = JsonNode.Parse(outputStringBuilder.ToString())?.AsArray();
                    if (jsonArray is not null)
                    {
                        var envVars = new List<EnvVar>();
                        foreach (var item in jsonArray)
                        {
                            if (item is not null)
                            {
                                var parts = item.ToString().Split('=', 2);
                                envVars.Add(new EnvVar { Name = parts[0], Value = parts[1] });
                            }
                        }
                        containerViewModel.Environment.Clear();
                        FillEnvironmentVariables(containerViewModel.Environment, envVars, envVars);
                    }
                }
            }
            catch
            {
                // If we fail to retrieve env vars from container at any point, we just skip it.
                if (processDisposable != null)
                {
                    await processDisposable.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private static ExecutableViewModel ConvertToExecutableViewModel(
        DistributedApplicationModel applicationModel, IEnumerable<Service> services, IEnumerable<Endpoint> endpoints, Executable executable)
    {
        var model = new ExecutableViewModel
        {
            Name = executable.Metadata.Name,
            NamespacedName = new(executable.Metadata.Name, null),
            CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
            ExecutablePath = executable.Spec.ExecutablePath,
            WorkingDirectory = executable.Spec.WorkingDirectory,
            Arguments = executable.Spec.Args,
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(services, executable)
        };

        FillEndpoints(applicationModel, services, endpoints, executable, model);

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }

        return model;
    }

    private static ProjectViewModel ConvertToProjectViewModel(
        DistributedApplicationModel applicationModel, IEnumerable<Service> services, IEnumerable<Endpoint> endpoints, Executable executable)
    {
        var model = new ProjectViewModel
        {
            Name = executable.Metadata!.Name,
            NamespacedName = new(executable.Metadata.Name, null),
            CreationTimeStamp = executable.Metadata?.CreationTimestamp?.ToLocalTime(),
            ProjectPath = executable.Metadata?.Annotations?[Executable.CSharpProjectPathAnnotation] ?? "",
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(services, executable)
        };

        FillEndpoints(applicationModel, services, endpoints, executable, model);

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }

        return model;
    }

    private static void FillEndpoints(
        DistributedApplicationModel applicationModel,
        IEnumerable<Service> services,
        IEnumerable<Endpoint> endpoints,
        CustomResource resource,
        ResourceViewModel resourceViewModel)
    {
        resourceViewModel.Endpoints.AddRange(
            endpoints.Where(ep => ep.Metadata.OwnerReferences.Any(or => or.Kind == resource.Kind && or.Name == resource.Metadata.Name))
            .Select(ep =>
            {
                var matchingService = services.SingleOrDefault(s => s.Metadata.Name == ep.Spec.ServiceName);
                if (matchingService is not null
                    && matchingService.Metadata.Annotations.TryGetValue(CustomResource.UriSchemeAnnotation, out var uriScheme)
                    && (string.Equals(uriScheme, "http", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(uriScheme, "https", StringComparison.OrdinalIgnoreCase)))
                {
                    var builder = new StringBuilder();
                    builder.Append(uriScheme);
                    builder.Append("://");
                    builder.Append(ep.Spec.Address);
                    builder.Append(':');
                    builder.Append(ep.Spec.Port);

                    // For project look into launch profile to append launch url
                    if (resourceViewModel is ProjectViewModel projectViewModel
                        && applicationModel.TryGetProjectWithPath(projectViewModel.ProjectPath, out var project)
                        && project.GetEffectiveLaunchProfile() is LaunchProfile launchProfile
                        && launchProfile.LaunchUrl is string launchUrl)
                    {
                        builder.Append('/');
                        builder.Append(launchUrl);
                    }

                    return builder.ToString();
                }

                return string.Empty;
            })
            .Where(e => !string.Equals(e, string.Empty, StringComparison.Ordinal)));
    }

    private static int? GetExpectedEndpointsCount(IEnumerable<Service> services, CustomResource resource)
    {
        var expectedCount = 0;
        if (resource.Metadata.Annotations is not null && resource.Metadata.Annotations.TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson))
        {
            var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(servicesProducedAnnotationJson);
            if (serviceProducerAnnotations is not null)
            {
                foreach (var serviceProducer in serviceProducerAnnotations)
                {
                    var matchingService = services.SingleOrDefault(s => s.Metadata.Name == serviceProducer.ServiceName);

                    if (matchingService is null)
                    {
                        // We don't have matching service so we cannot compute endpoint count completely
                        // So we return -1 indicating that it is unknown.
                        // Dashboard should show this as Starting
                        return null;
                    }

                    if (matchingService.Metadata.Annotations.TryGetValue(CustomResource.UriSchemeAnnotation, out var uriScheme)
                        && (string.Equals(uriScheme, "http", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(uriScheme, "https", StringComparison.OrdinalIgnoreCase)))
                    {
                        expectedCount++;
                    }
                }
            }
        }

        return expectedCount;
    }

    public void Dispose()
    {
        _kubernetesService.Dispose();
    }

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
