// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Utils;

/// <summary>
/// Shared utility methods for telemetry commands.
/// </summary>
internal static class TelemetryCommandHelper
{
    /// <summary>
    /// Gets the Dashboard connection information from either explicit parameters or the backchannel.
    /// </summary>
    /// <param name="dashboardUrl">Explicit dashboard URL (standalone mode).</param>
    /// <param name="apiKey">Explicit API key for authentication.</param>
    /// <param name="auxiliaryBackchannelMonitor">The backchannel monitor to get AppHost connections.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <returns>A tuple containing the endpoint URL and API token, or (null, null) if no connection is available.</returns>
    public static (string? EndpointUrl, string? ApiToken) GetDashboardConnection(
        string? dashboardUrl,
        string? apiKey,
        IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor,
        ILogger logger)
    {
        // If dashboard URL is provided, use standalone mode
        if (!string.IsNullOrEmpty(dashboardUrl))
        {
            logger.LogDebug("Using standalone Dashboard connection: {Url}", dashboardUrl);
            return (dashboardUrl, apiKey);
        }

        // Try to get connection from running AppHost via backchannel
        var connections = auxiliaryBackchannelMonitor.Connections.Values.ToList();

        if (connections.Count == 0)
        {
            logger.LogDebug("No AppHost connections available");
            return (null, null);
        }

        // Get in-scope connections
        var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

        AppHostAuxiliaryBackchannel? connection = null;

        if (inScopeConnections.Count == 1)
        {
            connection = inScopeConnections[0];
        }
        else if (inScopeConnections.Count > 1)
        {
            // Multiple in-scope connections - use the first one but log a warning
            logger.LogWarning("Multiple AppHosts running in scope, using first one");
            connection = inScopeConnections[0];
        }
        else if (connections.Count > 0)
        {
            // No in-scope connections, use the first available
            connection = connections[0];
        }

        if (connection?.McpInfo == null)
        {
            logger.LogDebug("No Dashboard MCP info available from AppHost");
            return (null, null);
        }

        logger.LogDebug("Using AppHost Dashboard connection: {Url}", connection.McpInfo.EndpointUrl);
        return (connection.McpInfo.EndpointUrl, connection.McpInfo.ApiToken);
    }

    /// <summary>
    /// Extracts text content from an MCP CallToolResult.
    /// </summary>
    /// <param name="result">The result from an MCP tool call.</param>
    /// <returns>The text content, or an empty string if no text content is found.</returns>
    public static string GetTextFromResult(CallToolResult result)
    {
        if (result.Content == null || result.Content.Count == 0)
        {
            return string.Empty;
        }

        var textContent = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        if (textContent?.Text != null)
        {
            return textContent.Text;
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts JSON content from an MCP tool result that may contain markdown headers.
    /// </summary>
    /// <param name="result">The raw result string from an MCP tool.</param>
    /// <returns>The JSON portion of the result, or the original string if no JSON is found.</returns>
    public static string ExtractJsonFromResult(string result)
    {
        // The MCP tool response contains markdown headers followed by JSON
        // We need to extract just the JSON part for formatting
        var lines = result.Split('\n');
        var jsonStartIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            {
                jsonStartIndex = i;
                break;
            }
        }

        if (jsonStartIndex >= 0)
        {
            return string.Join('\n', lines.Skip(jsonStartIndex));
        }

        // If no JSON found, return the original
        return result;
    }

    /// <summary>
    /// Escapes a string value for use in JSON.
    /// </summary>
    /// <param name="value">The string value to escape.</param>
    /// <returns>The escaped string.</returns>
    public static string EscapeJsonString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
