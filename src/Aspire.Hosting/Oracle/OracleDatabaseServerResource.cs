// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Oracle Database container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The Oracle Database server password.</param>
public class OracleDatabaseServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the Oracle Database server password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the Oracle Database server.
    /// </summary>
    public string ConnectionStringExpression =>
        $"user id=system;password={{{Name}.inputs.password}};data source={PrimaryEndpoint.GetExpression(EndpointProperty.Host)}:{PrimaryEndpoint.GetExpression(EndpointProperty.Port)};";

    /// <summary>
    /// Gets the connection string for the Oracle Database server.
    /// </summary>
    /// <returns>A connection string for the Oracle Database server in the form "user id=system;password=password;data source=host:port".</returns>
    public string? GetConnectionString()
    {
        return $"user id=system;password={PasswordUtil.EscapePassword(Password)};data source={PrimaryEndpoint.Host}:{PrimaryEndpoint.Port}";
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
