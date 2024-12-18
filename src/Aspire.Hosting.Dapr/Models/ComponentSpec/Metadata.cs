// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Dapr.Models.ComponentSpec;
/// <summary>
/// Defines the metadata for a component
/// </summary>
public sealed class Metadata
{
    /// <summary>
    /// The name of the component.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// The namespace where the component lives.
    /// </summary>
    public string? Namespace { get; init; }
}