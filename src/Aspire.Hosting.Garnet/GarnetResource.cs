// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Garnet resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class GarnetResource(string name) : ContainerResource(ThrowIfNull(name)), IResourceWithConnectionString
{

    /// <summary>
    /// Initializes a new instance of the <see cref="GarnetResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">A parameter that contains the Garnet server password.</param>
    public GarnetResource(string name, ParameterResource password) : this(name)
    {
        PasswordParameter = password;
    }

    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Garnet server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the parameter that contains the Garnet server password.
    /// </summary>
    public ParameterResource? PasswordParameter { get; }

    /// <summary>
    /// Gets the connection string expression for the Garnet server.
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
        builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

        if (PasswordParameter is not null)
        {
            builder.Append($",password={PasswordParameter}");
        }

        return builder.Build();
    }

    private static string ThrowIfNull([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => argument ?? throw new ArgumentNullException(paramName);
}
