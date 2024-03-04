// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The RabbitMQ server password.</param>
public class RabbitMQServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEnvironment
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// The RabbitMQ server password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the RabbitMQ server for the manifest.
    /// </summary>
    public string ConnectionStringExpression =>
        $"amqp://guest:{{{Name}.inputs.password}}@{PrimaryEndpoint.GetExpression(EndpointProperty.Host)}:{PrimaryEndpoint.GetExpression(EndpointProperty.Port)}";

    /// <summary>
    /// Gets the connection string for the RabbitMQ server.
    /// </summary>
    /// <returns>A connection string for the RabbitMQ server in the form "amqp://user:password@host:port".</returns>
    public string? GetConnectionString()
    {
        return $"amqp://guest:{Password}@{PrimaryEndpoint.Host}:{PrimaryEndpoint.Port}";
    }
}
