// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.Keycloak;

/// <summary>
/// Provides configuration settings for connecting to a Keycloak server.
/// </summary>
public sealed class KeycloakSettings
{
    private const string ConnectionStringEndpoint = nameof(Endpoint);

    /// <summary>
    /// The endpoint URI string of the Keycloak server to connect to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the realm of the Keycloak server to connect to.
    /// The realm is a logical grouping of users, roles, and clients in Keycloak.
    /// </summary>
    public string? Realm { get; set; }

    /// <summary>
    /// Gets or sets the expected audience for any received OpenIdConnect token.
    /// This value is used to validate the 'aud' claim in the token to ensure it's intended for the client application.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets the client ID used for authentication with the Keycloak server.
    /// This ID uniquely identifies the client application within the Keycloak system.
    /// </summary>
    public string? ClientId { get; set; }

    internal void ParseConnectionString(string? connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            Endpoint = uri;
        }
        else
        {
            var connectionBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (connectionBuilder.ContainsKey(ConnectionStringEndpoint) && Uri.TryCreate(connectionBuilder[ConnectionStringEndpoint].ToString(), UriKind.Absolute, out var serviceUri))
            {
                Endpoint = serviceUri;
            }
        }
    }
}
