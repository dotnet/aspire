// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a RabbitMQ resource.
/// </summary>
public class RabbitMQServerResource : ContainerResource, IResourceWithConnectionString, IResourceWithEnvironment
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">The RabbitMQ server password, or <see langword="null"/> to generate a random password.</param>
    public RabbitMQServerResource(string name, string? password = null) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        PasswordInput = new(this, "password");

        // don't use special characters in the password, since it goes into a URI
        Annotations.Add(InputAnnotation.CreateDefaultPasswordInput(password, special: false));
    }

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    internal InputReference PasswordInput { get; }

    /// <summary>
    /// Gets the RabbitMQ server password.
    /// </summary>
    public string Password => PasswordInput.Input.Value ?? throw new InvalidOperationException("Password cannot be null.");

    private ReferenceExpression ConnectionString =>
        ReferenceExpression.Create(
            $"amqp://guest:{PasswordInput}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

    /// <summary>
    /// Gets the connection string expression for the RabbitMQ server for the manifest.
    /// </summary>
    public string ConnectionStringExpression =>
        ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string for the RabbitMQ server.
    /// </summary>
    /// <returns>A connection string for the RabbitMQ server in the form "amqp://user:password@host:port".</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken) =>
        await ConnectionString.GetValueAsync(cancellationToken).ConfigureAwait(false);
}
