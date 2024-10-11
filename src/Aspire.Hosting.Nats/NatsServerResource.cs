// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a NATS server container.
/// </summary>

public class NatsServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    internal const string PrimaryNatsSchemeName = "nats";
    private const string DefaultUserName = "nats";

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the NATS server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the NATS server password.</param>
    public NatsServerResource(string name, ParameterResource? userName, ParameterResource password) : base(ThrowIfNull(name))
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the NATS server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// user name for the NATS server
    /// </summary>
    public ParameterResource? UserNameParameter { get; set; }

    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"{DefaultUserName}");

    /// <summary>
    /// password for the NATS server
    /// </summary>
    public ParameterResource PasswordParameter { get; set; }

    /// <summary>
    /// Gets the connection string expression for the NATS server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"{PrimaryNatsSchemeName}://{UserNameReference}:{PasswordParameter}@{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}");

    private static string ThrowIfNull([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => argument ?? throw new ArgumentNullException(paramName);
}
