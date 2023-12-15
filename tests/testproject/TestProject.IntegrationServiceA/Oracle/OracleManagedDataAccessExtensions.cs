// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Oracle.ManagedDataAccess.Client;

// This is a workaround while https://github.com/dotnet/aspire/pull/1004 is not merged.
public static class OracleManagedDataAccessExtensions
{
    public static void AddOracleClient(this WebApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionString = builder.Configuration.GetConnectionString(connectionName);
        builder.Services.AddScoped(_ => new OracleConnection(connectionString));
    }

    public static void AddKeyedOracleClient(this WebApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var connectionString = builder.Configuration.GetConnectionString(connectionName);
        builder.Services.AddKeyedSingleton(connectionName, (serviceProvider, _) => new OracleConnection(connectionString));
    }
}
