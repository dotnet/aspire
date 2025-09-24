// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation that stores a back-pointer to the original compute resource for an AzureProvisioningResource.
/// </summary>
public sealed class TargetComputeResourceAnnotation(IResource computeResource) : IResourceAnnotation
{
    /// <summary>
    /// Gets the compute resource associated with this annotation.
    /// </summary>
    public IResource ComputeResource { get; } = computeResource;
}
