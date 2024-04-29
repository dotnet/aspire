// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.Storage.Queues;
using Azure.Storage.Queues;

[assembly: ConfigurationSchema("Aspire:Azure:Storage:Queues", typeof(AzureStorageQueuesSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:Storage:Queues:ClientOptions", typeof(QueueClientOptions), exclusionPaths: ["Default"])]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity")]
