// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a YAML key-value pair structure, providing properties to access and modify
/// both the key and the corresponding value.
/// </summary>
public interface IYamlKeyValue : IYamlKey
{
    /// <summary>
    /// Gets or sets the value associated with the key in the YAML key-value pair.
    /// </summary>
    string Value { get; set; }
}
