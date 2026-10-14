// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Identifies a Bicep module whose resources are managed through a provider other than Azure Resource Manager.
/// </summary>
/// <remarks>
/// Provisioning, deployment cancellation, and local state management remain available, but ARM resource inspection,
/// relocation, deletion, and drift probes do not apply. Resource-group deletion leaves these resources and their
/// cached identification outputs intact, and reports them for provider-specific cleanup.
/// </remarks>
public sealed class NonArmResourceAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes an annotation describing the non-ARM objects created by a module.
    /// </summary>
    /// <param name="resourceIdOutputs">
    /// A mapping from provider resource types to Bicep output names containing their object IDs.
    /// Only non-secret identification outputs should be included because they are reported in deletion results.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourceIdOutputs"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a resource type or output name is empty.</exception>
    /// <example>
    /// <code lang="csharp">
    /// resource.Annotations.Add(new NonArmResourceAnnotation(new Dictionary&lt;string, string&gt;
    /// {
    ///     ["Microsoft.Graph/applications"] = "objectId",
    ///     ["Microsoft.Graph/servicePrincipals"] = "servicePrincipalId"
    /// }));
    /// </code>
    /// </example>
    public NonArmResourceAnnotation(IReadOnlyDictionary<string, string> resourceIdOutputs)
    {
        ArgumentNullException.ThrowIfNull(resourceIdOutputs);
        var outputs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (resourceType, outputName) in resourceIdOutputs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputName);
            outputs.Add(resourceType, outputName);
        }

        ResourceIdOutputs = new ReadOnlyDictionary<string, string>(outputs);
    }

    /// <summary>
    /// Gets the provider resource types and the output names used to identify objects left after resource-group deletion.
    /// </summary>
    public IReadOnlyDictionary<string, string> ResourceIdOutputs { get; }
}
