// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp;

using System.Collections.Immutable;
using System.Diagnostics;
using Aspire.Hosting.Dcp.Model;
using k8s.Models;

[DebuggerDisplay("Resource = {Resource.DcpResource.Metadata.Name}")]
internal class CreationQueueItem: IEquatable<CreationQueueItem>
{
    public readonly AppResource Resource;
    public readonly List<AppResource> Dependencies;
    public readonly Func<AppResource, CancellationToken, Task> DoCreate;

    public CreationQueueItem(AppResource resource, Func<AppResource, CancellationToken, Task> doCreate, IEnumerable<AppResource> dependencies)
    {
        if (resource.DcpResource is null || (resource.DcpResource is not Container && resource.DcpResource is not Executable && resource.DcpResource is not ContainerNetworkTunnelProxy))
        {
            throw new ArgumentException("Resource must be a Container, Executable, or ContainerNetworkTunnelProxy", nameof(resource));
        }
        Resource = resource;
        DoCreate = doCreate;
        Dependencies = new(dependencies);
    }

    public bool Equals(CreationQueueItem? other)
    {
        if (other is null)
        {
            return false;
        }

        return Resource.Equals(other.Resource);
    }
}

internal class ResourceCreationQueue
{
    private readonly List<CreationQueueItem> _items = new();
    private readonly List<int> _deps = new();
    private const int Created = -1;

    public void AddRange(IEnumerable<CreationQueueItem> items)
    {
        foreach (var item in items)
        {
            _items.Add(item);
            _deps.Add(item.Dependencies.Count);
        }
    }

    public IEnumerable<CreationQueueItem> GetCreationBatch() 
    {
        return _items.Where((item, index) => _deps[index] == 0).ToImmutableArray();
    }

    public void MarkItemsCreated(IEnumerable<CreationQueueItem> createdItems)
    {
        foreach (var createdItem in createdItems)
        {
            int index = _items.FindIndex(i => i.Equals(createdItem));
            if (index >= 0)
            {
                _deps[index] = Created;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_deps[i] != Created && _items[i].Dependencies.Any(d => d.Equals(createdItem.Resource)))
                    {
                        _deps[i] = _deps[i] - 1;
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Created item '{createdItem.Resource.DcpResource.Name}:{createdItem.Resource.DcpResource.GetType().Name}' not found in the queue", nameof(createdItems));
            }
        }
    }

    public void ThrowIfUncreatedItemsRemain()
    {
        var uncreatedItems = new List<CreationQueueItem>();
        for (int i = 0; i < _items.Count; i++)
        {
            if (_deps[i] != Created)
            {
                uncreatedItems.Add(_items[i]);
            }
        }
        if (uncreatedItems.Count > 0)
        {
            var itemDescriptions = uncreatedItems
                .Select(i => $"'{i.Resource.DcpResource.Name}:{i.Resource.DcpResource.GetType().Name}'")
                .ToArray();
            var message = $"The following resources could not be created due to unresolved dependencies: {string.Join(", ", itemDescriptions)}";
            throw new InvalidOperationException(message);
        }
    }

}

