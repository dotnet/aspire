// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents an annotation for customizing a Kubernetes service.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KubernetesServiceCustomizationAnnotation"/> class.
/// </remarks>
/// <param name="configure">The configuration action for customizing the Kubernetes service.</param>
public sealed class KubernetesServiceCustomizationAnnotation(Action<KubernetesServiceResource> configure) : IResourceAnnotation
{
    /// <summary>
    /// Gets the configuration action for customizing the Kubernetes service.
    /// </summary>
    public Action<KubernetesServiceResource> Configure { get; } = configure ?? throw new ArgumentNullException(nameof(configure));
}
