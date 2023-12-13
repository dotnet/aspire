// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

/// <summary>
/// Attribute used to automatically generate a JSON schema for a component's configuration.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class ConfigurationSchemaAttribute : Attribute
{
    /// <summary>
    /// The list of types that are bound to the configuration. These types will be walked and generate a JSON schema for all the properties.
    /// </summary>
    public Type[]? Types { get; set; }

    /// <summary>
    /// The paths corresponding to which config section each type in <see cref="Types"/> binds to.
    /// </summary>
    public string[]? ConfigurationPaths { get; set; }

    /// <summary>
    /// (optional) The config sections to exclude from the ConfigurationSchema. This is useful if there are properties you don't want to publicize in the config schema.
    /// </summary>
    public string[]? ExclusionPaths { get; set; }

    /// <summary>
    /// The list of log categories produced by the component. These categories will show up under the Logging:LogLevel section in appsettings.json.
    /// </summary>
    public string[]? LogCategories { get; set; }
}
