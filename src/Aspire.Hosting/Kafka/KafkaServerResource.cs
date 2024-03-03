// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents a Kafka broker.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class KafkaServerResource(string name) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEnvironment
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Kafka broker.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for Kafka broker for the manifest.
    /// </summary>
    public string ConnectionStringExpression =>
       $"{PrimaryEndpoint.GetExpression(EndpointProperty.Host)}:{PrimaryEndpoint.GetExpression(EndpointProperty.Port)}";

    /// <summary>
    /// Gets the connection string for Kafka broker.
    /// </summary>
    /// <returns>A connection string for the Kafka in the form "host:port" to be passed as <see href="https://docs.confluent.io/platform/current/clients/confluent-kafka-dotnet/_site/api/Confluent.Kafka.ClientConfig.html#Confluent_Kafka_ClientConfig_BootstrapServers">BootstrapServers</see>.</returns>
    public string? GetConnectionString()
    {
        return $"{PrimaryEndpoint.Host}:{PrimaryEndpoint.Port}";
    }
}
