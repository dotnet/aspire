// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents an annotation for customizing the PVC provisioning policy of a Kubernetes resource.
/// </summary>
public sealed class KubernetesProvisioningPolicyAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets whether or not PVs should be dynamically provisioned for the given resource.
    /// </summary>
    public bool ShouldDynamicallyProvision { get; set; }
}

/// <summary>
/// Extension methods for configuring the provisioning policy of a Kubernetes resource.
/// </summary>
public static class KubernetesProvisioningPolicyAnnotationExtensions
{
    /// <summary>
    /// Define a provisioning policy for a Kubernetes resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="shouldDynamicallyProvision">Whether or not to dynamically provision a PV for the resource.</param>
    /// <returns>The Kubernetes resource being configured.</returns>
    public static IResourceBuilder<T> WithDynamicProvisioning<T>(this IResourceBuilder<T> builder, bool shouldDynamicallyProvision) where T: Resource
    {        
        builder.WithAnnotation(new KubernetesProvisioningPolicyAnnotation() { ShouldDynamicallyProvision = shouldDynamicallyProvision }, ResourceAnnotationMutationBehavior.Replace);

        return builder;
    }
}
