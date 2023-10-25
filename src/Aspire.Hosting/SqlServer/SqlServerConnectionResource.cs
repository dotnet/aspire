// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server connection.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="connectionString">The SQL Server connection string.</param>
public class SqlServerConnectionResource(string name, string? connectionString) : Resource(name), ISqlServerResource
{
    private readonly string? _connectionString = connectionString;

    /// <summary>
    /// Gets the connection string for the SQL Server.
    /// </summary>
    /// <returns>The specified connection string.</returns>
    public string? GetConnectionString() => _connectionString;
}
