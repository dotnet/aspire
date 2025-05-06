// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;
using Azure.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents configuration settings for Azure AI Chat Completions client.
/// </summary>
public sealed class ChatCompletionsClientSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the AI Foundry account.
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Endpoint"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the ID of the AI model deployment to use for chat completions.
    /// </summary>
    public string? DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URI for the Azure AI service.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the token credential used for Azure authentication.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }

    /// <summary>
    /// Gets or sets the API key used for authentication with the Azure AI service.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <remarks>
    /// /// Azure AI Inference telemetry follows the pattern of Azure SDKs Diagnostics.
    /// </remarks>
    public bool DisableMetrics { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <remarks>
    /// Azure AI Inference telemetry follows the pattern of Azure SDKs Diagnostics.
    /// </remarks>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Parses a connection string and populates the settings properties.
    /// </summary>
    /// <param name="connectionString">The connection string containing configuration values.</param>
    /// <remarks>
    /// The connection string can contain the following keys:
    /// - DeploymentId: The ID of the AI model
    /// - Endpoint: The service endpoint URI
    /// - Key: The API key for authentication
    /// </remarks>
    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (connectionBuilder.TryGetValue(nameof(DeploymentId), out var modelId))
        {
            DeploymentId = modelId.ToString();
        }

        if (connectionBuilder.TryGetValue(nameof(Endpoint), out var endpoint) && Uri.TryCreate(endpoint.ToString(), UriKind.Absolute, out var serviceUri))
        {
            Endpoint = serviceUri;
        }

        if (connectionBuilder.TryGetValue(nameof(Key), out var key) && !string.IsNullOrWhiteSpace(key.ToString()))
        {
            Key = key.ToString();
        }
    }
}
