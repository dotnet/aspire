// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SurrealDB container.
/// </summary>
public class SurrealDbServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private const string DefaultUserName = "root";
    private const string SchemeUri = "ws";

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrealDbServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the SurrealDB username.</param>
    /// <param name="password">A parameter that contains the SurrealDB password.</param>
    public SurrealDbServerResource(string name, ParameterResource? userName, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the SurrealDB instance.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets the parameter that contains the SurrealDB username.
    /// </summary>
    public ParameterResource? UserNameParameter { get; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    /// <summary>
    /// Gets the parameter that contains the SurrealDB password.
    /// </summary>
    public ParameterResource PasswordParameter { get; }

    private ReferenceExpression ConnectionString =>
        ReferenceExpression.Create(
            $"Server={SchemeUri}://{PrimaryEndpoint.Property(EndpointProperty.IPV4Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}/rpc;User={UserNameReference};Password={PasswordParameter}");

    /// <summary>
    /// Gets the connection string expression for the SurrealDB instance.
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
    /// Gets the connection string for the SurrealDB instance.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the SurrealDB instance in the form "Server=scheme://host:port;User=username;Password=password".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return ConnectionString.GetValueAsync(cancellationToken);
    }

    private readonly Dictionary<string, (string, string)> _databases = new(StringComparer.Ordinal);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the pair (namespace name, database name).
    /// </summary>
    public IReadOnlyDictionary<string, (string, string)> Databases => _databases;

    internal void AddDatabase(string name, string namespaceName, string databaseName)
    {
        _databases.TryAdd(name, (namespaceName, databaseName));
    }
}
