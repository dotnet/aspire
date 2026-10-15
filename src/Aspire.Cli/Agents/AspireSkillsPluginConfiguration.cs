// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// Registers the official marketplace without invoking plugin CLIs or changing installation state.
/// </summary>
internal static class AspireSkillsPluginConfiguration
{
    internal const string MarketplaceName = "aspire-skills";
    internal const string PluginName = "aspire@aspire-skills";
    internal const string Repository = "microsoft/aspire-skills";
    internal const string RepositoryUrl = "https://github.com/" + Repository;
    private const string V1Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v1/";
    private const string V2Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v2/";

    public static async Task<IReadOnlyList<AgentTargetResult>> ConfigureAsync(
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope, IReadOnlyList<AgentClient> selectedClients,
        CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<AgentTargetResult>();
        var files = new Dictionary<string, List<(AgentClient Client, string Path)>>(AgentPath.Comparer);
        foreach (var client in selectedClients.Distinct())
        {
            var path = client.GetPluginConfigPath(scope, workspaceRoot, executionContext, environment);
            try
            {
                var physicalPath = AgentPath.Resolve(path);
                if (!files.TryGetValue(physicalPath, out var aliases))
                {
                    aliases = [];
                    files.Add(physicalPath, aliases);
                }

                aliases.Add((client, path));
            }
            catch (AgentConfigurationException ex)
            {
                results.Add(new([client], path, AgentConfigurationStatus.Blocked, ex.Message));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                results.Add(new([client], path, AgentConfigurationStatus.Failed,
                    string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.Configuration_ReadWriteFailed, ex.Message)));
            }
        }

        foreach (var (physicalPath, targets) in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clients = targets.Select(target => target.Client).Distinct().ToArray();
            var status = AgentConfigurationStatus.Configured;
            string? message = null;
            try
            {
                // Aliased settings share one publication, but must satisfy every selected
                // client's policy. A fresh reader lets later files observe earlier writes.
                var reader = new AgentConfigurationJson.ReadContext();
                var document = await reader.ReadAsync(physicalPath, cancellationToken);
                var root = document.Root?.DeepClone().AsObject() ?? new JsonObject();
                foreach (var target in targets)
                {
                    if (!AgentPath.Comparer.Equals((await reader.ReadAsync(target.Path, cancellationToken)).Path, physicalPath))
                    {
                        throw new AgentConfigurationException(AgentCommandStrings.Configuration_ConcurrentChange);
                    }

                    var edit = target.Client.UsesSkillsCatalog
                        ? await ApplyOpenCodeCatalogAsync(root, reader, target.Path, workspaceRoot, scope,
                            executionContext, environment, cancellationToken)
                        : Apply(root, await AgentConfigurationJson.ReadSettingsAsync(reader,
                            PluginSettings(target.Client, workspaceRoot, executionContext, environment), cancellationToken));
                    if (edit.Status is not AgentConfigurationStatus.Configured)
                    {
                        status = edit.Status;
                        message = edit.Message;
                        break;
                    }
                }

                if (status is AgentConfigurationStatus.Configured)
                {
                    if (JsonNode.DeepEquals(document.Root ?? new JsonObject(), root))
                    {
                        await reader.ValidateAsync(cancellationToken);
                        status = AgentConfigurationStatus.Unchanged;
                    }
                    else
                    {
                        await reader.SaveAsync(document, root, cancellationToken);
                    }
                }
            }
            catch (AgentConfigurationException ex)
            {
                status = AgentConfigurationStatus.Blocked;
                message = ex.Message;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                status = AgentConfigurationStatus.Failed;
                message = string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.Configuration_ReadWriteFailed, ex.Message);
            }

            results.Add(new(clients, targets[0].Path, status, message));
        }

        return results;
    }

    public static async Task<bool> HasRegistrationAsync(
        DirectoryInfo workspaceRoot, IReadOnlyList<AgentClient> clients,
        CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        // A registration in either visible scope competes with copied skills. Inspect
        // it before bundle/tool acquisition without disabling or migrating anything.
        var reader = new AgentConfigurationJson.ReadContext();
        foreach (var client in clients)
        {
            if (client.UsesSkillsCatalog &&
                environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline &&
                HasAspireCatalog(AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline))))
            {
                return true;
            }

            foreach (var path in PluginSettings(client, workspaceRoot, executionContext, environment))
            {
                var config = await reader.ReadOptionalAsync(path, cancellationToken);
                if (config is not null && (client.UsesSkillsCatalog ? HasAspireCatalog(config) : HasPlugin(config)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasPlugin(JsonObject root)
        => AgentConfigurationJson.OptionalObject(root, "enabledPlugins") is { } plugins &&
            AgentConfigurationJson.Boolean(plugins, PluginName) is true;

    private static AgentConfigurationEdit Apply(JsonObject root, IReadOnlyList<JsonObject> settings)
    {
        JsonObject? selectedSource = null;
        foreach (var config in settings.Append(root))
        {
            var plugins = AgentConfigurationJson.OptionalObject(config, "enabledPlugins");
            if (plugins is not null && AgentConfigurationJson.Boolean(plugins, PluginName) is false)
            {
                return AgentConfigurationEdit.Skipped(AgentCommandStrings.Configuration_Disabled);
            }

            var marketplaces = AgentConfigurationJson.OptionalObject(config, "extraKnownMarketplaces");
            if (marketplaces is not null && marketplaces.TryGetPropertyValue(MarketplaceName, out var marketplaceNode))
            {
                if (marketplaceNode is not JsonObject marketplace ||
                    AgentConfigurationJson.OptionalObject(marketplace, "source") is not { } source)
                {
                    throw AgentConfigurationJson.Shape($"extraKnownMarketplaces.{MarketplaceName}.source");
                }

                if (!IsOfficialSource(source))
                {
                    return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_Conflict);
                }

                // Keep inherited release/commit pins unless the destination declares its own source.
                if (ReferenceEquals(config, root) || source.ContainsKey("ref") || source.ContainsKey("sha") || selectedSource is null)
                {
                    selectedSource = source;
                }
            }
        }

        selectedSource ??= new JsonObject
        {
            ["source"] = "github",
            ["repo"] = Repository
        };
        // Leave marketplace policy enforcement to the client rather than matching its rules.
        if (settings.Append(root).Any(config =>
            config.ContainsKey("strictKnownMarketplaces") || config.ContainsKey("blockedMarketplaces")))
        {
            return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_PolicyBlocked);
        }

        var targetMarketplaces = AgentConfigurationJson.Object(root, "extraKnownMarketplaces");
        if (!targetMarketplaces.ContainsKey(MarketplaceName))
        {
            targetMarketplaces[MarketplaceName] = new JsonObject
            {
                ["source"] = selectedSource.DeepClone()
            };
        }

        // No autoUpdate opt-in and no installedPlugins/cache metadata: registration is not
        // acquisition. Existing entries, including explicit false, were checked above.
        var targetPlugins = AgentConfigurationJson.Object(root, "enabledPlugins");
        if (!targetPlugins.ContainsKey(PluginName))
        {
            targetPlugins[PluginName] = true;
        }

        return AgentConfigurationEdit.Applied();
    }

    private static bool IsOfficialSource(JsonObject source)
    {
        return AgentConfigurationJson.String(source["source"]) switch
        {
            "github" => string.Equals(AgentConfigurationJson.String(source["repo"]), Repository, StringComparison.OrdinalIgnoreCase),
            "git" => AgentConfigurationJson.String(source["url"]) is { } url &&
                (string.Equals(url.TrimEnd('/'), $"https://github.com/{Repository}", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(url.TrimEnd('/'), $"https://github.com/{Repository}.git", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static async Task<AgentConfigurationEdit> ApplyOpenCodeCatalogAsync(
        JsonObject root, AgentConfigurationJson.ReadContext reader, string targetPath,
        DirectoryInfo workspaceRoot, AgentConfigurationScope scope,
        CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        var configs = (await AgentConfigurationJson.ReadSettingsAsync(reader,
            PluginSettings(AgentClient.OpenCode, workspaceRoot, executionContext, environment), cancellationToken)).Append(root).ToList();
        // A higher-precedence file can appear before the reader's first observation.
        // Recheck which destination is active as well as validating the tracked files.
        if (!AgentPath.Comparer.Equals(targetPath,
            AgentClient.OpenCode.GetPluginConfigPath(scope, workspaceRoot, executionContext, environment)))
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

        return ApplyCatalog(root, configs, ResolveOpenCodeSchema(configs));
    }

    private static IEnumerable<string> PluginSettings(
        AgentClient client, DirectoryInfo workspaceRoot, CliExecutionContext context, IEnvironment environment)
    {
        if (client.UsesSkillsCatalog)
        {
            var defaultPaths = OpenCodePaths.GetConfigPaths(client.GlobalPluginDirectory(context, environment));
            var customDirectory = OpenCodePaths.GetOverride("OPENCODE_CONFIG_DIR", context, environment);
            IEnumerable<string> customPaths = customDirectory is null ? [] : OpenCodePaths.GetConfigPaths(customDirectory);
            var globalPaths = environment.GetEnvironmentVariable("OPENCODE_CONFIG") is { Length: > 0 }
                ? new[] { client.GlobalMcpConfigPath(context, environment) }.Concat(customPaths).Concat(defaultPaths)
                : defaultPaths.Concat(customPaths);

            foreach (var path in OpenCodePaths.GetProjectConfigPaths(workspaceRoot).Concat(globalPaths)
                .Concat(OpenCodePaths.GetConfigPaths(client.ManagedPluginDirectory(context, environment))).Distinct(AgentPath.Comparer))
            {
                yield return path;
            }
            yield break;
        }

        yield return Path.Combine(client.GlobalPluginDirectory(context, environment), "settings.json");
        foreach (var path in client.ProjectPluginSettingsFiles)
        {
            yield return Path.Combine(workspaceRoot.FullName, path);
        }

        var managed = client.ManagedPluginDirectory(context, environment);
        yield return Path.Combine(managed, "managed-settings.json");
        var fragments = Path.Combine(managed, "managed-settings.d");
        if (client.UsesManagedSettingsFragments && Directory.Exists(fragments))
        {
            foreach (var path in Directory.EnumerateFiles(fragments, "*.json").Order(StringComparer.Ordinal))
            {
                if (!Path.GetFileName(path).StartsWith('.'))
                {
                    yield return path;
                }
            }
        }
    }

    private static int ResolveOpenCodeSchema(IEnumerable<JsonObject> configs)
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

internal sealed record AgentConfigurationEdit(AgentConfigurationStatus Status, string? Message)
{
    public static AgentConfigurationEdit Applied() => new(AgentConfigurationStatus.Configured, null);
    public static AgentConfigurationEdit Skipped(string message) => new(AgentConfigurationStatus.Skipped, message);
    public static AgentConfigurationEdit Blocked(string message) => new(AgentConfigurationStatus.Blocked, message);
}

internal sealed class AgentConfigurationException(string message) : Exception(message);

internal enum AgentConfigurationStatus
{
    Configured,
    Unchanged,
    Skipped,
    Blocked,
    Failed
}

internal sealed record AgentTargetResult(
    IReadOnlyList<AgentClient> Environments,
    string TargetPath,
    AgentConfigurationStatus Status,
    string? Message);
