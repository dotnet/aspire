// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Npgsql;
using Aspire;

[assembly: ConfigurationSchema("Aspire:Azure:Npgsql", typeof(AzureNpgsqlSettings))]

[assembly: LoggingCategories(
    "Npgsql",
    "Npgsql.Command",
    "Npgsql.Connection",
    "Npgsql.Copy",
    "Npgsql.Exception",
    "Npgsql.Replication",
    "Npgsql.Transaction")]
