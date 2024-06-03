// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
public class MongoDBServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    private const string DefaultUserName = "admin";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the MongoDb server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the MongoDb server password.</param>
    public MongoDBServerResource(string name, ParameterResource? userName, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        UserNameParameter = userName;
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the MongoDB server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the parameter that contains the MongoDb server user name.
    /// </summary>
    public ParameterResource? UserNameParameter { get; }

    /// <summary>
    /// Gets the parameter that contains the MongoDb server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"mongodb://{UserNameReference}:{Uri.EscapeDataString(PasswordParameter.Value)}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

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
