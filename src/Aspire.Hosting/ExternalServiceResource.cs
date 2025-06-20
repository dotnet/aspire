// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an external service resource with service discovery capabilities.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="urlExpression">The URL expression for the external service.</param>
public sealed class ExternalServiceResource(string name, ReferenceExpression urlExpression) : Resource(name), IResourceWithServiceDiscovery, IResourceWithEnvironment, IResourceWithoutLifetime
{
    /// <summary>
    /// Gets the URL expression for the external service.
    /// </summary>
    public ReferenceExpression UrlExpression => urlExpression;
}