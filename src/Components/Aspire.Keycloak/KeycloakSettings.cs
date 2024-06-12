// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Keycloak;

/// <summary>
/// Provides configuration settings for connecting to a Keycloak server.
/// </summary>
public sealed class KeycloakSettings
{
    /// <summary>
    /// Gets or sets the realm of the Keycloak server to connect to.
    /// The realm is a logical grouping of users, roles, and clients in Keycloak.
    /// </summary>
    public string? Realm { get; set; }
}
