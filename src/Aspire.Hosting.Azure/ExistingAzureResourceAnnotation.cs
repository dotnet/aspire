// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a resource that is not managed by Aspire's provisioning or
/// container management layer.
/// </summary>
public sealed class ExistingAzureResourceAnnotation(ParameterResource nameParameter,
    ParameterResource? resourceGroupParameter = null) : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the existing resource.
    /// </summary>
    public ParameterResource NameParameter { get; } = nameParameter;

    /// <summary>
    /// Gets the name of the existing resource group.
    /// </summary>
    public ParameterResource? ResourceGroupParameter { get; } = resourceGroupParameter;
}
