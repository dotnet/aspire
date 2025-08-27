// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Microsoft.Azure.StackExchangeRedis;
using Aspire;
using StackExchange.Redis;

[assembly: ConfigurationSchema("Aspire:Microsoft:Azure:StackExchange:Redis", typeof(AzureStackExchangeRedisSettings))]
[assembly: ConfigurationSchema("Aspire:Microsoft:Azure:StackExchange:Redis:ConfigurationOptions", typeof(ConfigurationOptions))]

[assembly: LoggingCategories("StackExchange.Redis")]