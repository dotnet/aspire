// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Postgres;

public class PostgresConnectionResource(string name, string? connectionString) : DistributedApplicationResource(name), IPostgresResource
{
    private readonly string? _connectionString = connectionString;

    public string? GetConnectionString() => _connectionString;
}
