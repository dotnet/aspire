// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Utils;
using k8s;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

internal sealed partial class DashboardViewModelService : IDashboardViewModelService, IAsyncDisposable
{
    private const string AppHostSuffix = ".AppHost";

    private readonly string _applicationName;
    private readonly KubernetesService _kubernetesService;
    private readonly DistributedApplicationModel _applicationModel;
    private readonly ILogger _logger;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;

    private readonly Channel<(WatchEventType, string, CustomResource?)> _kubernetesChangesChannel;
    private readonly Dictionary<string, Container> _containersMap = [];
    private readonly Dictionary<string, Executable> _executablesMap = [];
    private readonly Dictionary<string, Service> _servicesMap = [];
    private readonly Dictionary<string, Endpoint> _endpointsMap = [];
    private readonly Dictionary<(ResourceKind, string), List<string>> _resourceAssociatedServicesMap = [];
    private readonly ConcurrentDictionary<string, List<EnvVar>> _additionalEnvVarsMap = [];
    private readonly HashSet<string> _containersWithTaskStarted = [];

    private readonly Channel<ResourceChanged<ResourceViewModel>> _resourceViewModelChangesChannel;

    private readonly ViewModelProcessor<ResourceViewModel> _resourceViewModelProcessor;

    public DashboardViewModelService(
        DistributedApplicationModel applicationModel, KubernetesService kubernetesService, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
    {
        _applicationModel = applicationModel;
        _kubernetesService = kubernetesService;
        _applicationName = ComputeApplicationName(hostEnvironment.ApplicationName);
        _logger = loggerFactory.CreateLogger<DashboardViewModelService>();
        _cancellationToken = _cancellationTokenSource.Token;
        _kubernetesChangesChannel = Channel.CreateUnbounded<(WatchEventType, string, CustomResource?)>();

        RunWatchTask<Executable>();
        RunWatchTask<Service>();
        RunWatchTask<Endpoint>();
        RunWatchTask<Container>();

        _resourceViewModelChangesChannel = Channel.CreateUnbounded<ResourceChanged<ResourceViewModel>>();

        Task.Run(ProcessKubernetesChanges);

        _resourceViewModelProcessor = new ViewModelProcessor<ResourceViewModel>(_resourceViewModelChangesChannel, _cancellationToken);
    }

    public string ApplicationName => _applicationName;

    public ViewModelMonitor<ResourceViewModel> GetResources() => _resourceViewModelProcessor.GetResourceMonitor();

    private void RunWatchTask<T>()
            where T : CustomResource
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var tuple in _kubernetesService.WatchAsync<T>(cancellationToken: _cancellationToken))
                {
                    await _kubernetesChangesChannel.Writer.WriteAsync(
                        (tuple.Item1, tuple.Item2.Metadata.Name, tuple.Item2), _cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Watch task over kubernetes resource of type: {resourceType} terminated", typeof(T).Name);
            }
        });
    }

    private async Task ProcessKubernetesChanges()
    {
        try
        {
            await foreach (var tuple in _kubernetesChangesChannel.Reader.ReadAllAsync(_cancellationToken))
            {
                var (watchEventType, name, resource) = tuple;
                // resource is null when we get notification from the task which fetch docker env vars
                // So we inject the resource from current copy of containersMap
                // But this could change in future
                Debug.Assert(resource is not null || _containersMap.ContainsKey(name),
                    "Received a change notification with null resource which doesn't correlate to existing container.");
                resource ??= _containersMap[name];

                switch (resource)
                {
                    case Container container:
                        await ProcessContainerChange(watchEventType, container).ConfigureAwait(false);
                        break;

                    case Executable executable
                    when !executable.IsCSharpProject():
                        await ProcessExecutableChange(watchEventType, executable).ConfigureAwait(false);
                        break;

                    case Executable executable
                    when executable.IsCSharpProject():
                        await ProcessProjectChange(watchEventType, executable).ConfigureAwait(false);
                        break;

                    case Endpoint endpoint:
                        await ProcessEndpointChange(watchEventType, endpoint).ConfigureAwait(false);
                        break;

                    case Service service:
                        await ProcessServiceChange(watchEventType, service).ConfigureAwait(false);
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Task to compute view model changes terminated");
        }
    }

    private async Task ProcessContainerChange(WatchEventType watchEventType, Container container)
    {
        if (!ProcessResourceChange(_containersMap, watchEventType, container))
        {
            return;
        }

        UpdateAssociatedServicesMap(ResourceKind.Container, watchEventType, container);
        List<EnvVar>? extraEnvVars = null;
        if (container.Status?.ContainerId is string containerId
            && !_additionalEnvVarsMap.TryGetValue(containerId, out extraEnvVars)
            && !_containersWithTaskStarted.Contains(containerId))
        {
            // Container is ready to be inspected
            // This task when returns will generate a notification in channel
            _ = Task.Run(() => ComputeEnvironmentVariablesFromDocker(containerId, container.Metadata.Name));
            _containersWithTaskStarted.Add(containerId);
        }

        var objectChangeType = ToObjectChangeType(watchEventType);

        var containerViewModel = ConvertToContainerViewModel(container, extraEnvVars);

        await WriteChange(containerViewModel, objectChangeType).ConfigureAwait(false);
    }

    private async Task ProcessExecutableChange(WatchEventType watchEventType, Executable executable)
    {
        if (!ProcessResourceChange(_executablesMap, watchEventType, executable))
        {
            return;
        }

        UpdateAssociatedServicesMap(ResourceKind.Executable, watchEventType, executable);

        var objectChangeType = ToObjectChangeType(watchEventType);
        var executableViewModel = ConvertToExecutableViewModel(executable);

        await WriteChange(executableViewModel, objectChangeType).ConfigureAwait(false);
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

        await WriteChange(projectViewModel, objectChangeType).ConfigureAwait(false);
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
                        var extraEnvVars = GetContainerEnvVars(container.Status?.ContainerId);
                        var containerViewModel = ConvertToContainerViewModel(container, extraEnvVars);

                        await WriteChange(containerViewModel).ConfigureAwait(false);
                    }
                    break;

                case "Executable":
                    if (_executablesMap.TryGetValue(ownerReference.Name, out var executable))
                    {
                        if (executable.IsCSharpProject())
                        {
                            // Project
                            var projectViewModel = ConvertToProjectViewModel(executable);

                            await WriteChange(projectViewModel).ConfigureAwait(false);
                        }
                        else
                        {
                            // Executable
                            var executableViewModel = ConvertToExecutableViewModel(executable);

                            await WriteChange(executableViewModel).ConfigureAwait(false);
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

        foreach (var kvp in _resourceAssociatedServicesMap.Where(e => e.Value.Contains(service.Metadata.Name)))
        {
            var (resourceKind, resourceName) = kvp.Key;
            switch (resourceKind)
            {
                case ResourceKind.Container:
                    if (_containersMap.TryGetValue(resourceName, out var container))
                    {
                        var extraEnvVars = GetContainerEnvVars(container.Status?.ContainerId);
                        var containerViewModel = ConvertToContainerViewModel(container, extraEnvVars);

                        await WriteChange(containerViewModel).ConfigureAwait(false);
                    }
                    break;

                case ResourceKind.Executable:
                    if (_executablesMap.TryGetValue(resourceName, out var executable))
                    {
                        if (executable.IsCSharpProject())
                        {
                            // Project
                            var projectViewModel = ConvertToProjectViewModel(executable);

                            await WriteChange(projectViewModel).ConfigureAwait(false);
                        }
                        else
                        {
                            // Executable
                            var executableViewModel = ConvertToExecutableViewModel(executable);

                            await WriteChange(executableViewModel).ConfigureAwait(false);
                        }
                    }
                    break;
            }
        }
    }

    private async Task WriteChange(ResourceViewModel resourceViewModel, ObjectChangeType changeType = ObjectChangeType.Modified)
    {
        await _resourceViewModelChangesChannel.Writer.WriteAsync(
            new ResourceChanged<ResourceViewModel>(changeType, resourceViewModel), _cancellationToken)
            .ConfigureAwait(false);
    }

    private ContainerViewModel ConvertToContainerViewModel(Container container, List<EnvVar>? additionalEnvVars)
    {
        var model = new ContainerViewModel
        {
            Name = container.Metadata.Name,
            DisplayName = container.Metadata.Name,
            Uid = container.Metadata.Uid,
            NamespacedName = new(container.Metadata.Name, null),
            ContainerId = container.Status?.ContainerId,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Image = container.Spec.Image!,
            LogSource = new DockerContainerLogSource(container.Status!.ContainerId!),
            State = container.Status?.State,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(_servicesMap.Values, container),
            Command = container.Spec.Command,
            Args = container.Spec.Args
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

        FillEndpoints(container, model, ResourceKind.Container);

        if (additionalEnvVars is not null)
        {
            FillEnvironmentVariables(model.Environment, additionalEnvVars, additionalEnvVars);
        }
        else if (container.Spec.Env is not null)
        {
            FillEnvironmentVariables(model.Environment, container.Spec.Env, container.Spec.Env);
        }

        return model;
    }

    private ExecutableViewModel ConvertToExecutableViewModel(Executable executable)
    {
        var model = new ExecutableViewModel
        {
            Name = executable.Metadata.Name,
            DisplayName = ComputeExecutableDisplayName(executable),
            Uid = executable.Metadata.Uid,
            NamespacedName = new(executable.Metadata.Name, null),
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            ExecutablePath = executable.Spec.ExecutablePath,
            WorkingDirectory = executable.Spec.WorkingDirectory,
            Arguments = executable.Spec.Args,
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(_servicesMap.Values, executable)
        };

        FillEndpoints(executable, model, ResourceKind.Executable);

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }
        return model;
    }

    private ProjectViewModel ConvertToProjectViewModel(Executable executable)
    {
        var model = new ProjectViewModel
        {
            Name = executable.Metadata.Name,
            DisplayName = ComputeExecutableDisplayName(executable),
            Uid = executable.Metadata.Uid,
            NamespacedName = new(executable.Metadata.Name, null),
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            ProjectPath = executable.Metadata.Annotations?[Executable.CSharpProjectPathAnnotation] ?? "",
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(_servicesMap.Values, executable)
        };

        FillEndpoints(executable, model, ResourceKind.Executable);

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }
        return model;
    }

    private void FillEndpoints(
        CustomResource resource,
        ResourceViewModel resourceViewModel,
        ResourceKind resourceKind)
    {
        foreach (var endpoint in _endpointsMap.Values)
        {
            if (endpoint.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == resource.Metadata.Name) != true)
            {
                continue;
            }

            var matchingService = _servicesMap.Values.SingleOrDefault(s => s.Metadata.Name == endpoint.Spec.ServiceName);
            if (matchingService?.UsesHttpProtocol(out var uriScheme) == true)
            {
                var endpointString = $"{uriScheme}://{endpoint.Spec.Address}:{endpoint.Spec.Port}";
                var proxyUrlString = $"{uriScheme}://{matchingService.AllocatedAddress}:{matchingService.AllocatedPort}";

                // For project look into launch profile to append launch url
                if (resourceViewModel is ProjectViewModel projectViewModel
                    && _applicationModel.TryGetProjectWithPath(projectViewModel.Name, projectViewModel.ProjectPath, out var project)
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

                resourceViewModel.Endpoints.Add(new(endpointString, proxyUrlString));
            }
        }

        if (_resourceAssociatedServicesMap.TryGetValue((resourceKind, resourceViewModel.Name), out var resourceServiceMappings))
        {
            foreach (var serviceName in resourceServiceMappings)
            {
                var service = _servicesMap.Values.FirstOrDefault(s => s.Metadata.Name == resourceViewModel.Name);
                if (service != null)
                {
                    resourceViewModel.Services.Add(new ResourceService(service.Metadata.Name, service.AllocatedAddress, service.AllocatedPort));
                }
            }
        }
    }

    private static int? GetExpectedEndpointsCount(IEnumerable<Service> services, CustomResource resource)
    {
        var expectedCount = 0;
        if (resource.Metadata.Annotations?.TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson) == true)
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
                        // So we return null indicating that it is unknown.
                        // Dashboard should show this as Starting
                        return null;
                    }

                    if (matchingService.UsesHttpProtocol(out _))
                    {
                        expectedCount++;
                    }
                }
            }
        }

        return expectedCount;
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

    private async Task ComputeEnvironmentVariablesFromDocker(string containerId, string name)
    {
        IAsyncDisposable? processDisposable = null;
        try
        {
            Task<ProcessResult> task;
            var outputStringBuilder = new StringBuilder();
            var spec = new ProcessSpec(FileUtil.FindFullPathFromPath("docker"))
            {
                Arguments = $"container inspect --format=\"{{{{json .Config.Env}}}}\" {containerId}",
                OnOutputData = s => outputStringBuilder.Append(s),
                KillEntireProcessTree = false,
                ThrowOnNonZeroReturnCode = false
            };

            (task, processDisposable) = ProcessUtil.Run(spec);

            var exitCode = (await task.WaitAsync(TimeSpan.FromSeconds(30), _cancellationToken).ConfigureAwait(false)).ExitCode;
            if (exitCode == 0)
            {
                var output = outputStringBuilder.ToString();
                if (output == string.Empty)
                {
                    return;
                }

                var jsonArray = JsonNode.Parse(output)?.AsArray();
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

                    _additionalEnvVarsMap[containerId] = envVars;
                    await _kubernetesChangesChannel.Writer.WriteAsync((WatchEventType.Modified, name, null), _cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to retrieve environment variables from docker container for {containerId}", containerId);
        }
        finally
        {
            if (processDisposable != null)
            {
                await processDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private List<EnvVar>? GetContainerEnvVars(string? containerId)
        => containerId is not null && _additionalEnvVarsMap.TryGetValue(containerId, out var envVars) ? envVars : null;

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
        => watchEventType switch
        {
            WatchEventType.Added => ObjectChangeType.Added,
            WatchEventType.Modified => ObjectChangeType.Modified,
            WatchEventType.Deleted => ObjectChangeType.Deleted,
            _ => ObjectChangeType.Other
        };

    public async ValueTask DisposeAsync()
    {
        // TODO: close all channels
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
    }

    private static string ComputeApplicationName(string applicationName)
    {
        if (applicationName.EndsWith(AppHostSuffix, StringComparison.OrdinalIgnoreCase))
        {
            applicationName = applicationName[..^AppHostSuffix.Length];
        }

        return applicationName;
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
