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
    /// Gets or sets the name of the hub used.
    /// </summary>
    public string? HubName { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Web PubSub health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                Endpoint = uri;
                return;
            }

            var connectionBuilder = new StableConnectionStringBuilder(connectionString);

            if (connectionBuilder.TryGetValue("Hub", out var entityPath))
            {
                HubName = entityPath?.ToString();
                connectionBuilder.Remove("Hub");
            }

            if (connectionBuilder.Count() == 1 &&
                connectionBuilder.TryGetValue("Endpoint", out var endpoint))
            {
                Endpoint = new Uri(endpoint.ToString()!);
                return;
            }

            ConnectionString = connectionBuilder.ConnectionString;
        }
    }
}
