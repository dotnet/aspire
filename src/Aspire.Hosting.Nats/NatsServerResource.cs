// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a NATS server container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class NatsServerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    internal const string PrimaryNatsSchemeName = "nats";
    private const string DefaultUserName = "nats";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the NATS server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the NATS server password.</param>
    public NatsServerResource(string name, ParameterResource? userName, ParameterResource? password) : this(name)
    {
        UserNameParameter = userName;
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the NATS server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets or sets the user name for the NATS server.
    /// </summary>
    public ParameterResource? UserNameParameter { get; set; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create(UserNameParameter) :
            ReferenceExpression.Create(DefaultUserName);

    /// <summary>
    /// Gets or sets the password for the NATS server.
    /// </summary>
    public ParameterResource? PasswordParameter { get; set; }

    /// <summary>
    /// Gets the connection string expression for the NATS server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => BuildConnectionString();

    internal ReferenceExpression BuildConnectionString()
    {
        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral($"{PrimaryNatsSchemeName}://");

        if (PasswordParameter is not null)
        {
            builder.Append($"{UserNameReference}:{PasswordParameter}@");
        }

        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");

        return builder.Build();
    }
}
