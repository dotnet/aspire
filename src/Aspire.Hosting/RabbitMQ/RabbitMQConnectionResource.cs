// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ connection.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="connectionString">The connection string.</param>
public class RabbitMQConnectionResource(string name, string? connectionString = null) : Resource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the connection string for the RabbitMQ connection.
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets the connection string for the RabbitMQ connection resource.
    /// </summary>
    /// <returns>The connection string for the RabbitMQ connection resource.</returns>
    public string? GetConnectionString() => ConnectionString;
}
