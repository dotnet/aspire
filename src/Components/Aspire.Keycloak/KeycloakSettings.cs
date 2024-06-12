// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Keycloak;

/// <summary>
/// Provides configuration settings for connecting to a Keycloak server.
/// </summary>
public sealed class KeycloakSettings
{
    /// <summary>
    /// The endpoint URI string of the Keycloak server to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }
}
