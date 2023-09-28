// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ApplicationComponentCollectionDebugView))]
internal sealed class DistributedApplicationComponentCollection : IDistributedApplicationComponentCollection
{
    private readonly List<IDistributedApplicationComponent> _components = new();

    public IDistributedApplicationComponent this[int index] { get => _components[index]; set => _components[index] = value; }
    public int Count => _components.Count;
    public bool IsReadOnly => false;
    public void Add(IDistributedApplicationComponent item) => _components.Add(item);
    public void Clear() => _components.Clear();
    public bool Contains(IDistributedApplicationComponent item) => _components.Contains(item);
    public void CopyTo(IDistributedApplicationComponent[] array, int arrayIndex) => _components.CopyTo(array, arrayIndex);
    public IEnumerator<IDistributedApplicationComponent> GetEnumerator() => _components.GetEnumerator();
    public int IndexOf(IDistributedApplicationComponent item) => _components.IndexOf(item);
    public void Insert(int index, IDistributedApplicationComponent item) => _components.Insert(index, item);
    public bool Remove(IDistributedApplicationComponent item) => _components.Remove(item);
    public void RemoveAt(int index) => _components.RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => _components.GetEnumerator();

    private sealed class ApplicationComponentCollectionDebugView(DistributedApplicationComponentCollection collection)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IDistributedApplicationComponent[] Items => collection.ToArray();
    }
}

