// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ resource.
/// </summary>
public class RabbitMQServerResource : ContainerResource, IResourceWithConnectionString, IResourceWithEnvironment
{
    internal const string PrimaryEndpointName = "tcp";
    private const string DefaultUserName = "guest";

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the RabbitMQ server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the RabbitMQ server password, or <see langword="null"/> to generate a random password.</param>
    public RabbitMQServerResource(string name, ParameterResource? userName, ParameterResource? password) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;

        if (PasswordParameter is null)
        {
            // don't use special characters in the password, since it goes into a URI
            Annotations.Add(InputAnnotation.CreateDefaultPasswordInput(special: false));
            PasswordInput = new(this, "password");
        }
    }

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets the parameter that contains the RabbitMQ server user name.
    /// </summary>
    public ParameterResource? UserNameParameter { get; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    private InputReference? PasswordInput { get; }

    /// <summary>
    /// Gets the parameter that contains the PostgreSQL server password.
    /// </summary>
    public ParameterResource? PasswordParameter { get; }

    internal ReferenceExpression PasswordReference =>
        PasswordParameter is not null ?
            ReferenceExpression.Create($"{PasswordParameter}") :
            ReferenceExpression.Create($"{PasswordInput!}"); // either PasswordParameter or PasswordInput is non-null

    /// <summary>
    /// Gets the connection string expression for the RabbitMQ server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"amqp://{UserNameReference}:{PasswordReference}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");
}
