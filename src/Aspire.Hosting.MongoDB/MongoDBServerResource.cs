// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class MongoDBServerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    private const string DefaultUserName = "admin";
    private const string DefaultAuthenticationDatabase = "admin";
    private const string DefaultAuthenticationMechanism = "SCRAM-SHA-256";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Initialize a resource that represents a MongoDB container.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userNameParameter">A parameter that contains the MongoDb server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="passwordParameter">A parameter that contains the MongoDb server password.</param>
    public MongoDBServerResource(string name, ParameterResource? userNameParameter, ParameterResource? passwordParameter) : this(name)
    {
        UserNameParameter = userNameParameter;
        PasswordParameter = passwordParameter;
    }

    /// <summary>
    /// Gets the primary endpoint for the MongoDB server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the host endpoint reference for this resource.
    /// </summary>
    public EndpointReferenceExpression Host => PrimaryEndpoint.Property(EndpointProperty.Host);

    /// <summary>
    /// Gets the port endpoint reference for this resource.
    /// </summary>
    public EndpointReferenceExpression Port => PrimaryEndpoint.Property(EndpointProperty.Port);

    /// <summary>
    /// Gets the parameter that contains the MongoDb server password.
    /// </summary>
    public ParameterResource? PasswordParameter { get; }
        
    /// <summary>
    /// Gets the parameter that contains the MongoDb server username.
    /// </summary>
    public ParameterResource? UserNameParameter { get; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    /// <summary>
    /// Gets the connection URI expression for the MongoDB server.
    /// </summary>
    /// <remarks>
    /// Format: <c>mongodb://[user:password@]{host}:{port}[?authSource=admin&amp;authMechanism=SCRAM-SHA-256]</c>. The credential and query segments are included only when a password is configured.
    /// </remarks>
    public ReferenceExpression UriExpression => BuildConnectionString();

    private static ReferenceExpression AuthenticationDatabaseReference => ReferenceExpression.Create($"{DefaultAuthenticationDatabase}");

    private static ReferenceExpression AuthenticationMechanismReference => ReferenceExpression.Create($"{DefaultAuthenticationMechanism}");

    internal ReferenceExpression BuildConnectionString(string? databaseName = null)
    {
        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral("mongodb://");

        if (PasswordParameter is not null)
        {
            builder.Append($"{UserNameReference:uri}:{PasswordParameter:uri}@");
        }

        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");

        if (databaseName is not null)
        {
            var databaseExpression = ReferenceExpression.Create($"{databaseName}");
            builder.AppendLiteral("/");
            builder.Append($"{databaseExpression:uri}");
        }

        if (PasswordParameter is not null)
        {
            builder.AppendLiteral("?authSource=");
            builder.Append($"{AuthenticationDatabaseReference:uri}");
            builder.AppendLiteral("&authMechanism=");
            builder.Append($"{AuthenticationMechanismReference:uri}");
        }

        return builder.Build();
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

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties()
    {
        yield return new("Host", ReferenceExpression.Create($"{Host}"));
        yield return new("Port", ReferenceExpression.Create($"{Port}"));
        yield return new("Username", UserNameReference);

        if (PasswordParameter is not null)
        {
            yield return new("Password", ReferenceExpression.Create($"{PasswordParameter}"));
            yield return new("AuthenticationDatabase", AuthenticationDatabaseReference);
            yield return new("AuthenticationMechanism", AuthenticationMechanismReference);
        }

        yield return new("Uri", UriExpression);
    }
}
