// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RabbitMQ;

public class RabbitMQConnectionResource(string name, string? connectionString = null) : DistributedApplicationResource(name), IDistributedApplicationResourceWithConnectionString
{
    public string? ConnectionString { get; set; } = connectionString;

    public string? GetConnectionString() => ConnectionString;
}
