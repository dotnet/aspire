// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a PostgreSQL container.
/// </summary>
public class PostgresServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">The PostgreSQL server password, or <see langword="null"/> to generate a random password.</param>
    public PostgresServerResource(string name, string? password = null) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        PasswordInput = new(this, "password");

        Annotations.Add(InputAnnotation.CreateDefaultPasswordInput(password));
    }

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    internal InputReference PasswordInput { get; }

    /// <summary>
    /// Gets the PostgreSQL server password.
    /// </summary>
    public string Password => PasswordInput.Input.Value ?? throw new InvalidOperationException("Password cannot be null.");

    /// <summary>
    /// Gets the connection string expression for the PostgreSQL server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return $"Host={PrimaryEndpoint.GetExpression(EndpointProperty.Host)};Port={PrimaryEndpoint.GetExpression(EndpointProperty.Port)};Username=postgres;Password={PasswordInput.ValueExpression}";
        }
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the PostgreSQL server in the form "Host=host;Port=port;Username=postgres;Password=password".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return new(GetConnectionString());
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL server.
    /// </summary>
    /// <returns>A connection string for the PostgreSQL server in the form "Host=host;Port=port;Username=postgres;Password=password".</returns>
    public string? GetConnectionString()
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionString();
        }

        return $"Host={PrimaryEndpoint.Host};Port={PrimaryEndpoint.Port};Username=postgres;Password={PasswordUtil.EscapePassword(Password)}";
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
