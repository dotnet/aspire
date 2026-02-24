// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An annotation that links an Azure resource to one of its private endpoint resources.
/// </summary>
/// <param name="privateEndpointResource">The private endpoint resource associated with the annotated Azure resource.</param>
internal sealed class PrivateEndpointResourceAnnotation(AzureBicepResource privateEndpointResource) : IResourceAnnotation
{
    /// <summary>
    /// Gets the private endpoint resource associated with the annotated Azure resource.
    /// </summary>
    public AzureBicepResource PrivateEndpointResource { get; } = privateEndpointResource;
}
