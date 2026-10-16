// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents.OpenCode;

internal static class OpenCodeConfiguration
{
    private const string V1Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v1/";
    private const string V2Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v2/";

    public static async Task<AgentFileEdit> CreateMcpEditAsync(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        CliExecutionContext executionContext, IEnvironment environment,
        CancellationToken cancellationToken)
    {
        var client = AgentClient.OpenCode;
        var path = client.GetMcpConfigPath(scope, workspaceRoot, executionContext, environment);
        Exception? selectionError = null;
        try
        {
            path = await SelectMcpConfigPathAsync(workspaceRoot, scope, executionContext, environment,
                new AgentConfigurationJson.ReadContext(), cancellationToken);
        }
        catch (Exception ex) when (ex is AgentConfigurationException or InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Keep a failed selection in the alias group. Another selected client must
            // not publish changes to the same file after this client rejected its inputs.
            selectionError = ex;
        }

        var edit = McpConfigFileHelper.CreateEdit(client, path, client.Mcp.ServersPropertyName, configureServer: true, scope);

        return edit with
        {
            ApplyAsync = async (root, reader, token) =>
            {
                if (selectionError is not null)
                {
                    ExceptionDispatchInfo.Throw(selectionError);
                }

                // Preselection only establishes the group key. Re-read the owning
                // JSON/JSONC inputs in this publication's snapshot before authorizing it.
                var current = await SelectMcpConfigPathAsync(
                    workspaceRoot, scope, executionContext, environment, reader, token);
                if (!AgentPath.Comparer.Equals(path, current))
                {
                    return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_ConcurrentChange);
                }

                return await edit.ApplyAsync(root, reader, token);
            }
        };
    }

    private static async Task<string> SelectMcpConfigPathAsync(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        CliExecutionContext executionContext, IEnvironment environment,
        AgentConfigurationJson.ReadContext reader, CancellationToken cancellationToken)
    {
        var client = AgentClient.OpenCode;
        var path = client.GetMcpConfigPath(scope, workspaceRoot, executionContext, environment);
        var discoverSiblings = scope is AgentConfigurationScope.Project ||
            environment.GetEnvironmentVariable("OPENCODE_CONFIG") is not { Length: > 0 };
        if (discoverSiblings)
        {
            // Select from tracked observations, including absence, so a new JSONC file
            // cannot take ownership during publication. An unused lower-precedence
            // sibling need not be readable unless MCP inheritance requires it below.
            var candidates = OpenCodePaths.GetConfigPaths(Path.GetDirectoryName(path)!).Reverse().ToArray();
            path = candidates[0];
            foreach (var candidate in candidates)
            {
                if ((await reader.ReadFileAsync(candidate, cancellationToken)).Bytes is not null)
                {
                    path = candidate;
                    break;
                }
            }
        }

        var config = await ReadMcpConfigAsync(path, reader, cancellationToken);
        if (discoverSiblings && Path.GetFileName(path) == "opencode.jsonc")
        {
            var servers = McpConfigFileHelper.GetServerContainer(config, client.Mcp.ServersPropertyName, path);
            // Remote servers use their URL instead of an inherited local command.
            if (servers is not null &&
                servers.TryGetPropertyValue("aspire", out var server) &&
                (server is not JsonObject aspire ||
                    aspire.ContainsKey("command") ||
                    (aspire["type"] is JsonValue type &&
                        type.TryGetValue<string>(out var connectionType) &&
                        connectionType == "remote")))
            {
                return path;
            }

            // JSONC {"mcp":{"aspire":{"enabled":true}}} inherits its command from
            // opencode.json. Repair that owner without shadowing pins or overrides.
            // Explicit OPENCODE_CONFIG files never enable sibling discovery.
            // https://opencode.ai/docs/config/#locations
            var jsonPath = Path.ChangeExtension(path, ".json");
            var jsonConfig = await ReadMcpConfigAsync(jsonPath, reader, cancellationToken);
            if (McpConfigFileHelper.GetServerContainer(jsonConfig, client.Mcp.ServersPropertyName, jsonPath)?.ContainsKey("aspire") is true)
            {
                return jsonPath;
            }
        }

        return path;
    }

    private static async Task<JsonObject> ReadMcpConfigAsync(
        string path,
        AgentConfigurationJson.ReadContext reader, CancellationToken cancellationToken)
        => (await reader.ReadAsync(path,
            bytes => McpConfigFileHelper.ParseConfig(bytes, AgentClient.OpenCode.Mcp.JsonDocumentOptions, path), cancellationToken)).Root ?? new JsonObject();

    public static AgentFileEdit CreateCatalogEdit(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        CliExecutionContext executionContext, IEnvironment environment)
    {
        var path = GetCatalogConfigPath(scope, workspaceRoot, executionContext, environment);

        return new(AgentClient.OpenCode, path, static bytes => AgentConfigurationJson.ParseObject(bytes.Span),
            (root, reader, token) => ApplyCatalogAsync(
                root, reader, path, workspaceRoot, scope, executionContext, environment, token));
    }

    public static async Task<bool> HasCatalogAsync(
        AgentConfigurationJson.ReadContext reader,
        DirectoryInfo workspaceRoot, CliExecutionContext executionContext, IEnvironment environment,
        CancellationToken cancellationToken)
    {
        // Inline settings can activate skills, but are not a writable registration target.
        if (environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline &&
            HasAspireCatalog(AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline))))
        {
            return true;
        }

        foreach (var path in CatalogSettings(workspaceRoot, executionContext, environment))
        {
            if (await reader.ReadOptionalAsync(path, cancellationToken) is { } config && HasAspireCatalog(config))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetCatalogConfigPath(
        AgentConfigurationScope scope, DirectoryInfo workspaceRoot,
        CliExecutionContext executionContext, IEnvironment environment)
        => scope is AgentConfigurationScope.Project
            ? OpenCodePaths.GetProjectConfigPath(workspaceRoot)
            : OpenCodePaths.GetGlobalMcpConfigPath(executionContext, environment);

    private static async Task<AgentConfigurationEdit> ApplyCatalogAsync(
        JsonObject root, AgentConfigurationJson.ReadContext reader, string targetPath,
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        var configs = (await AgentConfigurationJson.ReadSettingsAsync(reader,
            CatalogSettings(workspaceRoot, executionContext, environment), cancellationToken)).Append(root).ToList();
        // A higher-precedence file can appear before the reader's first observation.
        // Recheck the active destination as well as validating the tracked inputs.
        if (!AgentPath.Comparer.Equals(targetPath,
            GetCatalogConfigPath(scope, workspaceRoot, executionContext, environment)))
        {
            return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_ConcurrentChange);
        }

        if (environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline)
        {
            var inlineConfig = AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline));
            configs.Add(inlineConfig);
            if (inlineConfig.ContainsKey("skills"))
            {
                return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_PolicyBlocked);
            }
        }

        return ApplyCatalog(root, configs, ResolveSchema(configs));
    }

    private static IEnumerable<string> CatalogSettings(
        DirectoryInfo workspaceRoot, CliExecutionContext context, IEnvironment environment)
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

    private static int ResolveSchema(IEnumerable<JsonObject> configs)
    {
        var versions = new HashSet<int>();
        foreach (var config in configs)
        {
            if (config.TryGetPropertyValue("skills", out var skills))
            {
                var schema = skills switch
                {
                    JsonObject v1 => V1Skills(v1),
                    JsonArray v2 when v2.All(value => AgentConfigurationJson.String(value) is not null) => 2,
                    _ => throw AgentConfigurationJson.Shape("skills")
                };
                var sources = schema == 1 ? AgentConfigurationJson.OptionalStrings(skills!.AsObject(), "urls") : skills!.AsArray();
                if (sources?.Any(value => CatalogSchema(AgentConfigurationJson.String(value)) is { } declared && declared != schema) is true)
                {
                    throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
                }
                versions.Add(schema);
            }

            if (AgentConfigurationJson.OptionalObject(config, "mcp") is { Count: > 0 } mcp)
            {
                var schema = mcp.ContainsKey("servers") || mcp["timeout"] is JsonObject timeout &&
                    timeout.All(property => property.Key is "startup" or "catalog" or "execution") ? 2 : 1;
                if (schema == 2 && mcp.Any(property => property.Key is not ("servers" or "timeout")))
                {
                    throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
                }
                versions.Add(schema);
            }
        }

        if (versions.Count > 1)
        {
            throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
        }
        // Both schemas use the same $schema URL. An undetected client with no other
        // evidence uses stable V1; never convert existing settings to a different schema.
        return versions.SingleOrDefault(1);

        static int V1Skills(JsonObject skills)
        {
            AgentConfigurationJson.OptionalStrings(skills, "paths");
            return 1;
        }
    }

    private static AgentConfigurationEdit ApplyCatalog(JsonObject root, IReadOnlyList<JsonObject> configs, int schema)
    {
        var preferred = schema == 1 ? V1Catalog : V2Catalog;
        foreach (var config in configs)
        {
            var sources = schema == 1
                ? AgentConfigurationJson.OptionalObject(config, "skills") is { } skills ? AgentConfigurationJson.OptionalStrings(skills, "urls") : null
                : config["skills"] as JsonArray;
            if (sources?.Select(AgentConfigurationJson.String).FirstOrDefault(value => CatalogSchema(value) == schema) is { } existing)
            {
                preferred = existing;
                if (!existing.Contains("/main/opencode/", StringComparison.Ordinal))
                {
                    break;
                }
            }
        }

        var targetObject = schema == 1 ? AgentConfigurationJson.Object(root, "skills") : root;
        var key = schema == 1 ? "urls" : "skills";
        var target = AgentConfigurationJson.OptionalStrings(targetObject, key) ?? new JsonArray();
        if (!targetObject.ContainsKey(key))
        {
            targetObject[key] = target;
        }
        if (!target.Any(value => CatalogSchema(AgentConfigurationJson.String(value)) == schema))
        {
            target.Add((JsonNode?)preferred);
        }

        return AgentConfigurationEdit.Applied();
    }

    private static bool HasAspireCatalog(JsonObject root)
    {
        if (!root.TryGetPropertyValue("skills", out var skills))
        {
            return false;
        }

        JsonArray? sources;
        if (skills is JsonObject v1)
        {
            AgentConfigurationJson.OptionalStrings(v1, "paths");
            sources = AgentConfigurationJson.OptionalStrings(v1, "urls");
        }
        else
        {
            sources = skills is JsonArray array && array.All(value => AgentConfigurationJson.String(value) is not null)
                ? array : throw AgentConfigurationJson.Shape("skills");
        }

        return sources?.Any(value => CatalogSchema(AgentConfigurationJson.String(value)) is not null) is true;
    }

    private static int? CatalogSchema(string? source)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
            !uri.AbsolutePath.StartsWith("/microsoft/aspire-skills/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        return path.EndsWith("/opencode/v1", StringComparison.Ordinal) ? 1
            : path.EndsWith("/opencode/v2", StringComparison.Ordinal) ? 2 : null;
    }
}
