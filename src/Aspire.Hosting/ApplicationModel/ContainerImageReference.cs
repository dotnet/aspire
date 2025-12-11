// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the fully‑qualified container image reference that should be deployed.
/// </summary>
[DebuggerDisplay("{ValueExpression}")]
public class ContainerImageReference : IManifestExpressionProvider, IValueWithReferences, IValueProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImageReference"/> class.
    /// </summary>
    /// <param name="resource">The resource that this container image is associated with.</param>
    public ContainerImageReference(IResource resource)
    {
        Resource = resource;
    }

    /// <summary>
    /// Gets the resource that this container image is associated with.
    /// </summary>
    public IResource Resource { get; }

    /// <inheritdoc/>
    public string ValueExpression => $"{{{Resource.Name}.containerImage}}";

    /// <inheritdoc/>
    public IEnumerable<object> References => [Resource];

    /// <inheritdoc/>
    async ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        return await Resource.GetFullRemoteImageNameAsync(cancellationToken).ConfigureAwait(false);
    }
}
