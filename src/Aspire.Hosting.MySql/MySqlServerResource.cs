// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL container.
/// </summary>
public class MySqlServerResource : ContainerResource, IResourceWithConnectionString
{
    internal static string PrimaryEndpointName => "tcp";
    private const string DefaultUserName = "root";

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
    /// Gets the host endpoint reference for this resource.
    /// </summary>
    public EndpointReferenceExpression Host => PrimaryEndpoint.Property(EndpointProperty.Host);

    /// <summary>
    /// Gets the port endpoint reference for this resource.
    /// </summary>
    public EndpointReferenceExpression Port => PrimaryEndpoint.Property(EndpointProperty.Port);

    /// <summary>
    /// Gets or sets the parameter that contains the MySQL server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; set; }

    /// <summary>
    /// Gets the connection string expression for the MySQL server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Server={PrimaryEndpoint.Property(EndpointProperty.Host)};Port={PrimaryEndpoint.Property(EndpointProperty.Port)};User ID={DefaultUserName};Password={PasswordParameter}");

    private static ReferenceExpression UserNameReference => ReferenceExpression.Create($"{DefaultUserName}");

    /// <summary>
    /// Gets the connection URI expression for the MySQL server.
    /// </summary>
    /// <remarks>
    /// Format: <c>mysql://{user}:{password}@{host}:{port}</c>.
    /// </remarks>
    public ReferenceExpression UriExpression
    {
        get
        {
            var builder = new ReferenceExpressionBuilder();
            builder.AppendLiteral("mysql://");
            builder.Append($"{UserNameReference:uri}");
            builder.AppendLiteral(":");
            builder.Append($"{PasswordParameter:uri}");
            builder.AppendLiteral("@");
            builder.Append($"{Host:uri}");
            builder.AppendLiteral(":");
            builder.Append($"{Port:uri}");

            return builder.Build();
        }
    }

    internal ReferenceExpression BuildJdbcConnectionString(string? databaseName = null)
    {
        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral("jdbc:mysql://");
        builder.Append($"{Host:uri}");
        builder.AppendLiteral(":");
        builder.Append($"{Port:uri}");

        if (databaseName is not null)
        {
            var databaseExpression = ReferenceExpression.Create($"{databaseName}");
            builder.AppendLiteral("/");
            builder.Append($"{databaseExpression:uri}");
        }

        builder.AppendLiteral("?user=");
        builder.Append($"{UserNameReference:uri}");
        builder.AppendLiteral("&password=");
        builder.Append($"{PasswordParameter:uri}");

        return builder.Build();
    }

    /// <summary>
    /// Gets the JDBC connection string for the MySQL server.
    /// </summary>
    /// <remarks>
    /// Format: <c>jdbc:mysql://{host}:{port}/?user={user}&amp;password={password}</c>.
    /// </remarks>
    public ReferenceExpression JdbcConnectionString => BuildJdbcConnectionString();

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

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties()
    {
        yield return new("Host", ReferenceExpression.Create($"{Host}"));
        yield return new("Port", ReferenceExpression.Create($"{Port}"));
        yield return new("Username", UserNameReference);
        yield return new("Password", ReferenceExpression.Create($"{PasswordParameter}"));
        yield return new("Uri", UriExpression);
        yield return new("JdbcConnectionString", JdbcConnectionString);
    }
}
