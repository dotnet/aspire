// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// Conservative MCP entry creation and selected-target repair of the old "aspire mcp start" command.
/// </summary>
internal static class AspireMcpConfiguration
{
    internal const string ServerName = "aspire";

    public static AgentConfigurationEdit Apply(
        JsonObject root,
        AgentClient client,
        AgentConfigurationScope scope,
        string configFilePath)
    {
        try
        {
            McpConfigFileHelper.ConfigureMcpServer(root, client, scope, configFilePath);
            McpConfigFileHelper.UpdateDeprecatedCommand(root, client.McpServersPropertyName);
            return AgentConfigurationEdit.Applied();
        }
        catch (InvalidOperationException ex)
        {
            throw new AgentConfigurationException(ex.Message);
        }
    }

    public static AgentConfigurationEdit Apply(
        JsonObject root,
        string containerName,
        bool commandArray,
        bool bare = false)
    {
        var servers = bare ? root : AgentConfigurationJson.Object(root, containerName);
        if (!servers.TryGetPropertyValue(ServerName, out var node))
        {
            var client = commandArray ? AgentClient.OpenCode : AgentClient.ClaudeCode;
            servers[ServerName] = client.McpServerConfiguration.DeepClone();
            return AgentConfigurationEdit.Applied();
        }

        if (node is JsonValue disabledValue && disabledValue.TryGetValue<bool>(out var value) && !value)
        {
            return AgentConfigurationEdit.Skipped(AgentCommandStrings.Configuration_Disabled);
        }

        if (node is JsonObject existing)
        {
            McpConfigFileHelper.UpdateDeprecatedCommand(existing);
        }

        return AgentConfigurationEdit.Applied();
    }

    public static AgentConfigurationEdit? CheckPolicy(IEnumerable<JsonObject> settings)
        => CheckPolicy(settings, [], managedAllowlistOnly: false);

    public static AgentConfigurationEdit? CheckExistingEntry(
        JsonObject? servers,
        bool commandArray,
        Func<JsonObject?> getCurrentServers)
    {
        if (servers?.ContainsKey(ServerName) is not true)
        {
            return null;
        }

        if (getCurrentServers()?.ContainsKey(ServerName) is true)
        {
            return null;
        }

        // Inspect the destination only after validating the existing scope. Adding defaults
        // must not shadow its custom arguments, environment, or tool restrictions.
        return servers[ServerName] is not JsonObject server || !IsDefaultEntry(server, commandArray)
            ? AgentConfigurationEdit.Skipped(AgentCommandStrings.Configuration_ExistingMcpCustomization)
            : null;
    }

    public static AgentConfigurationEdit? CheckPolicy(
        IEnumerable<JsonObject> settings,
        IReadOnlyList<JsonObject> managedSettings,
        bool managedAllowlistOnly)
    {
        var allSettings = settings.ToArray();
        foreach (var root in allSettings)
        {
            foreach (var key in new[] { "disabledMcpServers", "disabledMcpjsonServers" })
            {
                if (AgentConfigurationJson.OptionalStrings(root, key)?.Any(value => AgentConfigurationJson.String(value) == ServerName) is true)
                {
                    return AgentConfigurationEdit.Skipped(AgentCommandStrings.Configuration_Disabled);
                }
            }

            if (ReadPolicyEntries(root, "deniedMcpServers") is { } denied &&
                denied.Any(entry => !IsNameOnlyPolicy(entry) || MatchesName(entry)))
            {
                return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_PolicyBlocked);
            }
        }

        // Claude allowlists merge, unless managed policy restricts them to managed sources.
        // Copilot's allowlist is managed-only. Denylists above still apply from every scope.
        // https://code.claude.com/docs/en/settings-reference#allowmanagedmcpserversonly
        var onlyManaged = managedAllowlistOnly ||
            managedSettings.Any(root => AgentConfigurationJson.Boolean(root, "allowManagedMcpServersOnly") is true);
        var allowlists = (onlyManaged ? managedSettings : allSettings)
            .Select(root => ReadPolicyEntries(root, "allowedMcpServers")).OfType<JsonArray>().ToArray();
        if (allowlists.Length > 0 && !allowlists.SelectMany(entries => entries).Any(entry => IsNameOnlyPolicy(entry) && MatchesName(entry)))
        {
            // Command/URL patterns and dynamic matcher forms cannot be safely evaluated
            // without the client's policy engine. Leave them intact and report the limitation.
            return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_PolicyBlocked);
        }

        return null;
    }

    public static bool IsDefaultEntry(JsonObject server, bool commandArray)
    {
        if (server.Any(property => property.Key is not ("type" or "command" or "args" or "env" or "tools" or "enabled" or "disabled")))
        {
            return false;
        }

        var command = commandArray ? AgentConfigurationJson.OptionalStrings(server, "command") : AgentConfigurationJson.OptionalStrings(server, "args");
        string[] expected = commandArray ? ["aspire", "agent", "mcp"] : ["agent", "mcp"];
        if (command is null || !command.Select(AgentConfigurationJson.String).SequenceEqual(expected, StringComparer.Ordinal) ||
            (!commandArray && AgentConfigurationJson.String(server["command"]) != "aspire"))
        {
            return false;
        }

        if (server.TryGetPropertyValue("env", out var environment) &&
            !JsonNode.DeepEquals(environment, new JsonObject { ["DOTNET_ROOT"] = "${DOTNET_ROOT}" }))
        {
            return false;
        }

        return !server.TryGetPropertyValue("tools", out var tools) || JsonNode.DeepEquals(tools, new JsonArray("*"));
    }

    public static bool UsesBareServers(JsonObject root)
        => !root.ContainsKey("mcpServers") && root.Any(property => property.Key != "$schema") &&
            root.Where(property => property.Key != "$schema").All(property =>
                (property.Value is JsonObject server && (server.ContainsKey("command") || server.ContainsKey("url") ||
                    server.ContainsKey("disabled") || server.ContainsKey("enabled"))) ||
                (property.Value is JsonValue value && value.TryGetValue<bool>(out var enabled) && !enabled));

    private static JsonArray? ReadPolicyEntries(JsonObject root, string key)
        => !root.TryGetPropertyValue(key, out var node) ? null : node as JsonArray ?? throw AgentConfigurationJson.Shape(key);

    private static bool IsNameOnlyPolicy(JsonNode? node)
        => node is JsonObject entry && entry.Count == 1 && AgentConfigurationJson.String(entry["serverName"]) is not null;

    private static bool MatchesName(JsonNode? node)
        => node is JsonObject entry && AgentConfigurationJson.String(entry["serverName"]) == ServerName;
}
