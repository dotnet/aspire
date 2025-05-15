// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Microsoft.Extensions.Configuration.AzureAppConfiguration;

[assembly: ConfigurationSchema("Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration", typeof(AzureAppConfigurationSettings))]

[assembly: LoggingCategories(
    "Microsoft.Extensions.Configuration.AzureAppConfiguration.Refresh")]
