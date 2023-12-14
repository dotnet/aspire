// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.StackExchange.Redis;
using Aspire;
using StackExchange.Redis;

[assembly: ConfigurationSchema(
    Types = [typeof(StackExchangeRedisSettings), typeof(ConfigurationOptions)],
    ConfigurationPaths = ["Aspire:StackExchange:Redis", "Aspire:StackExchange:Redis:ConfigurationOptions"],
    LogCategories = ["Aspire.StackExchange.Redis"])]
