// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Npgsql;
using Aspire;

[assembly: ConfigurationSchema(
    Types = [typeof(NpgsqlSettings)],
    ConfigurationPaths = ["Aspire:Npgsql"],
    LogCategories = [
        "Npgsql",
        "Npgsql.Command",
        "Npgsql.Connection",
        "Npgsql.Copy",
        "Npgsql.Exception",
        "Npgsql.Replication",
        "Npgsql.Transaction"])]
