// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// Supplies offline native registration edits without owning agent selection, tools, MCP, or hooks.
/// </summary>
internal static class NativeAgentPluginConfiguration
{
    internal const string V1Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v1/";
    internal const string V2Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v2/";
    public static async Task<IReadOnlyList<AgentTargetResult>> ConfigureAsync(
        NativeAgentPluginRequest request, CliExecutionContext executionContext, IEnvironment environment,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = new List<AgentTargetResult>();
        var files = new Dictionary<string, List<(AgentClient Client, string Path, IReadOnlyList<string> Candidates)>>(AgentPath.Comparer);
        foreach (var target in GetTargets(request, executionContext, environment))
        {
            try
            {
                var physicalPath = AgentPath.Resolve(target.Path);
                if (!files.TryGetValue(physicalPath, out var aliases))
                {
                    aliases = [];
                    files.Add(physicalPath, aliases);
                }

                aliases.Add(target);
            }
            catch (AgentConfigurationException ex)
            {
                results.Add(new([target.Client], target.Path, request.Scope, AgentConfigurationStatus.Blocked, ex.Message));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                results.Add(new([target.Client], target.Path, request.Scope, AgentConfigurationStatus.Failed,
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
                        ? await ApplyOpenCodeCatalogAsync(root, reader, target.Candidates, target.Path, request,
                            executionContext, environment, cancellationToken)
                        : AspireSkillsPluginConfiguration.Apply(root,
                            await AgentConfigurationJson.ReadSettingsAsync(reader,
                                PluginSettings(target.Client, request, executionContext, environment), cancellationToken));
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

            results.Add(new(clients, targets[0].Path, request.Scope, status, message));
        }

        return results;
    }

    private static IEnumerable<(AgentClient Client, string Path, IReadOnlyList<string> Candidates)> GetTargets(
        NativeAgentPluginRequest request, CliExecutionContext executionContext, IEnvironment environment)
    {
        foreach (var client in request.Clients.Distinct())
        {
            if (client.UsesSkillsCatalog)
            {
                if (request.Scope is AgentConfigurationScope.Global)
                {
                    // Skills and MCP share the client-declared config destination. Other
                    // merged config layers supply read-only evidence, not extra write targets.
                    var path = client.GlobalMcpConfigPath(executionContext, environment);
                    yield return (client, path, AgentPath.GetOverride("OPENCODE_CONFIG", executionContext, environment) is not null
                        ? [path] : ConfigFiles(Path.GetDirectoryName(path)!).ToArray());
                }
                else
                {
                    var location = OpenCodeLocations(request, executionContext, environment)
                        .Single(location => location.Scope is AgentConfigurationScope.Project);
                    yield return (client, location.Path, location.Candidates);
                }
            }
            else
            {
                var directory = request.Scope is AgentConfigurationScope.Global
                    ? client.GlobalPluginDirectory(executionContext, environment)
                    : Path.Combine(AgentPath.RequireWorkspaceRoot(request.WorkspaceRoot).FullName, client.ProjectPluginDirectory);
                yield return (client, Path.Combine(directory, "settings.json"), []);
            }
        }
    }

    private static async Task<AgentConfigurationEdit> ApplyOpenCodeCatalogAsync(
        JsonObject root, AgentConfigurationJson.ReadContext reader, IReadOnlyList<string> candidates, string targetPath,
        NativeAgentPluginRequest request, CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        var documents = new Dictionary<string, JsonObject>(AgentPath.Comparer);
        foreach (var path in PluginSettings(AgentClient.OpenCode, request, executionContext, environment))
        {
            if (await reader.ReadOptionalAsync(path, cancellationToken) is { } config)
            {
                documents[path] = config;
            }
        }

        var active = candidates.LastOrDefault(documents.ContainsKey) ?? targetPath;
        if (!AgentPath.Comparer.Equals(active, targetPath))
        {
            return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_ConcurrentChange);
        }

        var configs = documents.Values.Append(root).ToList();
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

    public static async Task<string?> CheckSkillsModeAsync(
        AspireSkillsMode mode, NativeAgentPluginRequest request,
        CliExecutionContext executionContext, IEnvironment environment, CancellationToken cancellationToken)
    {
        if (mode is AspireSkillsMode.Native)
        {
            var local = await LocalAspireSkills.FindAsync(executionContext.WorkingDirectory, request.WorkspaceRoot,
                request.Clients, executionContext, environment, cancellationToken);
            if (local.Errors.Count > 0 || local.Files.Count > 0)
            {
                return string.Join(System.Environment.NewLine, local.Errors.Concat(local.Files.Count > 0
                    ? [string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_LocalSkillsConflict,
                        string.Join(System.Environment.NewLine, local.Files.Select(file => file.Path)))]
                    : []));
            }
        }
        else if (mode is AspireSkillsMode.Bundled)
        {
            // A registration in either visible scope competes with copied skills. Inspect
            // it before bundle/tool acquisition without disabling or migrating anything.
            var reader = new AgentConfigurationJson.ReadContext();
            foreach (var client in request.Clients)
            {
                if (client.UsesSkillsCatalog &&
                    environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline &&
                    HasAspireCatalog(AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline))))
                {
                    return AgentCommandStrings.InitCommand_NativeSkillsConflict;
                }
                foreach (var path in PluginSettings(client, request, executionContext, environment))
                {
                    var config = await reader.ReadOptionalAsync(path, cancellationToken);
                    if (config is not null && (client.UsesSkillsCatalog
                        ? HasAspireCatalog(config)
                        : AspireSkillsPluginConfiguration.HasPlugin(config)))
                    {
                        return AgentCommandStrings.InitCommand_NativeSkillsConflict;
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> PluginSettings(
        AgentClient client, NativeAgentPluginRequest request, CliExecutionContext context, IEnvironment environment)
    {
        if (client.UsesSkillsCatalog)
        {
            foreach (var path in OpenCodeLocations(request, context, environment).SelectMany(location => location.Candidates)
                .Concat(ConfigFiles(client.GlobalPluginDirectory(context, environment)))
                .Concat(ConfigFiles(client.ManagedPluginDirectory(context, environment))).Distinct(AgentPath.Comparer))
            {
                yield return path;
            }
            yield break;
        }

        yield return Path.Combine(client.GlobalPluginDirectory(context, environment), "settings.json");
        if (request.WorkspaceRoot is { } project)
        {
            foreach (var path in client.ProjectPluginSettingsFiles)
            {
                yield return Path.Combine(project.FullName, path);
            }
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

    private static IEnumerable<OpenCodeLocation> OpenCodeLocations(
        NativeAgentPluginRequest request, CliExecutionContext context, IEnvironment environment)
    {
        if (request.WorkspaceRoot is { } project)
        {
            yield return Location(project.FullName, AgentConfigurationScope.Project, includeNested: true);
        }
        else if (request.Scope is AgentConfigurationScope.Project)
        {
            AgentPath.RequireWorkspaceRoot(request.WorkspaceRoot);
        }

        yield return AgentPath.GetOverride("OPENCODE_CONFIG", context, environment) is { } custom
            ? new(custom, AgentConfigurationScope.Global, [custom])
            : Location(AgentClient.OpenCode.GlobalPluginDirectory(context, environment), AgentConfigurationScope.Global, includeNested: false);
        if (AgentPath.GetOverride("OPENCODE_CONFIG_DIR", context, environment) is { } directory)
        {
            yield return Location(directory, AgentConfigurationScope.Global, includeNested: false);
        }

        static OpenCodeLocation Location(string directory, AgentConfigurationScope scope, bool includeNested)
        {
            var candidates = ConfigFiles(directory).ToList();
            if (includeNested)
            {
                candidates.AddRange(ConfigFiles(Path.Combine(directory, ".opencode")));
            }
            return new(candidates.LastOrDefault(File.Exists) ??
                OpenCodePaths.GetMcpConfigPath(new DirectoryInfo(directory)), scope, candidates);
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

    private static IEnumerable<string> ConfigFiles(string directory)
    {
        yield return Path.Combine(directory, "opencode.json");
        yield return Path.Combine(directory, "opencode.jsonc");
    }

    private sealed record OpenCodeLocation(string Path, AgentConfigurationScope Scope, IReadOnlyList<string> Candidates);
}

internal enum AspireSkillsMode
{
    Bundled,
    Native,
    None
}

internal sealed record NativeAgentPluginRequest(
    DirectoryInfo? WorkspaceRoot,
    AgentConfigurationScope Scope,
    IReadOnlyList<AgentClient> Clients);

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
    AgentConfigurationScope Scope,
    AgentConfigurationStatus Status,
    string? Message);
