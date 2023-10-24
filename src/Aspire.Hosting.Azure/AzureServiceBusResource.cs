// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class AzureServiceBusResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    // This is the full uri to the service bus namespace e.g namespace.servicebus.windows.net
    public string? ServiceBusEndpoint { get; set; }

    public string[] QueueNames { get; set; } = [];
    public string[] TopicNames { get; set; } = [];

    public string? GetConnectionString() => ServiceBusEndpoint;
}
