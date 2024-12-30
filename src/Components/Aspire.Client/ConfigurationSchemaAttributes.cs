// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

/// <summary>
/// Attribute used to automatically generate a JSON schema for a component's configuration.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ConfigurationSchemaAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSchemaAttribute"/> class.
    /// </summary>
    /// <param name="path">The path corresponding to which config section <see cref="Type"/> binds to.</param>
    /// <param name="type">The type that is bound to the configuration. This type will be walked and generate a JSON schema for all the properties.</param>
    /// <param name="exclusionPaths">(optional) The config sections to exclude from the ConfigurationSchema. This is useful if there are properties you don't want to publicize in the config schema.</param>
    public ConfigurationSchemaAttribute(string path, Type type, string[]? exclusionPaths = null)
    {
        Path = path;
        Type = type;
        ExclusionPaths = exclusionPaths;
    }

    /// <summary>
    /// The path corresponding to which config section <see cref="Type"/> binds to.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The type that is bound to the configuration. This type will be walked and generate a JSON schema for all the properties.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// (optional) The config sections to exclude from the ConfigurationSchema. This is useful if there are properties you don't want to publicize in the config schema.
    /// </summary>
    public string[]? ExclusionPaths { get; }
}
