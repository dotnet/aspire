// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a dictionary of connection-related properties.
/// </summary>
public sealed class ConnectionProperties : IDictionary<string, object>
{
    private readonly Dictionary<string, object> _properties = new();

    /// <summary>
    /// Initializes a new instance of the ConnectionProperties class with the specified resource owner.
    /// </summary>
    /// <param name="owner">The resource that provides the connection string. Cannot be null.</param>
    public ConnectionProperties(IResourceWithConnectionString owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Resource = owner;
    }

    /// <summary>
    /// Gets the resource owner.
    /// </summary>
    public IResourceWithConnectionString Resource { get; }

    /// <inheritdoc/>
    public ICollection<string> Keys => _properties.Keys;

    /// <inheritdoc/>
    public ICollection<object> Values => _properties.Values;

    /// <inheritdoc/>
    public int Count => _properties.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public object this[string key] { get => _properties[key]; set => _properties[key] = value; }

    /// <inheritdoc/>
    public void Add(string key, object value) => _properties.Add(key, value);

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _properties.ContainsKey(key);

    /// <inheritdoc/>
    public bool Remove(string key) => _properties.Remove(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _properties.TryGetValue(key, out value);

    /// <inheritdoc/>
    public void Add(KeyValuePair<string, object> item) => _properties.Add(item.Key, item.Value);

    /// <inheritdoc/>
    public void Clear() => _properties.Clear();

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, object> item) => _properties.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object>>)_properties).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<string, object> item) => _properties.Remove(item.Key);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _properties.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
