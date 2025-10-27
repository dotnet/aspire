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
    public bool DisableMetrics { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Gets or sets a boolean value indicating whether potentially sensitive information should be included in telemetry.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if potentially sensitive information should be included in telemetry;
    /// <see langword="false"/> if telemetry shouldn't include raw inputs and outputs.
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// By default, telemetry includes metadata, such as token counts, but not raw inputs
    /// and outputs, such as message content, function call arguments, and function call results.
    /// </remarks>
    public bool EnableSensitiveTelemetryData { get; set; }

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
