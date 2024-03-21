// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL container.
/// </summary>
public class MySqlServerResource : ContainerResource, IResourceWithConnectionString
{
    internal static string PrimaryEndpointName => "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">A parameter that contains the MySQL server password, or <see langword="null"/> to generate a random password.</param>
    public MySqlServerResource(string name, ParameterResource? password) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        PasswordParameter = password;

        if (PasswordParameter is null)
        {
            Annotations.Add(InputAnnotation.CreateDefaultPasswordInput());
            PasswordInput = new(this, "password");
        }
    }

    /// <summary>
    /// Gets the primary endpoint for the MySQL server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    private InputReference? PasswordInput { get; }

    /// <summary>
    /// Gets the parameter that contains the MySQL server password.
    /// </summary>
    public ParameterResource? PasswordParameter { get; }

    internal ReferenceExpression PasswordReference =>
        PasswordParameter is not null ?
            ReferenceExpression.Create($"{PasswordParameter}") :
            ReferenceExpression.Create($"{PasswordInput!}"); // either PasswordParameter or PasswordInput is non-null

    /// <summary>
    /// Gets the MySQL server root password.
    /// </summary>
    internal string Password => PasswordParameter?.Value ?? PasswordInput!.Input.Value ?? throw new InvalidOperationException("Password cannot be null.");

    /// <summary>
    /// Gets the connection string expression for the MySQL server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Server={PrimaryEndpoint.Property(EndpointProperty.Host)};Port={PrimaryEndpoint.Property(EndpointProperty.Port)};User ID=root;Password={PasswordReference}");

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
