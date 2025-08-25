// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.AI.Inference;

/// <summary>
/// Represents configuration settings for Azure AI Chat Completions client.
/// </summary>
public sealed class ChatCompletionsClientSettings : IConnectionStringSettings
{
    private bool? _disableTracing;

    /// <summary>
    /// Gets or sets the connection string used to connect to the AI Foundry account.
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Endpoint"/>, <see cref="DeploymentName"/> and <see cref="TokenCredential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the name of the AI model deployment to use for chat completions.
    /// </summary>
    public string? DeploymentName { get; set; }

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
    /// Azure AI Inference client library ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing
    {
        get { return _disableTracing ??= !GetTracingDefaultValue(); }
        set { _disableTracing = value; }
    }

    // Defaults DisableTracing to false if the experimental switch is set
    // TODO: remove this when ActivitySource support is no longer experimental
    private static bool GetTracingDefaultValue()
    {
        if (AppContext.TryGetSwitch("Azure.Experimental.EnableActivitySource", out var enabled))
        {
            return enabled;
        }

        var envVar = Environment.GetEnvironmentVariable("AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE");
        if (envVar is not null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a connection string and populates the settings properties.
    /// </summary>
    /// <param name="connectionString">The connection string containing configuration values.</param>
    /// <remarks>
    /// The connection string can contain the following keys:
    /// - Deployment: The deployment name (preferred)
    /// - DeploymentId: The deployment ID (legacy, for backward compatibility)
    /// - Model: The model name (used by GitHub Models)
    /// - Endpoint: The service endpoint URI
    /// - Key: The API key for authentication
    /// Note: Only one of Deployment, DeploymentId, or Model should be specified.
    /// </remarks>
    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        var connectionBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        // Check for deployment/model keys and ensure only one is provided
        var deploymentKeys = new List<string>();
        if (connectionBuilder.ContainsKey("Deployment"))
        {
            deploymentKeys.Add("Deployment");
        }
        if (connectionBuilder.ContainsKey("DeploymentId"))
        {
            deploymentKeys.Add("DeploymentId");
        }
        if (connectionBuilder.ContainsKey("Model"))
        {
            deploymentKeys.Add("Model");
        }

        if (deploymentKeys.Count > 1)
        {
            throw new ArgumentException($"The connection string contains multiple deployment/model keys: {string.Join(", ", deploymentKeys)}. Only one of 'Deployment', 'DeploymentId', or 'Model' should be specified.");
        }

        if (connectionBuilder.TryGetValue("Deployment", out var deployment))
        {
            DeploymentName = deployment.ToString();
        }
        else if (connectionBuilder.TryGetValue("DeploymentId", out var deploymentId))
        {
            DeploymentName = deploymentId.ToString();
        }
        else if (connectionBuilder.TryGetValue("Model", out var model))
        {
            DeploymentName = model.ToString();
        }

        // Use the EndpointAIInference key if available, otherwise fallback to Endpoint.
        // This is because Azure AI Inference may require a /models suffix in the endpoint URL while some
        // other client libraries may not. 
        if (connectionBuilder.TryGetValue("EndpointAIInference", out var endpoint) && Uri.TryCreate(endpoint.ToString(), UriKind.Absolute, out var serviceUri))
        {
            Endpoint = serviceUri;
        }
        else if (connectionBuilder.TryGetValue("Endpoint", out endpoint) && Uri.TryCreate(endpoint.ToString(), UriKind.Absolute, out serviceUri))
        {
            Endpoint = serviceUri;
        }

        if (connectionBuilder.TryGetValue("Key", out var key) && !string.IsNullOrWhiteSpace(key.ToString()))
        {
            Key = key.ToString();
        }
    }
}
