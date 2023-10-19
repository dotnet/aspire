// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Aspire.Dashboard.Model;

public sealed class ResourcesLoadedEventArgs<TResource> where TResource : ResourceViewModel
{
    public required IEnumerable<ResourceSelectItem<TResource>> Resources { get; init; }
    public ResourceSelectItem<TResource>? SelectedItem { get; set; }
}

public sealed class ResourceSelectorViewModel<TResource> : IAsyncDisposable where TResource : ResourceViewModel
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, ResourceSelectItem<TResource>> _resourcesMap = new();
    private readonly TaskCompletionSource _loadedTcs = new();
    private Task? _watchResourcesTask;

    public required Func<CancellationToken, Task<List<TResource>>> ResourceGetter { get; init; }
    public required Func<CancellationToken, IAsyncEnumerable<ResourceChanged<TResource>>> ResourceWatcher { get; init; }
    public Func<ResourcesLoadedEventArgs<TResource>, Task>? ResourcesLoaded { get; init; }
    public Func<TResource?, Task>? SelectedResourceChanged { get; set; }

    public ResourceSelectItem<TResource>? SelectedItem { get; set; }
    public Task LoadedAsync => _loadedTcs.Task;
    public string? UnselectedText { get; set; }

    public IEnumerable<ResourceSelectItem<TResource>> GetResources()
    {
        if (UnselectedText is not null)
        {
            yield return new ResourceSelectItem<TResource> { Text = UnselectedText };
        }

        foreach (var item in _resourcesMap.OrderBy(m => m.Key))
        {
            yield return item.Value;
        }
    }

    public async Task InitializeAsync()
    {
        if (_watchResourcesTask is null)
        {
            foreach (var item in await ResourceGetter(_cts.Token).ConfigureAwait(true))
            {
                _resourcesMap[item.Name] = new ResourceSelectItem<TResource> { Text = item.Name, Resource = item };
            }

            if (ResourcesLoaded != null)
            {
                var eventArgs = new ResourcesLoadedEventArgs<TResource>
                {
                    Resources = _resourcesMap.Values,
                    SelectedItem = SelectedItem
                };
                await ResourcesLoaded(eventArgs).ConfigureAwait(true);

                if (eventArgs.SelectedItem is not null)
                {
                    SelectedItem = eventArgs.SelectedItem;
                }
            }
            _loadedTcs.TrySetResult();

            _watchResourcesTask = Task.Run(async () =>
            {
                await foreach (var item in ResourceWatcher(_cts.Token))
                {
                    switch (item.ObjectChangeType)
                    {
                        case ObjectChangeType.Added:
                            _resourcesMap.TryAdd(item.Resource.Name, new ResourceSelectItem<TResource> { Text = item.Resource.Name, Resource = item.Resource });
                            break;
                        case ObjectChangeType.Deleted:
                            _resourcesMap.TryRemove(item.Resource.Name, out _);
                            break;
                    }

                }
            });
        }

        await _loadedTcs.Task.ConfigureAwait(true);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        if (_watchResourcesTask is { } t)
        {
            try
            {
                await t.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
