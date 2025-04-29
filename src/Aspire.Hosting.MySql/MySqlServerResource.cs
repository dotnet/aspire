// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL container.
/// </summary>
public class MySqlServerResource : ContainerResource, IResourceWithConnectionString
{
    internal static string PrimaryEndpointName => "tcp";

    private readonly Dictionary<string, string> _databases = new(StringComparers.ResourceName);
    private readonly List<MySqlDatabaseResource> _databaseResources = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">A parameter that contains the MySQL server password.</param>
    public MySqlServerResource(string name, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new(this, PrimaryEndpointName);
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the MySQL server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets the parameter that contains the MySQL server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; }

    /// <summary>
    /// Gets the connection string expression for the MySQL server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Server={PrimaryEndpoint.Property(EndpointProperty.Host)};Port={PrimaryEndpoint.Property(EndpointProperty.Port)};User ID=root;Password={PasswordParameter}");

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal IReadOnlyList<MySqlDatabaseResource> DatabaseResources => _databaseResources;

    internal void AddDatabase(MySqlDatabaseResource database)
    {
        _databases.TryAdd(database.Name, database.DatabaseName);
        _databaseResources.Add(database);
    }
}
