// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using System.Data.Common;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents configuration settings for Azure AI Chat Completions client.
/// </summary>
public sealed class ChatCompletionsClientSettings
{
    /// <summary>
    /// Gets or sets the name of the AI model to use for chat completions.
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Gets or sets the ID of the AI model to use for chat completions.
    /// </summary>
    public string? ModelId { get; set; }

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
    /// - ModelName: The name of the AI model
    /// - ModelId: The ID of the AI model
    /// - Endpoint: The service endpoint URI
    /// - Key: The API key for authentication
    /// </remarks>
    internal void ParseConnectionString(string connectionString)
    {
        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (connectionBuilder.TryGetValue(nameof(ModelName), out var model))
        {
            ModelName = model.ToString();
        }

        if (connectionBuilder.TryGetValue(nameof(ModelId), out var modelId))
        {
            ModelId = modelId.ToString();
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
