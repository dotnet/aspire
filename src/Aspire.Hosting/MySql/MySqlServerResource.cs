// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="password">The MySQL server root password.</param>
public class MySqlServerResource(string name, string password) : ContainerResource(name), IResourceWithConnectionString
{
    internal static string PrimaryEndpointName => "tcp";

    private EndpointReference? _primaryEndpoint;
    private InputReference? _passwordInput;

    /// <summary>
    /// Gets the primary endpoint for the MySQL server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    internal InputReference PasswordInput => _passwordInput ??= new(this, "password");

    /// <summary>
    /// Gets the MySQL server root password.
    /// </summary>
    public string Password { get; } = password;

    /// <summary>
    /// Gets the connection string expression for the MySQL server.
    /// </summary>
    public string ConnectionStringExpression =>
        $"Server={PrimaryEndpoint.GetExpression(EndpointProperty.Host)};Port={PrimaryEndpoint.GetExpression(EndpointProperty.Port)};User ID=root;Password={PasswordInput.ValueExpression}";

    /// <summary>
    /// Gets the connection string for the MySQL server.
    /// </summary>
    /// <returns>A connection string for the MySQL server in the form "Server=host;Port=port;User ID=root;Password=password".</returns>
    public string? GetConnectionString()
    {
        return $"Server={PrimaryEndpoint.Host};Port={PrimaryEndpoint.Port};User ID=root;Password=\"{PasswordUtil.EscapePassword(Password)}\"";
    }

    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
