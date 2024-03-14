// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.ServiceBus;

namespace Aspire.Hosting.Azure.Provisioning;

/// <summary>
/// Represents an Azure Service Bus resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureServiceBusConstructResource(string name)
    : AzureServiceBusResource(name)
{
    internal Action<ResourceModuleConstruct>? ConfigureConstruct { get; set; }
    internal List<(string Name, Action<ResourceModuleConstruct, ServiceBusQueue>? Configure)> Queues { get; } = [];
    internal List<(string Name, Action<ResourceModuleConstruct, ServiceBusTopic>? Configure)> Topics { get; } = [];
    internal List<(string TopicName, string Name, Action<ResourceModuleConstruct, ServiceBusSubscription>? Configure)> Subscriptions { get; } = [];
}
