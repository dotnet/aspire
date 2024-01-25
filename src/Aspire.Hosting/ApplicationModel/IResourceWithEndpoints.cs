// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has endpoints associated with it.
/// </summary>
public interface IResourceWithEndpoints : IResource
{
    /// <summary>
    /// Gets an endpoint reference for the specified endpoint name.
    /// </summary>
    /// <param name="endpointName">The name of the endpoint.</param>
    /// <returns>An <see cref="EndpointReference"/> object representing the endpoint reference 
    /// for the specified endpoint.</returns>
    public EndpointReference GetEndpoint(string endpointName)
    {
        return new EndpointReference(this, endpointName);
    }
}
