// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs;

[assembly: ConfigurationSchema("Aspire:Azure:Messaging:EventHubs", typeof(AzureMessagingEventHubsSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:Messaging:EventHubs:ClientOptions", typeof(EventHubConnectionOptions))] // TODO: this is temporary

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity",
    "Azure.Messaging.EventHubs")]
