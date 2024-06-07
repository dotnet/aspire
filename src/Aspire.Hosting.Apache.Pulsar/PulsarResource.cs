// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Pulsar resource.
/// </summary>
public class PulsarResource(string name)
    : ContainerResource(name),
    IResourceWithConnectionString
{
    internal const int ServiceInternalPort = 8080;
    internal const string ServiceEndpointName = "service";
    private EndpointReference? _serviceEndpoint;

    internal const int BrokerInternalPort = 6650;
    internal const string BrokerEndpointName = "broker";
    private EndpointReference? _brokerEndpoint;

    /// <summary>
    /// Gets the service endpoint for Pulsar server.
    /// </summary>
    public EndpointReference ServiceEndpoint => _serviceEndpoint ??= new(this, ServiceEndpointName);

    /// <summary>
    /// Gets the broker endpoint for Pulsar server.
    /// </summary>
    public EndpointReference BrokerEndpoint => _brokerEndpoint ??= new(this, BrokerEndpointName);

    /// <summary>
    /// Gets the connection string expression for Pulsar
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
        => ReferenceExpression.Create($"{BrokerEndpoint}");
}
