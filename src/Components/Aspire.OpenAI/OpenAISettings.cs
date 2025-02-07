// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;

namespace Aspire.OpenAI;

/// <summary>
/// The settings relevant to accessing OpenAI.
/// </summary>
public sealed class OpenAISettings
{
    private const string ConnectionStringEndpoint = "Endpoint";
    private const string ConnectionStringKey = "Key";

    private bool? _disableTracing;
    private bool? _disableMetrics;

    /// <summary>
    /// Gets or sets a <see cref="Uri"/> referencing the OpenAI REST API endpoint.
    /// Leave empty to connect to OpenAI, or set it to use a service using an API compatible with OpenAI.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets a the API key to used to authenticate with the service.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <remarks>
    /// OpenAI telemetry support is experimental, the shape of traces may change in the future without notice.
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
    /// OpenAI telemetry support is experimental, the shape of traces may change in the future without notice.
    /// It can be enabled by setting "OpenAI.Experimental.EnableOpenTelemetry" <see cref="AppContext"/> switch to true.
    /// Or by setting the "OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY" environment variable to "true".
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

    internal void ParseConnectionString(string? connectionString)
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
