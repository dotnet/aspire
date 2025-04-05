// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.Data.AppConfiguration;
using Azure.Data.AppConfiguration;

[assembly: ConfigurationSchema("Aspire:Azure:Data:AppConfiguration", typeof(AzureDataAppConfigurationSettings))]
[assembly: ConfigurationSchema("Aspire:Azure:Data:AppConfiguration:ClientOptions", typeof(ConfigurationClientOptions), exclusionPaths: ["Default"])]

[assembly: LoggingCategories(
    "Azure",
    "Azure.Core",
    "Azure.Identity",
    "Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh")]
