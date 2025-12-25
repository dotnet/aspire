// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models;

/// <summary>
/// Represents the complete Aspire application model extracted from NuGet packages.
/// </summary>
public sealed class ApplicationModel
{
    /// <summary>
    /// Gets or sets the integrations (NuGet packages) included in this application.
    /// </summary>
    public List<IntegrationModel> Integrations { get; init; } = [];

    /// <summary>
    /// Gets or sets the resource types available in this application.
    /// </summary>
    public List<ResourceModel> Resources { get; init; } = [];

    /// <summary>
    /// Gets or sets the extension methods on IDistributedApplicationBuilder.
    /// </summary>
    public List<ExtensionMethodModel> BuilderExtensions { get; init; } = [];

    /// <summary>
    /// Gets a flat list of all extension methods across all integrations.
    /// </summary>
    public IEnumerable<ExtensionMethodModel> AllExtensionMethods =>
        Integrations.SelectMany(i => i.ExtensionMethods)
            .Concat(BuilderExtensions);

    /// <summary>
    /// Gets the unique resource types by their type name.
    /// </summary>
    public Dictionary<string, ResourceModel> ResourcesByTypeName =>
        Resources.ToDictionary(r => r.TypeName);
}
