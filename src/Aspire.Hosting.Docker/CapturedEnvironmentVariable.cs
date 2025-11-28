// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a captured environment variable that will be written to the .env file 
/// adjacent to the Docker Compose file.
/// </summary>
public sealed class CapturedEnvironmentVariable
{
    /// <summary>
    /// Initializes a new instance of <see cref="CapturedEnvironmentVariable"/>.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="description">An optional description for the environment variable.</param>
    /// <param name="defaultValue">The default value for the environment variable.</param>
    /// <param name="source">The source object that originated this environment variable.</param>
    public CapturedEnvironmentVariable(string name, string? description = null, string? defaultValue = null, object? source = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        Description = description;
        DefaultValue = defaultValue;
        Source = source;
    }

    /// <summary>
    /// Gets the name of the environment variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the description for the environment variable.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the default value for the environment variable.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the source object that originated this environment variable.
    /// This could be a <see cref="Aspire.Hosting.ApplicationModel.ParameterResource"/>,
    /// <see cref="Aspire.Hosting.ApplicationModel.ContainerMountAnnotation"/>, or other source types.
    /// </summary>
    public object? Source { get; set; }
}
