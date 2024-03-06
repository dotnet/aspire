// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The SQL Sever password.</param>
public class SqlServerServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;
    private InputReference? _passwordInput;

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    internal InputReference PasswordInput => _passwordInput ??= new(this, "password");

    /// <summary>
    /// Gets the password for the SQL Server container resource.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the SQL Server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return $"Server={PrimaryEndpoint.GetExpression(EndpointProperty.Host)},{PrimaryEndpoint.GetExpression(EndpointProperty.Port)};User ID=sa;Password={PasswordInput.ValueExpression};TrustServerCertificate=true";
        }
    }

    /// <summary>
    /// Gets the connection string for the SQL Server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the SQL Server in the form "Server=host,port;User ID=sa;Password=password;TrustServerCertificate=true".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return new(GetConnectionString());
    }

    /// <summary>
    /// Gets the connection string for the SQL Server.
    /// </summary>
    /// <returns>A connection string for the SQL Server in the form "Server=host,port;User ID=sa;Password=password;TrustServerCertificate=true".</returns>
    public string? GetConnectionString()
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionString();
        }

        // HACK: Use the 127.0.0.1 address because localhost is resolving to [::1] following
        //       up with DCP on this issue.
        return $"Server=127.0.0.1,{PrimaryEndpoint.Port};User ID=sa;Password={PasswordUtil.EscapePassword(Password)};TrustServerCertificate=true";
    }

    private readonly Dictionary<string, string> _databases = new(StringComparers.ResourceName);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
