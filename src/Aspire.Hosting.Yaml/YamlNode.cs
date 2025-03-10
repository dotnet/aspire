// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents the base class for all YAML node types.
/// </summary>
/// <remarks>
/// This abstract class defines the common interface for all YAML nodes, enabling consistent processing and handling
/// of various YAML data types, such as objects, arrays, or primitive values.
/// </remarks>
public abstract class YamlNode
{
    /// <summary>
    /// Writes the YAML representation of the current node to the specified <see cref="YamlWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="YamlWriter"/> to which the YAML representation will be written. Must not be null.</param>
    public abstract void WriteTo(YamlWriter writer);
}
