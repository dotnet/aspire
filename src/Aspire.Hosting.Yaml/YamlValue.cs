// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a single YAML value node, such as a string, number, or any other primitive or custom value.
/// </summary>
/// <remarks>
/// This class is a concrete implementation of the <see cref="YamlNode"/> abstract class. It encapsulates a single value
/// that can be serialized into a YAML representation.
/// </remarks>
/// <remarks>
/// Represents a YAML value node that contains a single value.
/// </remarks>
/// <remarks>
/// This class is used to encapsulate a basic value in a YAML structure, such as a string, integer, or any other scalar
/// element. It is designed to be a part of the larger YAML node hierarchy.
/// </remarks>
public class YamlValue(object value) : YamlNode
{
    /// <summary>
    /// Gets the value associated with this YAML node.
    /// </summary>
    /// <remarks>
    /// Represents the scalar value stored in this YAML node, which can be of any object type.
    /// The value can be used when writing the node's representation to a YAML writer or for further processing.
    /// </remarks>
    public object Value { get; } = value;

    /// <summary>
    /// Writes the value of the current YAML node to the specified <see cref="YamlWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="YamlWriter"/> used to output the value of this YAML node.</param>
    public override void WriteTo(YamlWriter writer)
    {
        writer.WriteValue(Value);
    }
}
