// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents a Kafka broker container.
/// </summary>
/// <param name="name"></param>
public class KafkaContainerResource(string name) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEnvironment
{
    /// <summary>
    /// Gets the connection string for Kafka broker.
    /// </summary>
    /// <returns>A connection string for the Kafka in the form "host:port" to be passed as <see href="https://docs.confluent.io/platform/current/clients/confluent-kafka-dotnet/_site/api/Confluent.Kafka.ClientConfig.html#Confluent_Kafka_ClientConfig_BootstrapServers">BootstrapServers</see>.</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"Kafka resource \"{Name}\" does not have endpoint annotation.");
        }

        return allocatedEndpoints.SingleOrDefault()?.EndPointString;
    }

    internal int GetPort()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException($"Kafka resource \"{Name}\" does not have endpoint annotation.");
        }

        return allocatedEndpoints.Single().Port;
    }
}
