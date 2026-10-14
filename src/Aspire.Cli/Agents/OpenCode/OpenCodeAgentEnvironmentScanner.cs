// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Agents.OpenCode;

/// <summary>
/// Discovers OpenCode and configures native catalogs and MCP entries with one coherent schema across scopes.
/// </summary>
internal sealed class OpenCodeAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    internal const string V1Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v1/";
    internal const string V2Catalog = "https://raw.githubusercontent.com/microsoft/aspire-skills/main/opencode/v2/";
    internal const string MinimumV1CatalogVersion = "1.18.31";

    private static readonly SemVersion s_minimumV1CatalogVersion = SemVersion.Parse(MinimumV1CatalogVersion, SemVersionStyles.Strict);

    private readonly IOpenCodeCliRunner _openCodeCliRunner;
    private readonly CliExecutionContext _executionContext;
    private readonly IEnvironment _environment;
    private readonly ILogger<OpenCodeAgentEnvironmentScanner> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenCodeAgentEnvironmentScanner"/>.
    /// </summary>
    /// <param name="openCodeCliRunner">The OpenCode CLI runner for checking if OpenCode is installed.</param>
    /// <param name="executionContext">The CLI execution context for resolving workspace and user configuration paths.</param>
    /// <param name="environment">The environment abstraction for reading environment variables.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public OpenCodeAgentEnvironmentScanner(
        IOpenCodeCliRunner openCodeCliRunner,
        CliExecutionContext executionContext,
        IEnvironment environment,
        ILogger<OpenCodeAgentEnvironmentScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(openCodeCliRunner);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        _openCodeCliRunner = openCodeCliRunner;
        _executionContext = executionContext;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public AgentClient Client => AgentClient.OpenCode;

    public string DisplayName => Client.DisplayName;

    public override string ToString() => DisplayName;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentClientDetection>> ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Starting OpenCode environment scan in directory: {WorkingDirectory}", context.WorkingDirectory.FullName);

        var hasProjectConfiguration = HasProjectConfiguration(context.WorkingDirectory, context.RepositoryRoot);
        // Probe even when configuration exists so application can compare the installed version
        // with the file's schema. A missing executable does not invalidate project evidence.
        var version = await _openCodeCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (hasProjectConfiguration || version is not null)
        {
            _logger.LogDebug("Detected OpenCode with version: {Version}", version);
            return [new(AgentEnvironment.OpenCode, version)];
        }

        return [];
    }

    /// <summary>
    /// Checks for OpenCode configuration files within the workspace boundary.
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <param name="repositoryRoot">The workspace root to use as the boundary for searches.</param>
    private static bool HasProjectConfiguration(DirectoryInfo startDirectory, DirectoryInfo? repositoryRoot)
        // V2 also loads configuration inside .opencode; a skills-only directory is insufficient.
        // https://opencode.ai/v2/docs/config#locations
        => AgentPath.ProjectDirectories(startDirectory, repositoryRoot).Any(directory =>
            ConfigFiles(directory.FullName).Concat(ConfigFiles(Path.Combine(directory.FullName, ".opencode"))).Any(File.Exists));

    /// <inheritdoc />
    public IEnumerable<AgentConfigurationTarget> GetTargets(AgentInitRequest request)
        => GetTargets(request, null);

    public async Task<IReadOnlyList<AgentConfigurationTarget>> GetTargetsAsync(AgentInitRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var paths = new Dictionary<string, string>(AgentPath.Comparer);
        var failures = new Dictionary<string, Exception>(AgentPath.Comparer);
        if (request.Assets.Mcp)
        {
            foreach (var location in GetLocations(request, _executionContext, _environment).Where(location => location.Scope == request.Scope))
            {
                try
                {
                    var (_, path) = await ReadMcpLocationAsync(location.Path, location.Scope, cancellationToken);
                    paths[location.Path] = path;
                }
                catch (Exception ex) when (ex is AgentConfigurationException or IOException or UnauthorizedAccessException)
                {
                    failures[location.Path] = ex;
                }
            }
        }

        return GetTargets(request, paths).Select(target =>
            target.Asset is AgentAssetKind.Mcp && failures.TryGetValue(target.Path, out var error)
                ? target with { ApplyAsync = (_, _, _) => Task.FromException<AgentConfigurationEdit>(error) }
                : target).ToArray();
    }

    private IEnumerable<AgentConfigurationTarget> GetTargets(AgentInitRequest request, IReadOnlyDictionary<string, string>? mcpPaths)
    {
        if (!request.Assets.AspireSkills && !request.Assets.Mcp)
        {
            yield break;
        }

        var locations = GetLocations(request, _executionContext, _environment).ToArray();

        foreach (var location in locations.Where(location => location.Scope == request.Scope))
        {
            if (request.Assets.AspireSkills)
            {
                yield return Target(location, AgentAssetKind.AspireSkills, location.Path);
            }

            if (request.Assets.Mcp)
            {
                yield return Target(location, AgentAssetKind.Mcp, mcpPaths?.GetValueOrDefault(location.Path) ?? location.Path);
            }
        }

        AgentConfigurationTarget Target(OpenCodeLocation location, AgentAssetKind asset, string targetPath)
            => new(targetPath, location.Scope, asset, [this], asset is AgentAssetKind.AspireSkills ? "catalog:aspire" : "mcp:aspire",
                async (root, context, cancellationToken) =>
                {
                    var documents = new Dictionary<string, JsonObject>(AgentPath.Comparer);
                    foreach (var path in PluginSettings(request, _executionContext, _environment))
                    {
                        if (await context.ReadOptionalAsync(path, cancellationToken) is { } config)
                        {
                            documents[path] = config;
                        }
                    }

                    var present = location.Candidates.Where(documents.ContainsKey).ToArray();
                    var active = present.Length > 0 ? present[^1] : location.Path;
                    if (asset is AgentAssetKind.Mcp)
                    {
                        // An inherited {"mcp":{"aspire":...}} belongs to its JSON source,
                        // not the higher-precedence JSONC file used for catalog registration.
                        (_, active) = await ReadMcpLocationAsync(active, location.Scope, cancellationToken);
                    }
                    if (!AgentPath.Comparer.Equals(active, targetPath))
                    {
                        return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_ConcurrentChange);
                    }

                    var configs = documents.Values.Append(root).ToList();
                    if (_environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline)
                    {
                        var inlineConfig = AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline));
                        configs.Add(inlineConfig);
                        if (inlineConfig.ContainsKey(asset is AgentAssetKind.AspireSkills ? "skills" : "mcp"))
                        {
                            return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_PolicyBlocked);
                        }
                    }

                    var schema = ResolveSchema(request.Detections, configs, validateMcp: asset is AgentAssetKind.Mcp);
                    if (asset is AgentAssetKind.AspireSkills)
                    {
                        if (schema == 1 && GetUnsupportedCatalogVersion(request.Detections) is { } unsupportedVersion)
                        {
                            // The upstream V1 contract requires 1.18.31+ for skills.urls.
                            // Keep this capability check on the catalog mutation, not schema
                            // resolution: older clients still support native V1 MCP entries.
                            // https://github.com/microsoft/aspire-skills/pull/69
                            return AgentConfigurationEdit.Blocked(string.Format(CultureInfo.CurrentCulture,
                                AgentCommandStrings.Configuration_OpenCodeCatalogVersionUnsupported, unsupportedVersion, MinimumV1CatalogVersion));
                        }

                        return ApplySkills(root, configs, schema);
                    }

                    foreach (var config in configs)
                    {
                        var container = AgentConfigurationJson.OptionalObject(config, "mcp");
                        var servers = schema == 2 && container is not null ? AgentConfigurationJson.OptionalObject(container, "servers") : container;
                        if (AspireMcpConfiguration.CheckExistingEntry(servers, commandArray: true, () =>
                        {
                            var ownMcp = AgentConfigurationJson.OptionalObject(root, "mcp");
                            return schema == 2 && ownMcp is not null ? AgentConfigurationJson.OptionalObject(ownMcp, "servers") : ownMcp;
                        }) is { } existing)
                        {
                            return existing;
                        }
                    }

                    if (schema == 1)
                    {
                        return AspireMcpConfiguration.Apply(root, Client, location.Scope, targetPath);
                    }

                    var mcp = AgentConfigurationJson.Object(root, "mcp");
                    return AspireMcpConfiguration.Apply(mcp, "servers", commandArray: true);
                });
    }

    private async Task<(JsonObject Config, string Path)> ReadMcpLocationAsync(
        string path, AgentConfigurationScope scope, CancellationToken cancellationToken)
    {
        try
        {
            return await McpConfigFileHelper.ReadSelectedConfigAsync(path, Client, scope, _environment, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new AgentConfigurationException(ex.Message);
        }
    }

    internal static IEnumerable<string> PluginSettings(
        AgentInitRequest request, CliExecutionContext executionContext, IEnvironment environment)
        => GetLocations(request, executionContext, environment).SelectMany(location => location.Candidates)
            .Concat(ConfigFiles(OpenCodePaths.GetConfigDirectory(executionContext, environment)))
            .Concat(ConfigFiles(OpenCodePaths.GetManagedDirectory(executionContext, environment)))
            .Distinct(AgentPath.Comparer);

    private static IEnumerable<OpenCodeLocation> GetLocations(
        AgentInitRequest request, CliExecutionContext executionContext, IEnvironment environment)
    {
        if (request.WorkspaceRoot is { } workspaceRoot)
        {
            yield return FindLocation(workspaceRoot.FullName, AgentConfigurationScope.Project, includeNested: true,
                AgentClient.OpenCode.GetMcpConfigPath(AgentConfigurationScope.Project, workspaceRoot, executionContext, environment));
        }
        else if (request.Scope is AgentConfigurationScope.Project)
        {
            AgentPath.RequireWorkspaceRoot(request.WorkspaceRoot);
        }

        var userDirectory = OpenCodePaths.GetConfigDirectory(executionContext, environment);
        yield return AgentPath.GetOverride("OPENCODE_CONFIG", executionContext, environment) is { } customFile
            ? new OpenCodeLocation(customFile, AgentConfigurationScope.Global, [customFile])
            : FindLocation(userDirectory, AgentConfigurationScope.Global, includeNested: false,
                AgentClient.OpenCode.GetMcpConfigPath(AgentConfigurationScope.Global, request.WorkspaceRoot, executionContext, environment));
        if (AgentPath.GetOverride("OPENCODE_CONFIG_DIR", executionContext, environment) is { } customDirectory)
        {
            yield return FindLocation(customDirectory, AgentConfigurationScope.Global, includeNested: false,
                OpenCodePaths.GetMcpConfigPath(new DirectoryInfo(customDirectory)));
        }
    }

    private static int ResolveSchema(IReadOnlyList<AgentClientDetection> detections, IEnumerable<JsonObject> configs, bool validateMcp)
    {
        // Both generations share a $schema URL; use installed versions and configuration shapes.
        var versions = new HashSet<int>();
        foreach (var detection in detections.Where(detection => detection.Client is AgentEnvironment.OpenCode))
        {
            if (detection.Version is { } version)
            {
                if (version.Major > 2)
                {
                    throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
                }

                versions.Add(version.Major < 2 ? 1 : 2);
            }
        }

        foreach (var config in configs)
        {
            if (config.TryGetPropertyValue("skills", out var skills))
            {
                switch (skills)
                {
                    case JsonObject v1:
                        AgentConfigurationJson.OptionalStrings(v1, "paths");
                        CheckCatalogs(AgentConfigurationJson.OptionalStrings(v1, "urls"), 1);
                        versions.Add(1);
                        break;
                    case JsonArray v2 when v2.All(value => AgentConfigurationJson.String(value) is not null):
                        CheckCatalogs(v2, 2);
                        versions.Add(2);
                        break;
                    default:
                        throw AgentConfigurationJson.Shape("skills");
                }
            }

            if (AgentConfigurationJson.OptionalObject(config, "mcp") is { Count: > 0 } mcp)
            {
                if (mcp.ContainsKey("servers") || IsV2Timeout(mcp["timeout"]))
                {
                    if (mcp.Any(property => property.Key is not ("servers" or "timeout")))
                    {
                        throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
                    }

                    // Timeout values do not govern catalog registration; only the schema markers do.
                    if (validateMcp && mcp.ContainsKey("timeout") &&
                        (!IsV2Timeout(mcp["timeout"]) || mcp["timeout"]!.AsObject().Any(property =>
                            property.Value is not JsonValue value || !value.TryGetValue<int>(out var timeout) || timeout <= 0)))
                    {
                        throw AgentConfigurationJson.Shape("mcp.timeout");
                    }

                    CheckServerShapes(AgentConfigurationJson.OptionalObject(mcp, "servers"), 2);
                    versions.Add(2);
                }
                else
                {
                    CheckServerShapes(mcp, 1);
                    versions.Add(1);
                }
            }
        }

        if (versions.Count > 1)
        {
            throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
        }

        // An explicitly selected, undetected client with no schema evidence uses stable V1.
        return versions.SingleOrDefault(1);
    }

    private static string? GetUnsupportedCatalogVersion(IReadOnlyList<AgentClientDetection> detections)
    {
        foreach (var detection in detections.Where(detection => detection.Client is AgentEnvironment.OpenCode))
        {
            if (detection.Version is { } version && SemVersion.ComparePrecedence(version, s_minimumV1CatalogVersion) < 0)
            {
                return version.ToString();
            }
        }

        return null;
    }

    private static AgentConfigurationEdit ApplySkills(JsonObject root, IReadOnlyList<JsonObject> configs, int schema)
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

        JsonArray target;
        if (schema == 1)
        {
            var skills = AgentConfigurationJson.Object(root, "skills");
            target = AgentConfigurationJson.OptionalStrings(skills, "urls") ?? new JsonArray();
            if (!skills.ContainsKey("urls"))
            {
                skills["urls"] = target;
            }
        }
        else
        {
            target = root["skills"] as JsonArray ?? new JsonArray();
            if (!root.ContainsKey("skills"))
            {
                root["skills"] = target;
            }
        }

        if (!target.Any(value => CatalogSchema(AgentConfigurationJson.String(value)) == schema))
        {
            target.Add((JsonNode?)preferred);
        }

        return AgentConfigurationEdit.Applied();
    }

    private static void CheckCatalogs(JsonArray? sources, int schema)
    {
        if (sources is not null && sources.Any(value => CatalogSchema(AgentConfigurationJson.String(value)) is { } declared && declared != schema))
        {
            throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
        }
    }

    internal static bool HasAspireCatalog(JsonObject root)
    {
        var sources = root["skills"] switch
        {
            null => null,
            JsonObject skills => AgentConfigurationJson.OptionalStrings(skills, "urls"),
            JsonArray array => array,
            _ => throw AgentConfigurationJson.Shape("skills")
        };
        return sources?.Any(source => CatalogSchema(AgentConfigurationJson.String(source)) is not null) is true;
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
            : path.EndsWith("/opencode/v2", StringComparison.Ordinal) ? 2
            : null;
    }

    private static void CheckServerShapes(JsonObject? servers, int schema)
    {
        if (servers is null)
        {
            return;
        }

        foreach (var (_, node) in servers)
        {
            if (node is not JsonObject server)
            {
                throw AgentConfigurationJson.Shape("mcp");
            }

            if (server.ContainsKey(schema == 1 ? "disabled" : "enabled"))
            {
                throw new AgentConfigurationException(AgentCommandStrings.Configuration_OpenCodeSchemaConflict);
            }
        }
    }

    private static bool IsV2Timeout(JsonNode? node)
        => node is JsonObject timeout && timeout.All(property => property.Key is "startup" or "catalog" or "execution");

    private static OpenCodeLocation FindLocation(string directory, AgentConfigurationScope scope, bool includeNested, string defaultPath)
    {
        var candidates = ConfigFiles(directory).ToList();
        if (includeNested)
        {
            candidates.AddRange(ConfigFiles(Path.Combine(directory, ".opencode")));
        }

        return new OpenCodeLocation(candidates.LastOrDefault(File.Exists) ?? defaultPath, scope, candidates);
    }

    private static IEnumerable<string> ConfigFiles(string directory)
    {
        yield return Path.Combine(directory, "opencode.json");
        yield return Path.Combine(directory, "opencode.jsonc");
    }

    private sealed record OpenCodeLocation(string Path, AgentConfigurationScope Scope, IReadOnlyList<string> Candidates);
}
