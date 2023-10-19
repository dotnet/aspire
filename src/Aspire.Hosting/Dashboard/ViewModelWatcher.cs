// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using k8s;
using NamespacedName = Aspire.Dashboard.Model.NamespacedName;

namespace Aspire.Hosting.Dashboard;

internal sealed class ViewModelWatcher<TResource, TViewModel> : IAsyncEnumerable<ResourceChanged<TViewModel>>
    where TResource : CustomResource
    where TViewModel : ResourceViewModel
{
    private readonly KubernetesService _kubernetesService;
    private readonly DistributedApplicationModel _applicationModel;
    private readonly IEnumerable<NamespacedName>? _existingObjects;
    private readonly Func<TResource, bool> _validResource;
    private readonly Func<DistributedApplicationModel, IEnumerable<Service>, IEnumerable<Endpoint>, TResource, TViewModel> _convertToViewModel;

    public ViewModelWatcher(
        KubernetesService kubernetesService,
        DistributedApplicationModel applicationModel,
        IEnumerable<NamespacedName>? existingObjects,
        Func<TResource, bool> validResource,
        Func<DistributedApplicationModel, IEnumerable<Service>, IEnumerable<Endpoint>, TResource, TViewModel> convertToViewModel)
    {
        _kubernetesService = kubernetesService;
        _applicationModel = applicationModel;
        _existingObjects = existingObjects;
        _validResource = validResource;
        _convertToViewModel = convertToViewModel;
    }

    public IAsyncEnumerator<ResourceChanged<TViewModel>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Enumerator(this, cancellationToken);
    }

    private sealed class Enumerator : IAsyncEnumerator<ResourceChanged<TViewModel>>
    {
        private readonly Dictionary<string, TResource> _resourceMap = [];
        private readonly Dictionary<string, List<string>> _resourceAssociatedServicesMap = [];
        private readonly Dictionary<string, Service> _servicesMap = [];
        private readonly Dictionary<string, Endpoint> _endpointsMap = [];

        private readonly DistributedApplicationModel _applicationModel;
        private readonly IEnumerable<NamespacedName>? _existingObjects;
        private readonly Func<TResource, bool> _validResource;
        private readonly Func<DistributedApplicationModel, IEnumerable<Service>, IEnumerable<Endpoint>, TResource, TViewModel> _convertToViewModel;
        private readonly CancellationToken _cancellationToken;

        private readonly Channel<(WatchEventType, CustomResource)> _channel;
        private readonly Queue<ResourceChanged<TViewModel>> _buffer = new();

        public Enumerator(
            ViewModelWatcher<TResource, TViewModel> enumerable,
            CancellationToken cancellationToken)
        {
            _applicationModel = enumerable._applicationModel;
            _existingObjects = enumerable._existingObjects;
            _validResource = enumerable._validResource;
            _convertToViewModel = enumerable._convertToViewModel;
            _cancellationToken = cancellationToken;
            Current = default!;

            _channel = Channel.CreateUnbounded<(WatchEventType, CustomResource)>();

            RunWatchTask<TResource>(enumerable._kubernetesService, cancellationToken);
            RunWatchTask<Service>(enumerable._kubernetesService, cancellationToken);
            RunWatchTask<Endpoint>(enumerable._kubernetesService, cancellationToken);
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

                var (watchEventType, resource) = await _channel.Reader.ReadAsync(_cancellationToken).ConfigureAwait(false);
                var objectChangeType = ToObjectChangeType(watchEventType);
                switch (resource)
                {
                    case TResource customResource
                    when _validResource(customResource) && ProcessChange(_resourceMap, watchEventType, customResource):

                        UpdateAssociatedServicesMap(watchEventType, customResource);
                        if (!IsAddedEventForExistingResource(watchEventType, customResource))
                        {
                            Current = ComputeResult(objectChangeType, customResource);

                            return true;
                        }

                        break;

                    case Endpoint endpoint
                    when ProcessChange(_endpointsMap, watchEventType, endpoint):

                        var matchingResource = _resourceMap.Values.FirstOrDefault(
                            e => endpoint.Metadata.OwnerReferences.Any(or => or.Kind == e.Kind && or.Name == e.Metadata.Name));

                        if (matchingResource is not null)
                        {
                            Current = ComputeResult(ObjectChangeType.Modified, matchingResource);

                            return true;
                        }

                        break;

                    case Service service
                    when ProcessChange(_servicesMap, watchEventType, service):

                        foreach (var kvp in _resourceAssociatedServicesMap.Where(e => e.Value.Contains(service.Metadata.Name)))
                        {
                            _buffer.Enqueue(ComputeResult(ObjectChangeType.Modified, _resourceMap[kvp.Key]));
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

        private void RunWatchTask<T>(KubernetesService kubernetesService, CancellationToken cancellationToken)
            where T : CustomResource
        {
            _ = Task.Run(async () =>
            {
                await foreach (var tuple in kubernetesService.WatchAsync<T>(cancellationToken: cancellationToken))
                {
                    await _channel.Writer.WriteAsync(tuple, _cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private void UpdateAssociatedServicesMap(WatchEventType watchEventType, CustomResource resource)
        {
            if (watchEventType == WatchEventType.Deleted)
            {
                _resourceAssociatedServicesMap.Remove(resource.Metadata.Name);
            }
            else if (resource.Metadata.Annotations is not null && resource.Metadata.Annotations
                .TryGetValue(CustomResource.ServiceProducerAnnotation, out var servicesProducedAnnotationJson))
            {
                var serviceProducerAnnotations = JsonSerializer.Deserialize<ServiceProducerAnnotation[]>(servicesProducedAnnotationJson);
                if (serviceProducerAnnotations is not null)
                {
                    _resourceAssociatedServicesMap[resource.Metadata.Name]
                        = serviceProducerAnnotations.Select(e => e.ServiceName).ToList();
                }
            }
        }

        private bool IsAddedEventForExistingResource(WatchEventType watchEventType, CustomResource resource)
            => watchEventType == WatchEventType.Added
                && _existingObjects?.Any(
                    o => string.Equals(o.Name, resource.Metadata.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(o.Namespace, resource.Metadata.NamespaceProperty, StringComparison.OrdinalIgnoreCase)) == true;

        private ResourceChanged<TViewModel> ComputeResult(ObjectChangeType objectChangeType, TResource resource)
            => new(objectChangeType, _convertToViewModel(_applicationModel, _servicesMap.Values, _endpointsMap.Values, resource));

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
    }
}
