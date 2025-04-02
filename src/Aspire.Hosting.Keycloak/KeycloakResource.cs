// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Keycloak resource.
/// <param name="name">The name of the resource.</param>
/// <param name="admin">A parameter that contains the Keycloak admin, or <see langword="null"/> to use a default value.</param>
/// <param name="adminPassword">A parameter that contains the Keycloak admin password.</param>
/// </summary>
public sealed class KeycloakResource(string name, ParameterResource? admin, ParameterResource adminPassword)
    : ContainerResource(name), IResourceWithServiceDiscovery
{
    private const string DefaultAdmin = "admin";
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Gets the parameter that contains the Keycloak admin.
    /// </summary>
    public ParameterResource? AdminUserNameParameter { get; } = admin;

    internal ReferenceExpression AdminReference =>
        AdminUserNameParameter is not null ?
            ReferenceExpression.Create($"{AdminUserNameParameter}") :
            ReferenceExpression.Create($"{DefaultAdmin}");

    /// <summary>
    /// Gets the parameter that contains the Keycloak admin password.
    /// </summary>
    public ParameterResource AdminPasswordParameter { get; } = adminPassword ?? throw new ArgumentNullException(nameof(adminPassword));
}
