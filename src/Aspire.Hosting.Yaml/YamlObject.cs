// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a YAML object node containing key-value pairs, where keys are strings and values are other YAML nodes.
/// </summary>
/// <remarks>
/// This class models a YAML object structure, allowing the storage and manipulation of multiple key-value pairs
/// where the keys are strings and the values are instances of <see cref="YamlNode"/>. It provides methods for
/// adding, replacing, retrieving, and checking the presence of keys within the object.
/// Additionally, the class includes functionality to serialize itself to a YAML-formatted string and to
/// deserialize a raw YAML string to a <see cref="YamlObject"/>.
/// </remarks>
public class YamlObject : YamlNode
{
    /// <summary>
    /// Stores the key-value pairs that define the properties of the YAML object node.
    /// </summary>
    /// <remarks>
    /// The <c>_properties</c> field is a dictionary where the keys are strings and the values are instances of
    /// <see cref="YamlNode"/>. This field is used internally to manage the contents of the YAML object node,
    /// allowing the addition, replacement, and retrieval of key-value pairs.
    /// </remarks>
    public readonly Dictionary<string, YamlNode> Properties = [];

    /// <summary>
    /// Adds a key-value pair to the YAML object, or updates the value if the key already exists.
    /// </summary>
    /// <param name="key">The key to associate with the value in the YAML object.</param>
    /// <param name="value">The <see cref="YamlNode"/> representing the value to be added or updated.</param>
    public void Add(string key, YamlNode value) => Properties[key] = value;

    /// <summary>
    /// Replaces the value associated with the specified key in the YAML object.
    /// </summary>
    /// <param name="key">The key whose associated value is to be replaced.</param>
    /// <param name="value">The new value to be associated with the specified key.</param>
    public void Replace(string key, YamlNode value) => Properties[key] = value;

    /// <summary>
    /// Checks if the YAML object contains a specified key.
    /// </summary>
    /// <param name="key">The key to check for in the object.</param>
    /// <returns>True if the object contains the specified key; otherwise, false.</returns>
    public bool ContainsKey(string key) => Properties.ContainsKey(key);

    /// <summary>
    /// Retrieves the <see cref="YamlNode"/> associated with the specified key in the YAML object.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <returns>
    /// The <see cref="YamlNode"/> associated with the specified key, or <c>null</c> if the key does not exist.
    /// </returns>
    public YamlNode? Get(string key) => Properties.GetValueOrDefault(key);

    /// <summary>
    /// Writes the current YAML object to the specified <see cref="YamlWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="YamlWriter"/> instance to which the YAML content should be serialized.</param>
    public override void WriteTo(YamlWriter writer)
    {
        writer.WriteStartObject();
        foreach (var (k, v) in Properties)
        {
            writer.WritePropertyName(k);
            v.WriteTo(writer);
        }
        writer.WriteEndObject();
    }

    /// <summary>
    /// Creates a <see cref="YamlObject"/> instance by deserializing the provided YAML-formatted string.
    /// </summary>
    /// <param name="yaml">The YAML-formatted string to be deserialized into a <see cref="YamlObject"/>.</param>
    /// <returns>A new <see cref="YamlObject"/> instance representing the deserialized YAML structure.</returns>
    public static YamlObject FromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        var data = deserializer.Deserialize<Dictionary<string, object>>(yaml);
        return FromDictionary(data);
    }

    /// <summary>
    /// Creates a new <see cref="YamlObject"/> populated with key-value pairs from the provided dictionary.
    /// </summary>
    /// <param name="dict">The dictionary containing string keys and object values to be converted into a <see cref="YamlObject"/>.</param>
    /// <returns>A <see cref="YamlObject"/> representing the dictionary, where keys are strings and values are converted to appropriate <see cref="YamlNode"/> types.</returns>
    protected static YamlObject FromDictionary(Dictionary<string, object> dict)
    {
        var obj = new YamlObject();
        foreach (var (key, val) in dict)
        {
            obj.Add(key, ConvertToYamlNode(val));
        }
        return obj;
    }

    /// <summary>
    /// Converts a given object into a <see cref="YamlNode"/> instance based on its type.
    /// </summary>
    /// <param name="value">The object to be converted into a <see cref="YamlNode"/>. It can be a dictionary, list, or any other value.</param>
    /// <returns>A <see cref="YamlNode"/> representation of the input object. Returns a <see cref="YamlObject"/> for dictionaries, a <see cref="YamlArray"/> for lists, or a <see cref="YamlValue"/> for other types.</returns>
    protected static YamlNode ConvertToYamlNode(object value) => value switch
    {
        Dictionary<string, object> d => FromDictionary(d),
        List<object> list => FromList(list),
        _ => new YamlValue(value)
    };

    /// <summary>
    /// Converts a list of objects into a <see cref="YamlArray"/> instance, where each item in the list
    /// is transformed into a corresponding <see cref="YamlNode"/>.
    /// </summary>
    /// <param name="list">The list of objects to be converted into a <see cref="YamlArray"/>.</param>
    /// <returns>A <see cref="YamlArray"/> containing the converted YAML nodes.</returns>
    protected static YamlArray FromList(List<object> list)
    {
        var arr = new YamlArray();
        foreach (var item in list)
        {
            arr.Add(ConvertToYamlNode(item));
        }
        return arr;
    }

    /// <summary>
    /// Converts the current YAML object into a YAML-formatted string.
    /// </summary>
    /// <returns>A string representing the YAML structure of the current object.</returns>
    public string ToYamlString()
    {
        var writer = new YamlWriter();
        WriteTo(writer);
        return writer.Compile();
    }
}
