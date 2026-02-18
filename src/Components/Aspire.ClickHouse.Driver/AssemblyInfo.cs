// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.ClickHouse.Driver;

[assembly: ConfigurationSchema("Aspire:ClickHouse:Driver", typeof(ClickHouseClientSettings))]

[assembly: LoggingCategories(
    "ClickHouse.Driver",
    "ClickHouse.Driver.Connection",
    "ClickHouse.Driver.Command",
    "ClickHouse.Driver.Transport",
    "ClickHouse.Driver.BulkCopy",
    "ClickHouse.Driver.Client")]
