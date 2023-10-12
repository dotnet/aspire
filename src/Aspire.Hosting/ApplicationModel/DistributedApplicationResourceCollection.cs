// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ApplicationResourceCollectionDebugView))]
internal sealed class DistributedApplicationResourceCollection : IDistributedApplicationResourceCollection
{
    private readonly List<IDistributedApplicationResource> _resources = new();

    public IDistributedApplicationResource this[int index] { get => _resources[index]; set => _resources[index] = value; }
    public int Count => _resources.Count;
    public bool IsReadOnly => false;
    public void Add(IDistributedApplicationResource item) => _resources.Add(item);
    public void Clear() => _resources.Clear();
    public bool Contains(IDistributedApplicationResource item) => _resources.Contains(item);
    public void CopyTo(IDistributedApplicationResource[] array, int arrayIndex) => _resources.CopyTo(array, arrayIndex);
    public IEnumerator<IDistributedApplicationResource> GetEnumerator() => _resources.GetEnumerator();
    public int IndexOf(IDistributedApplicationResource item) => _resources.IndexOf(item);
    public void Insert(int index, IDistributedApplicationResource item) => _resources.Insert(index, item);
    public bool Remove(IDistributedApplicationResource item) => _resources.Remove(item);
    public void RemoveAt(int index) => _resources.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();

    private sealed class ApplicationResourceCollectionDebugView(DistributedApplicationResourceCollection collection)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IDistributedApplicationResource[] Items => collection.ToArray();
    }
}

