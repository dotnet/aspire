// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a YAML array structure, which is a collection of <see cref="YamlNode"/> objects.
/// Enables adding, replacing, and writing child nodes in sequence.
/// </summary>
public class YamlArray : YamlNode
{
    private readonly List<YamlNode> _items = [];

    /// <summary>
    /// Gets the collection of <see cref="YamlNode"/> objects contained within the YAML array.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="YamlNode"/> objects that make up the current YAML array.
    /// </value>
    public IReadOnlyList<YamlNode> Items => _items;

    /// <summary>
    /// Adds a specified <see cref="YamlNode"/> to the current YAML array.
    /// </summary>
    /// <param name="value">The <see cref="YamlNode"/> to be added to the array.</param>
    public void Add(YamlNode value) => _items.Add(value);

    /// <summary>
    /// Replaces the <see cref="YamlNode"/> at the specified index in the current YAML array with a new value.
    /// </summary>
    /// <param name="index">The zero-based index of the node to replace.</param>
    /// <param name="value">The new <see cref="YamlNode"/> to insert at the specified index.</param>
    public void ReplaceAt(int index, YamlNode value)
    {
        if (index >= 0 && index < _items.Count)
        {
            _items[index] = value;
        }
    }

    /// <summary>
    /// Writes the YAML representation of the current YAML array to the specified <see cref="YamlWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="YamlWriter"/> to which the YAML array will be written.</param>
    public override void WriteTo(YamlWriter writer)
    {
        writer.WriteStartArray();
        foreach (var item in _items)
        {
            writer.WriteArrayItem(item);
        }
        writer.WriteEndArray();
    }
}
