// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation that stores a back-pointer to the parent resource for an AzureProvisioningResource.
/// This is the inverse of the DeploymentTargetAnnotation.
/// </summary>
public sealed class DeploymentTargetParentAnnotation(IResource parentResource) : IResourceAnnotation
{
    /// <summary>
    /// Gets the parent resource associated with this annotation.
    /// </summary>
    public IResource ParentResource { get; } = parentResource;
}
