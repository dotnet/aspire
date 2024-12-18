// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dapr.Models.ComponentSpec;
/// <summary>
/// The configuration for the component
/// </summary>
public sealed class Spec
{
    /// <summary>
    /// The component type
    /// </summary>
    public required string Type { get; init; }
    /// <summary>
    /// The version of the component type
    /// </summary>
    public required string Version { get; init; } = "v1";
    /// <summary>
    /// The defined timeout of the component
    /// </summary>
    public string? InitTimeout { get; init; }
    /// <summary>
    /// If errors are ignored
    /// </summary>
    public bool? IgnoreErrors { get; init; }
    /// <summary>
    /// All the configuration values of the component
    /// </summary>
    public List<MetadataValue> Metadata { get; init; } = new();
}