// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server container.
/// </summary>
public class SqlServerServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">A parameter that contains the SQL Server password.</param>
    public SqlServerServerResource(string name, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new(this, PrimaryEndpointName);
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the SQL Server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets the parameter that contains the SQL Server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; private set; }

    private ReferenceExpression ConnectionString =>
        ReferenceExpression.Interpolate(
            $"Server={PrimaryEndpoint.Property(EndpointProperty.IPV4Host)},{PrimaryEndpoint.Property(EndpointProperty.Port)};User ID=sa;Password={PasswordParameter};TrustServerCertificate=true");

    /// <summary>
    /// Gets the connection string expression for the SQL Server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return ConnectionString;
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

        return ConnectionString.GetValueAsync(cancellationToken);
    }

    private readonly Dictionary<string, string> _databases = new(StringComparers.ResourceName);
    private readonly List<SqlServerDatabaseResource> _databaseResources = [];

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void SetPassword(ParameterResource password)
    {
        ArgumentNullException.ThrowIfNull(password);

        PasswordParameter = password;
    }

    internal void AddDatabase(SqlServerDatabaseResource database)
    {
        _databases.TryAdd(database.Name, database.DatabaseName);
        _databaseResources.Add(database);
    }

    internal IReadOnlyList<SqlServerDatabaseResource> DatabaseResources => _databaseResources;
}
