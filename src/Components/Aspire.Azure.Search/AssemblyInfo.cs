// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.Search;
using Azure.Search;

[assembly: ConfigurationSchema("Aspire:Azure:Search:Documents", typeof(AzureSearchSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:Search:ClientOptions", typeof(SearchClientOptions), exclusionPaths: ["Default"])]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity",
    "Azure-Search"
)]
