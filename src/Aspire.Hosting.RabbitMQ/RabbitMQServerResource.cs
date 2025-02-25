// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ resource.
/// </summary>
public class RabbitMQServerResource : ContainerResource, IResourceWithConnectionString, IResourceWithEnvironment
{
    internal const string PrimaryEndpointName = "tcp";
    internal const string ManagementEndpointName = "management";
    private const string DefaultUserName = "guest";

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the RabbitMQ server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the RabbitMQ server password.</param>
    public RabbitMQServerResource(string name, ParameterResource? userName, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the RabbitMQ server.
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

    /// <summary>
    /// Gets the parameter that contains the RabbitMQ server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; }

    /// <summary>
    /// Gets the connection string expression for the RabbitMQ server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"amqp://{UserNameReference}:{PasswordParameter}@{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");
}
