// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// Provides shared methods for reading and updating selected MCP configuration files.
/// </summary>
internal static class McpConfigFileHelper
{
    /// <summary>
    /// Reads an existing MCP config file and parses it into a <see cref="JsonObject"/>, or creates a new one if the file doesn't exist.
    /// </summary>
    /// <param name="configFilePath">The path to the MCP configuration file.</param>
    /// <param name="documentOptions">The JSON parsing options for this configuration format.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The parsed <see cref="JsonObject"/> from the file, or a new empty <see cref="JsonObject"/> if the file doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the file contains malformed JSON or its root is not an object.
    /// Malformed JSON exceptions wrap the underlying <see cref="JsonException"/>.
    /// </exception>
    public static async Task<JsonObject> ReadConfigAsync(string configFilePath, JsonDocumentOptions documentOptions, CancellationToken cancellationToken)
    {
        if (!File.Exists(configFilePath))
        {
            return new JsonObject();
        }

        var content = await File.ReadAllTextAsync(configFilePath, cancellationToken);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content, documentOptions: documentOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.MalformedConfigFileError, configFilePath), ex);
        }

        return root as JsonObject
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, ErrorStrings.ConfigurationFileMustBeJsonObject, configFilePath));
    }

    public static bool ConfigureMcpServer(JsonObject config, AgentClient client, AgentConfigurationScope scope)
    {
        if (config[client.McpServersPropertyName] is JsonObject existingServers && existingServers.ContainsKey("aspire"))
        {
            return false;
        }

        if (client.McpSchema is { } schema && !config.ContainsKey("$schema"))
        {
            config["$schema"] = schema;
        }

        if (config[client.McpServersPropertyName] is not JsonObject servers)
        {
            servers = new JsonObject();
            config[client.McpServersPropertyName] = servers;
        }

        var serverConfiguration = scope is AgentConfigurationScope.Global
            ? client.GlobalMcpServerConfiguration ?? client.McpServerConfiguration
            : client.McpServerConfiguration;
        // JsonNodes have single-parent ownership; each destination needs its own copy.
        servers["aspire"] = serverConfiguration.DeepClone();

        return true;
    }

    public static bool UpdateDeprecatedCommand(JsonObject config, string serverContainerKey)
    {
        if (config[serverContainerKey] is not JsonObject servers || servers["aspire"] is not JsonObject aspire)
        {
            return false;
        }

        // Legacy MCP entries use args: ["mcp", "start"], or OpenCode's
        // command: ["aspire", "mcp", "start"]. Only these deprecated forms are repaired;
        // custom commands and version-pinned server definitions are left intact.
        if (aspire["args"] is JsonArray { Count: >= 2 } args &&
            args[0]?.GetValue<string>() == "mcp" && args[1]?.GetValue<string>() == "start")
        {
            aspire["args"] = new JsonArray("agent", "mcp");
        }
        else if (aspire["command"] is JsonArray { Count: >= 3 } command &&
            command[0]?.GetValue<string>() == "aspire" &&
            command[1]?.GetValue<string>() == "mcp" && command[2]?.GetValue<string>() == "start")
        {
            aspire["command"] = new JsonArray("aspire", "agent", "mcp");
        }
        else
        {
            return false;
        }

        return true;
    }
}
