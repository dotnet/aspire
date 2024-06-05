// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// /// <param name="userNameParameter">A parameter that contains the MongoDb server user name, or <see langword="null"/> to use a default value.</param>
/// <param name="passwordParameter">A parameter that contains the MongoDb server password.</param>
public class MongoDBServerResource(string name, ParameterResource? userNameParameter = null, ParameterResource? passwordParameter = null) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    private const string DefaultUserName = "admin";

    private EndpointReference? _primaryEndpoint;
    
    /// <summary>
    /// Gets the primary endpoint for the MongoDB server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the parameter that contains the MongoDb server password.
    /// </summary>
    public ParameterResource? PasswordParameter => passwordParameter;

    internal ReferenceExpression UserNameReference =>
        userNameParameter is not null ?
            ReferenceExpression.Create($"{userNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
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
