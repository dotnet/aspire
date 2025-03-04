// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Valkey resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ValkeyResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValkeyResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">A parameter that contains the Valkey server password.</param>
    public ValkeyResource(string name, ParameterResource password) : this(name)
    {
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the parameter that contains the Valkey server password.
    /// </summary>
    public ParameterResource? PasswordParameter { get; }

    /// <summary>
    /// Gets the primary endpoint for the Valkey server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Valkey server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return BuildConnectionString();
        }
    }

    private ReferenceExpression BuildConnectionString()
    {
        var builder = new ReferenceExpressionBuilder();
        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");

        if (PasswordParameter is not null)
        {
            builder.Append($",password={PasswordParameter}");
        }

        return builder.Build();
    }
}
