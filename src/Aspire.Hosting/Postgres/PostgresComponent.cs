// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public class PostgresComponent(string name, string? connectionString) : DistributedApplicationComponent(name), IPostgresComponent
{
    public string? GetConnectionString(string? databaseName = null) =>
        connectionString is null ? null :
        databaseName is null ?
            connectionString :
            connectionString.EndsWith(';') ?
                $"{connectionString}Database={databaseName}" :
                $"{connectionString};Database={databaseName}";

}
