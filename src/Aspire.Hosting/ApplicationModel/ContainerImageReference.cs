// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the fully‑qualified container image reference that should be deployed.
/// </summary>
[DebuggerDisplay("{ValueExpression}")]
public class ContainerImageReference(IResource resource) : IManifestExpressionProvider, IValueWithReferences, IValueProvider
{
    /// <summary>
    /// Gets the resource that this container image is associated with.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <inheritdoc/>
    public string ValueExpression => $"{{{Resource.Name}.containerImage}}";

    /// <inheritdoc/>
    public IEnumerable<object> References => [Resource];

    /// <inheritdoc/>
    async ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        var deploymentTarget = Resource.GetDeploymentTargetAnnotation() ?? throw new InvalidOperationException($"Resource '{Resource.Name}' does not have a deployment target.");
        var containerRegistry = deploymentTarget.ContainerRegistry ?? throw new InvalidOperationException($"Resource '{Resource.Name}' does not have a container registry.");
        var registryEndpoint = await containerRegistry.Endpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);

        var tag = Resource.TryGetLastAnnotation<DeploymentImageTagCallbackAnnotation>(out var deploymentTag) ? deploymentTag.Callback() :
                  Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var annotation) ? annotation.Tag : "latest";
        return $"{registryEndpoint}/{Resource.Name.ToLowerInvariant()}:{tag}";
    }
}
