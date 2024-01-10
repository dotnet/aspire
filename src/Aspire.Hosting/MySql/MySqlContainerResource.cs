// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The MySQL server root password.</param>
public class MySqlContainerResource(string name, string password) : ContainerResource(name), IMySqlParentResource
{
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string for the MySQL server.
    /// </summary>
    /// <returns>A connection string for the MySQL server in the form "Server=host;Port=port;User ID=root;Password=password".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single(); // We should only have one endpoint for MySQL.

        var connectionString = $"Server={allocatedEndpoint.Address};Port={allocatedEndpoint.Port};User ID=root;Password=\"{PasswordUtil.EscapePassword(Password)}\";";
        return connectionString;
    }
}
