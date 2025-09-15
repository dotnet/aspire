// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a TCP/UDP port that a container can expose.
/// </summary>
[DebuggerDisplay("{ValueExpression}")]
public class ContainerPortReference(IResource resource) : IManifestExpressionProvider, IValueWithReferences, IValueProvider
{
    /// <summary>
    /// Gets the resource that this container port is associated with.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <inheritdoc/>
    public string ValueExpression => $"{{{Resource.Name}.containerPort}}";

    /// <inheritdoc/>
    public IEnumerable<object> References => [Resource];

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken)
    {
        // If the resource has endpoints defined, try to get the target port from the first endpoint
        if (Resource is IResourceWithEndpoints resourceWithEndpoints)
        {
            var endpointAnnotation = resourceWithEndpoints.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
            if (endpointAnnotation?.TargetPort is int targetPort)
            {
                return ValueTask.FromResult<string?>(targetPort.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        // Fall back to a default port if no endpoint is defined or no target port is specified
        return ValueTask.FromResult<string?>("8080");
    }
}
