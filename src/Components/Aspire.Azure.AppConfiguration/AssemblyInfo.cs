// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Azure.AppConfiguration;

[assembly: ConfigurationSchema("Aspire:Azure:AppConfiguration", typeof(AzureAppConfigurationSettings))]

[assembly: LoggingCategories(
    "Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh")]
