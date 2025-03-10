// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a class for defining and managing environment variables within a Docker Compose configuration.
/// </summary>
/// <remarks>
/// This class inherits from the <see cref="YamlObject"/> class, allowing it to serialize and manage
/// environment variables in a format suitable for Docker Compose YAML configurations.
/// Environment variables are specified as a collection of key-value pairs.
/// </remarks>
public sealed class ComposeServiceEnvironment : YamlObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComposeServiceEnvironment"/> class.
    /// </summary>
    public ComposeServiceEnvironment()
    {
    }

    /// <summary>
    /// Adds a new environment variable to the <see cref="ComposeServiceEnvironment"/> instance
    /// within the Docker Compose configuration.
    /// </summary>
    /// <param name="variable">The environment variable to add, represented as a <see cref="ComposeEnvironmentVariable"/>.</param>
    /// <returns>
    /// The updated instance of <see cref="ComposeServiceEnvironment"/>, allowing method chaining.
    /// </returns>
    public ComposeServiceEnvironment AddEnvironmentalVariable(ComposeEnvironmentVariable variable)
    {
        this.Add(variable.Key, variable);
        return this;
    }
}
