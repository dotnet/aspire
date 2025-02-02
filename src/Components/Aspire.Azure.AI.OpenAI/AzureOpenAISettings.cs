// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
using System.Data.Common;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.AI.OpenAI;

/// <summary>
/// The settings relevant to accessing Azure OpenAI or OpenAI.
/// </summary>
public sealed class AzureOpenAISettings : IConnectionStringSettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringKey = "Key";

    private bool? _disableTracing;
    private bool? _disableMetrics;

    /// <summary>
    /// Gets or sets a <see cref="Uri"/> referencing the Azure OpenAI endpoint.
    /// This is likely to be similar to "https://{account_name}.openai.azure.com".
    /// </summary>
    /// <remarks>
    /// Leave empty and provide a <see cref="Key"/> value to use a non-Azure OpenAI inference endpoint.
    /// Used along with <see cref="Credential"/> or <see cref="Key"/> to establish the connection.
    /// </remarks>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure OpenAI resource.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the key to use to authenticate to the Azure OpenAI endpoint.
    /// </summary>
    /// <remarks>
    /// When defined it will use an <see cref="ApiKeyCredential"/> instance instead of <see cref="Credential"/>.
    /// </remarks>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <remarks>
    /// Azure AI OpenAI telemetry support is experimental, the shape of traces may change in the future without notice.
    /// It can be enabled by setting "OpenAI.Experimental.EnableOpenTelemetry" <see cref="AppContext"/> switch to true.
    /// Or by setting "OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY" environment variable to "true".
    /// </remarks>
    public bool DisableMetrics
    {
        get { return _disableMetrics ??= !GetTelemetryDefaultValue(); }
        set { _disableMetrics = value; }
    }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <remarks>
    /// Azure AI OpenAI telemetry support is experimental, the shape of traces may change in the future without notice.
    /// It can be enabled by setting "OpenAI.Experimental.EnableOpenTelemetry" <see cref="AppContext"/> switch to true.
    /// Or by setting "OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY" environment variable to "true".
    /// </remarks>
    public bool DisableTracing
    {
        get { return _disableTracing ??= !GetTelemetryDefaultValue(); }
        set { _disableTracing = value; }
    }

    // TODO: remove this when telemetry support is no longer experimental
    private static bool GetTelemetryDefaultValue()
    {
        if (AppContext.TryGetSwitch("OpenAI.Experimental.EnableOpenTelemetry", out var enabled))
        {
            return enabled;
        }

        var envVar = Environment.GetEnvironmentVariable("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY");
        if (envVar is not null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
        {
            return true;
        }

        return false;
    }

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
