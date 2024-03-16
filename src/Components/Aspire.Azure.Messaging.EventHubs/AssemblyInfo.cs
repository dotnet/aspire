// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;

[assembly: ConfigurationSchema("Aspire:Azure:Messaging:EventHubs", typeof(AzureMessagingEventHubsSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:Messaging:EventHubs:EventHubConsumerClient:ClientOptions", typeof(EventHubConsumerClientOptions))]
[assembly: ConfigurationSchema("Aspire:Azure:Messaging:EventHubs:EventHubProducerClient:ClientOptions", typeof(EventHubProducerClientOptions))]
[assembly: ConfigurationSchema("Aspire:Azure:Messaging:EventHubs:EventProcessorClient:ClientOptions", typeof(EventProcessorClientOptions))]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity",
    "Azure.Messaging.EventHubs")]
