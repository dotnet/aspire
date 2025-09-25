// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

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
    [Obsolete("Use the constructor that accepts IServiceProvider to support async deployment image tag callbacks.")]
    public ContainerImageReference(IResource resource)
    {
        Resource = resource;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerImageReference"/> class with service provider access.
    /// </summary>
    /// <param name="resource">The resource that this container image is associated with.</param>
    /// <param name="serviceProvider">The service provider for accessing services during callback execution.</param>
    public ContainerImageReference(IResource resource, IServiceProvider? serviceProvider)
    {
        Resource = resource;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the resource that this container image is associated with.
    /// </summary>
    public IResource Resource { get; }

    /// <summary>
    /// Gets the service provider for accessing services during callback execution, if available.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

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

        string tag;
        if (Resource.TryGetLastAnnotation<DeploymentImageTagCallbackAnnotation>(out var deploymentTag))
        {
            if (ServiceProvider is null)
            {
                throw new InvalidOperationException($"Deployment image tag callback requires a service provider, but none was provided to ContainerImageReference for resource '{Resource.Name}'.");
            }

            var executionContext = ServiceProvider.GetRequiredService<DistributedApplicationExecutionContext>();
            var context = new DeploymentImageTagCallbackAnnotationContext
            {
                Resource = Resource,
                CancellationToken = cancellationToken,
                ExecutionContext = executionContext
            };
            tag = await deploymentTag.Callback(context).ConfigureAwait(false);
        }
        else if (Resource.TryGetLastAnnotation<ContainerImageAnnotation>(out var annotation))
        {
            tag = annotation.Tag ?? "latest";
        }
        else
        {
            tag = "latest";
        }
        
        return $"{registryEndpoint}/{Resource.Name.ToLowerInvariant()}:{tag}";
    }
}
