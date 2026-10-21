// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Agents.OpenCode;

internal sealed class OpenCodeAgentClient(CliExecutionContext context, IEnvironment environment)
    : IAgentClient
{
    public const string ClientId = "opencode";

    private static readonly McpConfigFileHelper.Configuration s_mcp = new()
    {
        ServersPropertyName = "mcp",
        JsonDocumentOptions = ConfigurationHelper.ParseOptions,
        Schema = "https://opencode.ai/config.json",
        ServerConfiguration = new JsonObject
        {
            ["type"] = "local",
            ["command"] = new JsonArray("aspire", "agent", "mcp"),
            ["enabled"] = true
        }
    };

    private static readonly McpConfigFileHelper.Configuration s_nativeMcp = s_mcp with
    {
        ServersPropertyName = "servers",
        Schema = null,
        ServerConfiguration = new JsonObject
        {
            ["type"] = "local",
            ["command"] = new JsonArray("aspire", "agent", "mcp"),
            ["disabled"] = false
        }
    };

    public string Id => ClientId;
    public string DisplayName { get; } = AgentCommandStrings.Environment_OpenCode;
    public AgentEnvironment Environments => AgentEnvironment.OpenCode;

    public string GetSkillBaseDirectory(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => Path.Combine(scope is AgentConfigurationScope.Project ? workspaceRoot.FullName : context.HomeDirectory.FullName,
            AgentPath.UniversalSkillsDirectory);

    public IEnumerable<string> GetLocalSkillDirectories(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
    {
        // https://opencode.ai/docs/skills/
        var root = scope is AgentConfigurationScope.Project ? workspaceRoot.FullName : context.HomeDirectory.FullName;
        yield return GetSkillBaseDirectory(scope, workspaceRoot);
        yield return Path.Combine(root, ".claude", "skills");
        var openCodeRoot = scope is AgentConfigurationScope.Project ? Path.Combine(root, ".opencode")
            : OpenCodePaths.GetConfigDirectory(context, environment);
        yield return Path.Combine(openCodeRoot, "skill");
        yield return Path.Combine(openCodeRoot, "skills");
        if (scope is AgentConfigurationScope.Global &&
            OpenCodePaths.GetOverride("OPENCODE_CONFIG_DIR", context, environment) is { } custom)
        {
            yield return Path.Combine(custom, "skill");
            yield return Path.Combine(custom, "skills");
        }
    }

    public string GetMcpConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => scope is AgentConfigurationScope.Project ? OpenCodePaths.GetMcpConfigPath(workspaceRoot)
            : OpenCodePaths.GetGlobalMcpConfigPath(context, environment);

    public IEnumerable<(string Path, bool ConfigureServer)> GetMcpConfigPaths(
        AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => [(GetMcpConfigPath(scope, workspaceRoot), true)];

    public async Task<(JsonObject Config, string Path)> ReadMcpConfigAsync(
        string configFilePath, AgentConfigurationScope scope, CancellationToken cancellationToken)
    {
        var config = await McpConfigFileHelper.ReadConfigAsync(configFilePath, s_mcp.JsonDocumentOptions, cancellationToken);
        var discoverSiblings = scope is AgentConfigurationScope.Project ||
            environment.GetEnvironmentVariable("OPENCODE_CONFIG") is not { Length: > 0 };
        if (discoverSiblings && Path.GetFileName(configFilePath) == "opencode.jsonc")
        {
            var (servers, native) = GetMcpServerContainer(config, configFilePath);
            // Remote servers use their URL instead of an inherited local command.
            if (servers is not null &&
                servers.TryGetPropertyValue("aspire", out var server) &&
                (native ||
                    server is not JsonObject aspire ||
                    aspire.ContainsKey("command") ||
                    (aspire["type"] is JsonValue type &&
                        type.TryGetValue<string>(out var connectionType) &&
                        connectionType == "remote")))
            {
                return (config, configFilePath);
            }

            // JSONC {"mcp":{"aspire":{"enabled":true}}} inherits its command from
            // opencode.json. Repair that owner without shadowing pins or overrides.
            // Explicit OPENCODE_CONFIG files never enable sibling discovery.
            // https://opencode.ai/docs/config/#locations
            var jsonPath = Path.ChangeExtension(configFilePath, ".json");
            var jsonConfig = await McpConfigFileHelper.ReadConfigAsync(jsonPath, s_mcp.JsonDocumentOptions, cancellationToken);
            if (GetMcpServerContainer(jsonConfig, jsonPath).Servers?.ContainsKey("aspire") is true)
            {
                return (jsonConfig, jsonPath);
            }
        }

        return (config, configFilePath);
    }

    public (bool Repaired, bool Added) UpdateMcpConfig(
        JsonObject config, string configFilePath, bool configureServer, AgentConfigurationScope scope)
    {
        var (_, native) = GetMcpServerContainer(config, configFilePath);
        var root = native ? config["mcp"]!.AsObject() : config;
        var configuration = native ? s_nativeMcp : s_mcp;
        var repaired = McpConfigFileHelper.UpdateDeprecatedCommand(root, configuration.ServersPropertyName);
        var added = configureServer && McpConfigFileHelper.ConfigureMcpServer(root, configuration, scope, configFilePath);
        if (added && native && !config.ContainsKey("$schema"))
        {
            config["$schema"] = s_mcp.Schema;
        }

        return (repaired, added);
    }

    public override string ToString() => Id;

    private static (JsonObject? Servers, bool Native) GetMcpServerContainer(JsonObject root, string path)
    {
        var mcp = McpConfigFileHelper.GetServerContainer(root, s_mcp.ServersPropertyName, path);
        if (mcp is not null && mcp.TryGetPropertyValue("servers", out var servers))
        {
            // {"mcp":{"servers":{"type":"local","command":[...]}}} names a V1 server.
            // Otherwise V2's mcp.servers.aspire wins over mcp.aspire within this file;
            // file precedence is handled separately by ReadMcpConfigAsync.
            // https://github.com/anomalyco/opencode/blob/52a10f573f1da5103c85990da9341e3645a39365/packages/core/src/config/normalize.ts
            var legacyServer = servers is JsonObject server &&
                (AgentConfigurationJson.String(server["type"]) is "local" or "remote" ||
                    server["enabled"] is JsonValue enabled && enabled.TryGetValue<bool>(out _));
            if (!legacyServer)
            {
                if (servers is not JsonObject native)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        AgentCommandStrings.McpServerContainerMustBeJsonObject, "mcp.servers", path));
                }
                if (native.ContainsKey("aspire") || !mcp.ContainsKey("aspire"))
                {
                    return (native, true);
                }
            }
        }

        return (mcp, false);
    }

    public string GetIntegrationConfigPath(AgentConfigurationScope scope, DirectoryInfo workspaceRoot)
        => scope is AgentConfigurationScope.Project
            ? OpenCodePaths.GetProjectConfigPath(workspaceRoot)
            : OpenCodePaths.GetGlobalMcpConfigPath(context, environment);

    public async Task<bool> HasIntegrationRegistrationAsync(DirectoryInfo workspaceRoot, CancellationToken cancellationToken)
    {
        // Inline settings can activate skills, but are not a writable registration target.
        if (environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline &&
            AspireSkillsConfiguration.HasOpenCodeCatalog(AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline))))
        {
            return true;
        }

        foreach (var path in CatalogSettings(workspaceRoot))
        {
            if (await AgentConfigurationJson.ReadAsync(path, cancellationToken) is { } config && AspireSkillsConfiguration.HasOpenCodeCatalog(config))
            {
                return true;
            }
        }

        return false;
    }

    public async Task UpdateIntegrationConfigAsync(
        JsonObject config, DirectoryInfo workspaceRoot, AgentConfigurationScope scope, CancellationToken cancellationToken)
    {
        var configs = await AgentConfigurationJson.ReadSettingsAsync(
            CatalogSettings(workspaceRoot), GetIntegrationConfigPath(scope, workspaceRoot), cancellationToken);
        if (environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline)
        {
            var inlineConfig = AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline));
            if (inlineConfig.ContainsKey("skills"))
            {
                throw AspireSkillsConfiguration.ExistingConfiguration("OPENCODE_CONFIG_CONTENT.skills");
            }
        }

        AspireSkillsConfiguration.ApplyOpenCodeCatalog(config, configs.Append(config));
    }

    private IEnumerable<string> CatalogSettings(DirectoryInfo workspaceRoot)
    {
        var defaultPaths = OpenCodePaths.GetConfigPaths(OpenCodePaths.GetConfigDirectory(context, environment));
        var customDirectory = OpenCodePaths.GetOverride("OPENCODE_CONFIG_DIR", context, environment);
        IEnumerable<string> customPaths = customDirectory is null ? [] : OpenCodePaths.GetConfigPaths(customDirectory);
        var globalPaths = environment.GetEnvironmentVariable("OPENCODE_CONFIG") is { Length: > 0 }
            ? new[] { OpenCodePaths.GetGlobalMcpConfigPath(context, environment) }.Concat(customPaths).Concat(defaultPaths)
            : defaultPaths.Concat(customPaths);

        return OpenCodePaths.GetProjectConfigPaths(workspaceRoot).Concat(globalPaths)
            .Concat(OpenCodePaths.GetConfigPaths(OpenCodePaths.GetManagedSettingsDirectory(context, environment))).Distinct(AgentPath.Comparer);
    }
}
