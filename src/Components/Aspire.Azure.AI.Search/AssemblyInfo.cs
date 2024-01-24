// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.AI.Search;
using Azure.Search.Documents;

[assembly: ConfigurationSchema("Aspire:Azure:AI:Search", typeof(AzureAISearchSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:AI:Search:ClientOptions", typeof(SearchClientOptions), exclusionPaths: ["Default"])]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity"
)]
