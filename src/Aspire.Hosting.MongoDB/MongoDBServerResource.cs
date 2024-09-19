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
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{ConnectionStringWithoutOptionsExpression}{ConnectionStringOptionsExpression}");

    /// <summary>
    /// Gets the connection string for the MongoDB server without options parameters.
    /// </summary>
    internal ReferenceExpression ConnectionStringWithoutOptionsExpression
    {
        get
        {
            if (PasswordParameter is null)
            {
                return ReferenceExpression.Create($"mongodb://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
            }
            else
            {
                return ReferenceExpression.Create($"mongodb://{UserNameReference}:{PasswordParameter}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
            }
        }
    }

    /// <summary>
    /// Gets the options parameters for connection string of the MongoDB server.
    /// </summary>
    internal ReferenceExpression ConnectionStringOptionsExpression
    {
        get
        {
            if (PasswordParameter is null)
            {
                return ReferenceExpression.Create($"");
            }
            else
            {
                return ReferenceExpression.Create($"?authSource={DefaultAuthenticationDatabase}&authMechanism={DefaultAuthenticationMechanism}");
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
