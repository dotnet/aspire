// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class MongoDBServerResource(string name) : ContainerResource(name), IResourceWithConnectionString, IResourceWithDirectConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    private const string DefaultUserName = "admin";
    internal const string DefaultAuthenticationDatabase = "admin";
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
    /// Gets the direct connection string for the MongoDB server.
    /// </summary>
    /// <remarks>
    /// This is useful to connect to the resource when replica sets are enabled. In those cases, the database will only
    /// accept the registered name for the replica, which is only accessible from within the container network.
    /// </remarks>
    public ReferenceExpression DirectConnectionStringExpression => BuildConnectionString(directConnection: true);

    internal ReferenceExpression BuildConnectionString(string? databaseName = null, bool directConnection = false)
    {
        var builder = new ReferenceExpressionBuilder();

        builder.AppendLiteral("mongodb://");

        if (PasswordParameter is not null)
        {
            builder.Append($"{UserNameReference}:{PasswordParameter}@");
        }

        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");

        var slashAppended = false;
        var queryAppended = false;

        if (databaseName is { })
        {
            EnsureSlash(builder, ref slashAppended);
            builder.AppendFormatted(databaseName);
        }

        if (PasswordParameter is not null)
        {
            EnsureQuery(builder, ref queryAppended);
            builder.Append($"authSource={DefaultAuthenticationDatabase}&authMechanism={DefaultAuthenticationMechanism}");
        }

        if (Annotations.OfType<MongoDbReplicaSetAnnotation>().FirstOrDefault() is { ReplicaSetName: { } replicaSetName })
        {
            EnsureQuery(builder, ref queryAppended);
            builder.Append($"{MongoDbReplicaSetAnnotation.QueryName}={replicaSetName}");
        }

        if (directConnection)
        {
            EnsureQuery(builder, ref queryAppended);
            builder.AppendLiteral("directConnection=true");
        }

        return builder.Build();

        static void EnsureQuery(ReferenceExpressionBuilder builder, ref bool queryAppended)
        {
            if (!queryAppended)
            {
                builder.AppendLiteral("?");
                queryAppended = true;
            }
            else
            {
                builder.AppendLiteral("&");
            }
        }
        static void EnsureSlash(ReferenceExpressionBuilder builder, ref bool slashAppended)
        {
            if (!slashAppended)
            {
                builder.AppendLiteral("/");
                slashAppended = true;
            }
        }
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
