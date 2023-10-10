// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureServiceBusComponent(string name) : DistributedApplicationComponent(name), IAzureComponent, IDistributedApplicationComponentWithConnectionString
{
    public string? ServiceBusEndpoint { get; set; }

    public string[] QueueNames { get; set; } = [];
    public string[] TopicNames { get; set; } = [];

    public string? GetConnectionString() => ServiceBusEndpoint;
}
