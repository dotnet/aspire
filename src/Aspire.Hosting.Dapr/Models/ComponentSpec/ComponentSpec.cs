// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Dapr.Models.ComponentSpec;
/// <summary>
/// The dapr component spec
/// </summary>
public sealed class ComponentSpec
{
    /// <summary>
    /// The api version for the component spec
    /// </summary>
    public readonly string ApiVersion = "dapr.io/v1alpha1";
    /// <summary>
    /// The kind of this spec.
    /// </summary>
    public readonly string Kind = "Component";
    /// <summary>
    /// The component metadata
    /// </summary>
    public required Metadata Metadata { get; init; }
    /// <summary>
    /// The configuration for the component
    /// </summary>
    public required Spec Spec { get; init; }
    /// <summary>
    /// Connection to a secret store
    /// </summary>
    public Auth? Auth { get; init; }
    /// <summary>
    /// Defined scopes where the component can be accessed from
    /// </summary>
    public List<string> Scopes { get; init; } = new();

}
