// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Yaml;

/// <summary>
/// Represents a YAML key structure, providing a property to access and modify the key.
/// </summary>
public interface IYamlKey
{
    /// <summary>
    /// Gets or sets the key associated with the YAML key-value pair.
    /// </summary>
    string Key { get; set; }
}
