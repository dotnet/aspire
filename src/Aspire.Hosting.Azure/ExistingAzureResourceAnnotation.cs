// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents a resource that is not managed by Aspire's provisioning or
/// container management layer.
/// </summary>
public class ExistingAzureResourceAnnotation(IResourceBuilder<ParameterResource> nameParameter) : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the existing resource.
    /// </summary>
    public IResourceBuilder<ParameterResource> NameParameter { get; } = nameParameter;
}
