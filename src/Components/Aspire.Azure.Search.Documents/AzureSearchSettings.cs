// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;
using Azure;
using Azure.Core;

namespace Aspire.Azure.Search.Documents;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Search.
/// </summary>
public sealed class AzureSearchSettings : IConnectionStringSettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringKey = "Key";

    /// <summary>
    /// Gets or sets a <see cref="Uri"/> referencing the Azure Search endpoint.
    /// This is likely to be similar to "https://{search_service}.search.windows.net".
    /// </summary>
    /// <remarks>
    /// Must not contain shared access signature.
    /// Used along with <see cref="Credential"/> or <see cref="Key"/> to establish the connection.
    /// </remarks>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure Search resource.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the key to use to authenticate to the Azure Search endpoint.
    /// </summary>
    /// <remarks>
    /// When defined it will use an <see cref="AzureKeyCredential"/> instance instead of <see cref="Credential"/>.
    /// </remarks>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the Azure Search health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Tracing { get; set; } = true;

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
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

            if (connectionBuilder.ContainsKey(ConnectionStringKey))
            {
                Key = connectionBuilder[ConnectionStringKey].ToString();
            }
        }
    }
}
