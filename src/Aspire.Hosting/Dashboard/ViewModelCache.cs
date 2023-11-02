// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Utils;
using k8s;
using NamespacedName = Aspire.Dashboard.Model.NamespacedName;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

internal abstract class ViewModelCache<TResource, TViewModel>
    where TResource : CustomResource
    where TViewModel : ResourceViewModel
{
    private readonly KubernetesService _kubernetesService;
    private readonly DistributedApplicationModel _applicationModel;

    private readonly object _syncLock = new();
    private readonly Dictionary<string, TViewModel> _resourcesMap = [];
    private readonly List<ResourceChanged<TViewModel>> _resourceChanges = [];
    private readonly Channel<ResourceChanged<TViewModel>> _publishingChannel;
    private readonly List<Channel<ResourceChanged<TViewModel>>?> _subscribedChannels = [];

    protected ViewModelCache(
        KubernetesService kubernetesService,
        DistributedApplicationModel applicationModel,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _kubernetesService = kubernetesService;
        _applicationModel = applicationModel;
        _publishingChannel = Channel.CreateUnbounded<ResourceChanged<TViewModel>>();

        Task.Run(async () =>
        {
            try
            {
                // Start an enumerator which combines underlying kubernetes watches
                // And return stream of changes in view model in publishing channel
                var enumerator = new ViewModelGeneratingEnumerator(
                    _kubernetesService,
                    _applicationModel,
                    logger,
                    FilterResource,
                    ConvertToViewModel,
                    cancellationToken);

                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    var (objectChangeType, resource) = enumerator.Current;
                    switch (objectChangeType)
                    {
                        case ObjectChangeType.Added:
                            _resourcesMap.Add(resource.Name, resource);
                            break;

                        case ObjectChangeType.Modified:
                            _resourcesMap[resource.Name] = resource;
                            break;

                        case ObjectChangeType.Deleted:
                            _resourcesMap.Remove(resource.Name);
                            break;
                    }

                    await _publishingChannel.Writer.WriteAsync(enumerator.Current, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Task to write view model change terminated for resource type: {resourceType}", typeof(TViewModel).Name);
            }
        }, cancellationToken);

        Task.Run(async () =>
        {
            try
            {
                // Receive data from publishing channel
                // Update snapshot and send data to other subscribers
                await foreach (var change in _publishingChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    Channel<ResourceChanged<TViewModel>>?[] listeningChannels = [];
                    lock (_syncLock)
                    {
                        _resourceChanges.Add(change);
                        listeningChannels = _subscribedChannels.ToArray();
                    }

                    foreach (var channel in listeningChannels)
                    {
                        if (channel is not null)
                        {
                            await channel.Writer.WriteAsync(change, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Task to publish view model changes to subscribers terminated for resource type: {resourceType}", typeof(TViewModel).Name);
            }
        }
        , cancellationToken);
    }

    public ValueTask<List<TViewModel>> GetResourcesAsync()
    {
        return ValueTask.FromResult(_resourcesMap.Values.ToList());
    }

    public async IAsyncEnumerable<ResourceChanged<TViewModel>> WatchResourceAsync(
        IEnumerable<NamespacedName>? existingObjects,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var listeningChannel = Channel.CreateUnbounded<ResourceChanged<TViewModel>>();
        List<ResourceChanged<TViewModel>> existingChanges = [];
        lock (_syncLock)
        {
            existingChanges = _resourceChanges.ToList();
            _subscribedChannels.Add(listeningChannel);
        }

        // We create a new watch based on existing changes and subscribing for few changes
        var enumerator = new ViewModelWatchEnumerator(existingChanges, listeningChannel, cancellationToken);
        while (await enumerator.MoveNextAsync().ConfigureAwait(false))
        {
            var result = enumerator.Current;
            if (result.ObjectChangeType == ObjectChangeType.Added
                && existingObjects?.Any(
                    o => string.Equals(o.Name, result.Resource.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(o.Namespace, result.Resource.NamespacedName.Namespace, StringComparison.OrdinalIgnoreCase)) == true)
            {
                continue;
            }

            yield return result;
        }
        await enumerator.DisposeAsync().ConfigureAwait(false);

        lock (_syncLock)
        {
            _subscribedChannels.Remove(listeningChannel);
        }
    }

    protected abstract bool FilterResource(TResource resource);

    protected abstract TViewModel ConvertToViewModel(
        DistributedApplicationModel applicationModel,
        IEnumerable<Service> services,
        IEnumerable<Endpoint> endpoints,
        TResource resource,
        List<EnvVar>? additionalEnvVars);

    protected static void FillEndpoints(
        DistributedApplicationModel applicationModel,
        IEnumerable<Service> services,
        IEnumerable<Endpoint> endpoints,
        CustomResource resource,
        ResourceViewModel resourceViewModel)
    {
        resourceViewModel.Endpoints.AddRange(
            endpoints.Where(ep => ep.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == resource.Metadata.Name) == true)
            .Select(ep =>
            {
                var matchingService = services.SingleOrDefault(s => s.Metadata.Name == ep.Spec.ServiceName);
                if (matchingService?.Metadata.Annotations?.TryGetValue(CustomResource.UriSchemeAnnotation, out var uriScheme) == true
                    && (string.Equals(uriScheme, "http", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(uriScheme, "https", StringComparison.OrdinalIgnoreCase)))
                {
                    var endpointString = $"{uriScheme}://{ep.Spec.Address}:{ep.Spec.Port}";

                    // For project look into launch profile to append launch url
                    if (resourceViewModel is ProjectViewModel projectViewModel
                        && applicationModel.TryGetProjectWithPath(projectViewModel.ProjectPath, out var project)
                        && project.GetEffectiveLaunchProfile() is LaunchProfile launchProfile
                        && launchProfile.LaunchUrl is string launchUrl)
                    {
                        endpointString += $"/{launchUrl}";
                    }

                    return endpointString;
                }

                return string.Empty;
            })
            .Where(e => !string.Equals(e, string.Empty, StringComparison.Ordinal)));
    }

    protected static int? GetExpectedEndpointsCount(IEnumerable<Service> services, CustomResource resource)
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

                    if (matchingService.Metadata.Annotations?.TryGetValue(CustomResource.UriSchemeAnnotation, out var uriScheme) == true
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

    protected static void FillEnvironmentVariables(List<EnvironmentVariableViewModel> target, List<EnvVar> effectiveSource, List<EnvVar>? specSource)
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

    private sealed class ViewModelGeneratingEnumerator : IAsyncEnumerator<ResourceChanged<TViewModel>>
    {
        private readonly Dictionary<string, TResource> _resourceMap = [];
        private readonly Dictionary<string, List<string>> _resourceAssociatedServicesMap = [];
        private readonly Dictionary<string, Service> _servicesMap = [];
        private readonly Dictionary<string, Endpoint> _endpointsMap = [];
        private readonly HashSet<string> _resourcesWithTaskLaunched = [];
        private readonly ConcurrentDictionary<string, List<EnvVar>> _additionalEnvVarsMap = [];

        private readonly DistributedApplicationModel _applicationModel;
        private readonly ILogger _logger;
        private readonly Func<TResource, bool> _filterResource;
        private readonly Func<DistributedApplicationModel, IEnumerable<Service>, IEnumerable<Endpoint>, TResource, List<EnvVar>?, TViewModel> _convertToViewModel;
        private readonly CancellationToken _cancellationToken;

        private readonly Channel<(WatchEventType, string, CustomResource?)> _channel;
        private readonly Queue<ResourceChanged<TViewModel>> _buffer = new();

        public ViewModelGeneratingEnumerator(
            KubernetesService kubernetesService,
            DistributedApplicationModel applicationModel,
            ILogger logger,
            Func<TResource, bool> _filterResource,
            Func<DistributedApplicationModel, IEnumerable<Service>, IEnumerable<Endpoint>, TResource, List<EnvVar>?, TViewModel> convertToViewModel,
            CancellationToken cancellationToken)
        {
            _applicationModel = applicationModel;
            _logger = logger;
            this._filterResource = _filterResource;
            _convertToViewModel = convertToViewModel;
            _cancellationToken = cancellationToken;
            Current = default!;

            _channel = Channel.CreateUnbounded<(WatchEventType, string, CustomResource?)>();

            RunWatchTask<TResource>(kubernetesService, cancellationToken);
            RunWatchTask<Service>(kubernetesService, cancellationToken);
            RunWatchTask<Endpoint>(kubernetesService, cancellationToken);
        }

        public ResourceChanged<TViewModel> Current { get; private set; }

        public async ValueTask<bool> MoveNextAsync()
        {
            while (true)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (_buffer.Count > 0)
                {
                    Current = _buffer.Dequeue();

                    return true;
                }

                // Process change in any of the watches to compute new view model if any
                var (watchEventType, name, resource) = await _channel.Reader.ReadAsync(_cancellationToken).ConfigureAwait(false);
                var objectChangeType = ToObjectChangeType(watchEventType);
                // When we don't get resource that means this is notification generated when receiving env vars from docker command
                // So we inject the resource from last copy we have
                resource ??= _resourceMap[name];
                switch (resource)
                {
                    case TResource customResource
                    when _filterResource(customResource) && ProcessChange(_resourceMap, watchEventType, customResource):

                        UpdateAssociatedServicesMap(watchEventType, customResource);
                        if (customResource is Container container)
                        {
                            if (!_additionalEnvVarsMap.TryGetValue(name, out var list)
                                && !_resourcesWithTaskLaunched.Contains(name)
                                && container.Status?.State is not null && container.Status.ContainerId is not null)
                            {
                                // Container is ready to be inspected
                                // This task when returns will generate a notification in channel
                                _ = Task.Run(() => ComputeEnvironmentVariablesFromDocker(container, _cancellationToken));
                                _resourcesWithTaskLaunched.Add(name);
                            }
                            // For containers we always send list of env vars which we may have computed earlier from docker command
                            Current = ComputeResult(objectChangeType, customResource, list);
                        }
                        else
                        {
                            Current = ComputeResult(objectChangeType, customResource, null);
                        }

                        return true;

                    case Endpoint endpoint
                    when ProcessChange(_endpointsMap, watchEventType, endpoint):

                        var matchingResource = _resourceMap.Values.FirstOrDefault(
                            e => endpoint.Metadata.OwnerReferences?.Any(or => or.Kind == e.Kind && or.Name == e.Metadata.Name) == true);

                        if (matchingResource is not null)
                        {
                            Current = ComputeResult(ObjectChangeType.Modified, matchingResource, null);

                            return true;
                        }

                        break;

                    case Service service
                    when ProcessChange(_servicesMap, watchEventType, service):

                        if (service.Metadata.Annotations?.TryGetValue(CustomResource.UriSchemeAnnotation, out var uriScheme) == true
                            && (string.Equals(uriScheme, "http", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(uriScheme, "https", StringComparison.OrdinalIgnoreCase)))
                        {
                            // We only re-compute the view model if the service can generate an endpoint
                            foreach (var kvp in _resourceAssociatedServicesMap.Where(e => e.Value.Contains(name)))
                            {
                                _buffer.Enqueue(ComputeResult(ObjectChangeType.Modified, _resourceMap[kvp.Key], null));
                            }
                        }

                        break;
                }
            }

            Current = default!;
            return false;
        }

        public ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();

            return ValueTask.CompletedTask;
        }

        private async Task ComputeEnvironmentVariablesFromDocker(Container container, CancellationToken cancellationToken)
        {
            IAsyncDisposable? processDisposable = null;
            try
            {
                Task<ProcessResult> task;
                var outputStringBuilder = new StringBuilder();
                var spec = new ProcessSpec(FileUtil.FindFullPathFromPath("docker"))
                {
                    Arguments = $"container inspect --format=\"{{{{json .Config.Env}}}}\" {container.Status!.ContainerId}",
                    OnOutputData = s => outputStringBuilder.Append(s),
                    KillEntireProcessTree = false,
                    ThrowOnNonZeroReturnCode = false
                };

                (task, processDisposable) = ProcessUtil.Run(spec);

                var exitCode = (await task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false)).ExitCode;
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

                        _additionalEnvVarsMap[container.Metadata.Name] = envVars;
                        await _channel.Writer.WriteAsync((WatchEventType.Modified, container.Metadata.Name, null), _cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // If we fail to retrieve env vars from container at any point, we just skip it.
                if (processDisposable != null)
                {
                    await processDisposable.DisposeAsync().ConfigureAwait(false);
                }
                _logger.LogError(ex, "Failed to retrieve environment variables from docker container for {containerId}", container.Status!.ContainerId);
            }
        }

        private void UpdateAssociatedServicesMap(WatchEventType watchEventType, CustomResource resource)
        {
            // We keep track of associated services for the resource
            // So whenever we get the service we can figure out if the service can generate endpoint for the resource
            if (watchEventType == WatchEventType.Deleted)
            {
                _resourceAssociatedServicesMap.Remove(resource.Metadata.Name);
            }
            else if (resource.Metadata.Annotations?.TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson) == true)
            {
                var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(servicesProducedAnnotationJson);
                if (serviceProducerAnnotations is not null)
                {
                    _resourceAssociatedServicesMap[resource.Metadata.Name]
                        = serviceProducerAnnotations.Select(e => e.ServiceName).ToList();
                }
            }
        }

        private ResourceChanged<TViewModel> ComputeResult(ObjectChangeType objectChangeType, TResource resource, List<EnvVar>? additionalEnvVars)
            => new(objectChangeType,
                _convertToViewModel(_applicationModel, _servicesMap.Values, _endpointsMap.Values, resource, additionalEnvVars));

        private static bool ProcessChange<T>(Dictionary<string, T> map, WatchEventType watchEventType, T resource)
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

        private void RunWatchTask<T>(KubernetesService kubernetesService, CancellationToken cancellationToken)
            where T : CustomResource
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var tuple in kubernetesService.WatchAsync<T>(cancellationToken: cancellationToken))
                    {
                        await _channel.Writer.WriteAsync((tuple.Item1, tuple.Item2.Metadata.Name, tuple.Item2), _cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Task to watch kubernetes changes terminated for resource: {resource} for view model: {viewModel}",
                        typeof(T).Name, typeof(TViewModel).Name);
                }
            }, cancellationToken);
        }
    }

    private sealed class ViewModelWatchEnumerator : IAsyncEnumerator<ResourceChanged<TViewModel>>
    {
        private readonly List<ResourceChanged<TViewModel>> _existingChanges;
        private readonly Channel<ResourceChanged<TViewModel>> _listeningChannel;
        private readonly CancellationToken _cancellationToken;

        private int _index;

        public ViewModelWatchEnumerator(
            List<ResourceChanged<TViewModel>> existingChanges,
            Channel<ResourceChanged<TViewModel>> listeningChannel,
            CancellationToken cancellationToken)
        {
            _existingChanges = existingChanges;
            _listeningChannel = listeningChannel;
            _cancellationToken = cancellationToken;
            Current = default!;
        }

        public ResourceChanged<TViewModel> Current { get; private set; }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (!_cancellationToken.IsCancellationRequested)
            {
                // We return from existing changes first
                // and then start listening on the channel
                if (_index < _existingChanges.Count)
                {
                    Current = _existingChanges[_index++];

                    return true;
                }

                try
                {
                    Current = await _listeningChannel.Reader.ReadAsync(_cancellationToken).ConfigureAwait(false);

                    return true;
                }
                catch { }
            }

            Current = default!;
            return false;
        }

        public ValueTask DisposeAsync()
        {
            _listeningChannel.Writer.Complete();

            return ValueTask.CompletedTask;
        }
    }
}
