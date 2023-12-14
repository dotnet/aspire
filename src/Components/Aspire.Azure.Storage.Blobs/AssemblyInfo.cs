// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Storage.Blobs;
using Aspire;
using Azure.Storage.Blobs;

[assembly: ConfigurationSchema(
    Types = [typeof(AzureStorageBlobsSettings), typeof(BlobClientOptions)],
    ConfigurationPaths = ["Aspire:Azure:Storage:Blobs", "Aspire:Azure:Storage:Blobs:ClientOptions"],
    ExclusionPaths = ["Aspire:Azure:Storage:Blobs:ClientOptions:Default"],
    LogCategories = [
        "Azure",
        "Azure.Core",
        "Azure.Identity"])]
