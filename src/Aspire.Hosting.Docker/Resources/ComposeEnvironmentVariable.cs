// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a single environment variable in a Docker Compose configuration as a key-value pair.
/// </summary>
/// <remarks>
/// This class defines an environment variable for Docker Compose configurations, encapsulating both
/// the key and its corresponding value. It inherits from the <see cref="YamlValue"/> class,
/// enabling serialization into a YAML-compliant format for use within Docker Compose YAML files.
/// </remarks>
public sealed class ComposeEnvironmentVariable(string key, string? value) : YamlValue(value ?? string.Empty)
{
    /// <summary>
    /// Gets the key part of the environment variable.
    /// </summary>
    /// <remarks>
    /// The key represents the variable name in the key-value pair of an environment variable
    /// within a Docker Compose configuration. This property is part of the serialized YAML
    /// output used to define environment variables in the Docker Compose file.
    /// </remarks>
    public string Key => key;
}
