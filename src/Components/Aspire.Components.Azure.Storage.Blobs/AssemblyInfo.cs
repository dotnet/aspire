// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Storage.Blobs;
using Aspire;
using Azure.Storage.Blobs;

[assembly: ConfigurationSchema("Aspire:Azure:Storage:Blobs", typeof(AzureStorageBlobsSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:Storage:Blobs:ClientOptions", typeof(BlobClientOptions), exclusionPaths: ["Default"])]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity")]
