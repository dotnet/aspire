// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents Pulsar Manager resource
/// </summary>
public class PulsarManagerResource : ContainerResource
{
    internal const string FrontendEndpointName = "frontend";
    internal const string BackendEndpointName = "backend";

    /// <summary>
    /// Initializes a new instance of the <see cref="PulsarManagerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public PulsarManagerResource(string name) : base(name)
    {
        BackendEndpoint = new(this, FrontendEndpointName);
        FrontendEndpoint = new(this, FrontendEndpointName);
    }

    /// <summary>
    /// Gets the frontend endpoint of Pulsar Manager
    /// </summary>
    public EndpointReference FrontendEndpoint { get; }

    /// <summary>
    /// Gets the backend endpoint of Pulsar Manager
    /// </summary>
    public EndpointReference BackendEndpoint { get; }
}
