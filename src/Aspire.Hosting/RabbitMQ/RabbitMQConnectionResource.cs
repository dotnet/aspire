// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RabbitMQ;

/// <summary>
/// A resource that represents a RabbitMQ connection.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="connectionString">The connection string.</param>
public class RabbitMQConnectionResource(string name, string? connectionString = null) : DistributedApplicationResource(name), IDistributedApplicationResourceWithConnectionString
{
    public string? ConnectionString { get; set; } = connectionString;

    public string? GetConnectionString() => ConnectionString;
}
