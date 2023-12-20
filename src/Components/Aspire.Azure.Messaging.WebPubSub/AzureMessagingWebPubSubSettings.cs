// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Messaging.WebPubSub;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Web PubSub.
/// </summary>
public sealed class AzureMessagingWebPubSubSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the hub used to connect to the Web PubSub service.
    /// </summary>
    public string? Hub { get; set; }

    /// <summary>
    /// Gets or sets the connection string used to connect to the Web PubSub service.
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Endpoint"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the endpoint of the Web PubSub service.
    /// This is likely to be similar to "https://{name}.webpubsub.azure.com/".
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Web PubSub endpoint.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.</para>
    /// <para>Disabled by default.</para>
    /// </summary>
    /// <remarks>
    /// ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    public bool Tracing { get; set; }

    public void ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                Endpoint = uri;
            }
            else
            {
                ConnectionString = connectionString;
            }
        }
    }
}
