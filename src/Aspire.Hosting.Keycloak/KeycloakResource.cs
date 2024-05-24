// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Keycloak resource.
/// </summary>
public sealed class KeycloakResource : ContainerResource, IResourceWithServiceDiscovery
{
    private const string DefaultAdmin = "admin";

    /// <param name="name">The name of the resource.</param>
    /// <param name="admin">A parameter that contains the Keycloak admin, or <see langword="null"/> to use a default value.</param>
    /// <param name="adminPassword">A parameter that contains the Keycloak admin password.</param>
    public KeycloakResource(string name, ParameterResource? admin, ParameterResource adminPassword) : base(name)
    {
        ArgumentNullException.ThrowIfNull(adminPassword);

        AdminParameter = admin;
        AdminPasswordParameter = adminPassword;
    }

    /// <summary>
    /// Gets the parameter that contains the Keycloak admin.
    /// </summary>
    public ParameterResource? AdminParameter { get; }

    internal ReferenceExpression AdminReference =>
        AdminParameter is not null ?
            ReferenceExpression.Create($"{AdminParameter}") :
            ReferenceExpression.Create($"{DefaultAdmin}");

    /// <summary>
    /// Gets the parameter that contains the Keycloak admin password.
    /// </summary>
    public ParameterResource AdminPasswordParameter { get; }
}