// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;

namespace Aspire.Hosting.Azure.CosmosDB;

internal static class CosmosUtils
{
    internal static bool IsEmulatorConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return false;
        }

        var builder = new DbConnectionStringBuilder();
        builder.ConnectionString = connectionString;
        if (!builder.TryGetValue("AccountKey", out var v))
        {
            return false;
        }
        var accountKeyFromConnectionString = v.ToString();
        return accountKeyFromConnectionString == CosmosConstants.EmulatorAccountKey;
    }

    /// <summary>
    /// Parses the connection string to extract the account endpoint and connection string.
    /// </summary>
    /// <remarks>
    /// The connection string can be in the following formats:
    /// A valid Uri
    /// AccountEndpoint={valid Uri} - optionally with [;Database={databaseName}[;Container={containerName}]]
    /// {valid CosmosDB ConnectionString} - optionally with [;Database={databaseName}[;Container={containerName}]]
    /// </remarks>
    internal static CosmosConnectionInfo ParseConnectionString(string connectionString)
    {
        Uri? accountEndpoint;
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out accountEndpoint))
        {
            return new CosmosConnectionInfo(accountEndpoint, null);
        }

        var connectionBuilder = new StableConnectionStringBuilder(connectionString);

        // Strip out the database and container from the connection string in order
        // to tell if we are left with just AccountEndpoint.
        if (connectionBuilder.TryGetValue("Database", out var databaseValue))
        {
            connectionBuilder["Database"] = null;
        }

        if (connectionBuilder.TryGetValue("Container", out var containerValue))
        {
            connectionBuilder["Container"] = null;
        }

        // if we are only left with the AccountEndpoint, then we set the AccountEndpoint and
        // not the connection string and include the database and container names.
        if (connectionBuilder.Count() == 1 &&
            connectionBuilder.TryGetValue("AccountEndpoint", out var accountEndpointValue) &&
            Uri.TryCreate(accountEndpointValue.ToString(), UriKind.Absolute, out accountEndpoint))
        {
            return new CosmosConnectionInfo(accountEndpoint, null, databaseValue?.ToString(), containerValue?.ToString());
        }

        return new CosmosConnectionInfo(null, connectionBuilder.ConnectionString, databaseValue?.ToString(), containerValue?.ToString());
    }
}

internal readonly struct CosmosConnectionInfo
{
    public CosmosConnectionInfo(Uri? accountEndpoint, string? connectionString, string? databaseName = null, string? containerName = null)
    {
        Debug.Assert(accountEndpoint is not null ^ connectionString is not null, "only one should be set.");

        AccountEndpoint = accountEndpoint;
        ConnectionString = connectionString;
        DatabaseName = databaseName;
        ContainerName = containerName;
    }

    public Uri? AccountEndpoint { get; }
    public string? ConnectionString { get; }
    public string? DatabaseName { get; }
    public string? ContainerName { get; }
}
