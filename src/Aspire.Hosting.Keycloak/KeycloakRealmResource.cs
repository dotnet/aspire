// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Keycloak;

/// <summary>
/// Represents a Keycloak Realm resource.
/// </summary>
/// <param name="name">The name of the realm resource.</param>
/// <param name="realmName">The name of the realm.</param>
/// <param name="parent">The Keycloak server resource associated with this database.</param>
public sealed class KeycloakRealmResource(string name, string realmName, KeycloakResource parent) : Resource(name), IResourceWithParent<KeycloakResource>, IResourceWithConnectionString
{
    private EndpointReference? _parentEndpoint;
    private EndpointReference ParentEndpoint => _parentEndpoint ??= new(Parent, "http");

    /// <inheritdoc/>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{ParentEndpoint.Property(EndpointProperty.Url)}/realms/{RealmName}/");

    /// <summary>
    /// Gets the issuer expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression IssuerExpression => ReferenceExpression.Create(
        $"{ParentEndpoint.Property(EndpointProperty.Url)}/realms/{RealmName}");

    /// <summary>
    /// Gets or sets the metadata address for the Keycloak realm.
    /// </summary>
    public string MetadataAddress { get; set; } = ".well-known/openid-configuration";

    /// <summary>
    /// Gets the metadata address expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression MetadataAddressExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{MetadataAddress}");

    /// <summary>
    /// Gets or sets the 'authorization_endpoint' for the Keycloak realm.
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = "protocol/openid-connect/auth";

    /// <summary>
    /// Gets the 'authorization_endpoint' expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression AuthorizationEndpointExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{AuthorizationEndpoint}");

    /// <summary>
    /// Gets or sets the 'token_endpoint' for the Keycloak realm.
    /// </summary>
    public string TokenEndpoint { get; set; } = "protocol/openid-connect/token";

    /// <summary>
    /// Gets the 'token_endpoint' expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression TokenEndpointExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{TokenEndpoint}");

    /// <summary>
    /// Gets or sets the 'introspection_endpoint' for the Keycloak realm.
    /// </summary>
    public string IntrospectionEndpoint { get; set; } = "protocol/openid-connect/token/introspect";

    /// <summary>
    /// Gets the 'introspection_endpoint' expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression IntrospectionEndpointExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{IntrospectionEndpoint}");

    /// <summary>
    /// Gets or sets 'user_info_endpoint' for the Keycloak realm.
    /// </summary>
    public string UserInfoEndpoint { get; set; } = "protocol/openid-connect/userinfo";

    /// <summary>
    /// Gets 'user_info_endpoint' expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression UserInfoEndpointExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{UserInfoEndpoint}");

    /// <summary>
    /// Gets or sets the 'end_session_endpoint' for the Keycloak realm.
    /// </summary>
    public string EndSessionEndpoint { get; set; } = "protocol/openid-connect/logout";

    /// <summary>
    /// Gets the 'end_session_endpoint' expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression EndSessionEndpointExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{EndSessionEndpoint}");

    /// <summary>
    /// Gets or sets the 'registration_endpoint' for the Keycloak realm.
    /// </summary>
    public string RegistrationEndpoint { get; set; } = "clients-registrations/openid-connect";

    /// <summary>
    /// Gets the 'registration_endpoint' expression for the Keycloak realm.
    /// </summary>
    public ReferenceExpression RegistrationEndpointExpression => ReferenceExpression.Create($"{ConnectionStringExpression}{RegistrationEndpoint}");

    /// <inheritdoc/>
    public KeycloakResource Parent { get; } = parent;

    /// <summary>
    /// Gets the name of the realm.
    /// </summary>
    public string RealmName { get; } = realmName;
}
