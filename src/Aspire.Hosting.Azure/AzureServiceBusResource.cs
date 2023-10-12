// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureServiceBusResource(string name) : DistributedApplicationResource(name), IAzureResource, IDistributedApplicationResourceWithConnectionString
{
    // This is the full uri to the service bus namespace e.g namespace.servicebus.windows.net
    public string? ServiceBusEndpoint { get; set; }

    public string[] QueueNames { get; set; } = [];
    public string[] TopicNames { get; set; } = [];

    public string? GetConnectionString() => ServiceBusEndpoint;
}
