// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Oracle Database container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The Oracle Database server password.</param>
public class OracleDatabaseContainerResource(string name, string password) : ContainerResource(name), IOracleDatabaseParentResource
{
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string for the Oracle Database server.
    /// </summary>
    /// <returns>A connection string for the Oracle Database server in the form "user id=system;password=password;data source=localhost:port".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for Oracle Database.

        var connectionString = $"user id=system;password={PasswordUtil.EscapePassword(Password)};data source={allocatedEndpoint.Address}:{allocatedEndpoint.Port}";
        return connectionString;
    }
}
