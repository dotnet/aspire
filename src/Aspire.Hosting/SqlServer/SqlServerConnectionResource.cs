// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.SqlServer;

public class SqlServerConnectionResource(string name, string? connectionString) : DistributedApplicationResource(name), ISqlServerResource
{
    private readonly string? _connectionString = connectionString;

    public string? GetConnectionString() => _connectionString;
}
