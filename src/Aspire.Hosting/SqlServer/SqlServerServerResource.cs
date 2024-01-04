// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The SQL Sever password.</param>
public class SqlServerServerResource(string name, string password) : Resource(name), ISqlServerParentResource
{
    /// <summary>
    /// Gets the password for the SQL Server container resource.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string for the SQL Server.
    /// </summary>
    /// <returns>A connection string for the SQL Server in the form "Server=host,port;User ID=sa;Password=password;TrustServerCertificate=true;".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var endpoint = allocatedEndpoints.Single();

        // HACK: Use the 127.0.0.1 address because localhost is resolving to [::1] following
        //       up with DCP on this issue.
        return $"Server=127.0.0.1,{endpoint.Port};User ID=sa;Password={PasswordUtil.EscapePassword(Password)};TrustServerCertificate=true;";
    }
}
