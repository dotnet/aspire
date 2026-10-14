// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Agents.VsCode;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Agents.Copilot;

/// <summary>
/// Discovers Copilot App or CLI and configures their shared environment.
/// </summary>
internal sealed class CopilotAgentEnvironmentScanner : IAgentEnvironmentScanner
{
    internal const string HookEventName = "postToolUse";

    private readonly ICopilotCliRunner _copilotCliRunner;
    private readonly ICopilotAppInstallationDetector _copilotAppInstallationDetector;
    private readonly IVsCodeCliRunner _vsCodeCliRunner;
    private readonly CliExecutionContext _executionContext;
    private readonly IEnvironment _environment;
    private readonly ILogger<CopilotAgentEnvironmentScanner> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CopilotAgentEnvironmentScanner"/>.
    /// </summary>
    /// <param name="copilotCliRunner">The Copilot CLI runner for checking if Copilot CLI is installed.</param>
    /// <param name="copilotAppInstallationDetector">The detector for checking if the Copilot App is installed.</param>
    /// <param name="vsCodeCliRunner">The runner used to discover editor-hosted Copilot.</param>
    /// <param name="executionContext">The CLI execution context for resolving workspace and user configuration paths.</param>
    /// <param name="environment">The environment abstraction for reading environment variables.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public CopilotAgentEnvironmentScanner(
        ICopilotCliRunner copilotCliRunner,
        ICopilotAppInstallationDetector copilotAppInstallationDetector,
        IVsCodeCliRunner vsCodeCliRunner,
        CliExecutionContext executionContext,
        IEnvironment environment,
        ILogger<CopilotAgentEnvironmentScanner> logger)
    {
        ArgumentNullException.ThrowIfNull(copilotCliRunner);
        ArgumentNullException.ThrowIfNull(copilotAppInstallationDetector);
        ArgumentNullException.ThrowIfNull(vsCodeCliRunner);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);
        _copilotCliRunner = copilotCliRunner;
        _copilotAppInstallationDetector = copilotAppInstallationDetector;
        _vsCodeCliRunner = vsCodeCliRunner;
        _executionContext = executionContext;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public AgentClient Client => AgentClient.Copilot;

    public string DisplayName => Client.DisplayName;

    public override string ToString() => DisplayName;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AgentClientDetection>> ScanAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Starting GitHub Copilot environment scan");
        var detections = new List<AgentClientDetection>();

        if (_copilotAppInstallationDetector.GetInstallationMarker() is { } installationMarker)
        {
            _logger.LogDebug("Detected GitHub Copilot App using installation marker {Marker}", installationMarker);
            detections.Add(new(AgentEnvironment.CopilotApp, Version: null));
        }

        // VS Code can supply an interactive Copilot installation shim. Do not invoke it during
        // discovery, where an installation prompt could hang the enclosing command.
        if (_environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode")
        {
            _logger.LogDebug("Detected VS Code terminal environment. Skipping the Copilot CLI version probe.");
        }
        else
        {
            var version = await _copilotCliRunner.GetVersionAsync(cancellationToken).ConfigureAwait(false);
            if (version is not null)
            {
                _logger.LogDebug("Found GitHub Copilot CLI version: {Version}", version);
                detections.Add(new(AgentEnvironment.CopilotCli, version));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (detections.Count == 0 && await HasVsCodeHintAsync(context, cancellationToken))
        {
            detections.Add(new(AgentEnvironment.VsCode, Version: null));
        }

        return detections.AsReadOnly();
    }

    private async Task<bool> HasVsCodeHintAsync(AgentEnvironmentScanContext context, CancellationToken cancellationToken)
    {
        var isVsCodeTerminal = _environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode";
        var hasProjectConfiguration = context.FindProjectDirectory(".vscode", _executionContext.HomeDirectory) is not null;
        if (isVsCodeTerminal || hasProjectConfiguration)
        {
            return true;
        }

        foreach (var insiders in new[] { false, true })
        {
            var version = await _vsCodeCliRunner.GetVersionAsync(new VsCodeRunOptions { UseInsiders = insiders }, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (version is not null)
            {
                // VS Code is a selection hint, not evidence that Copilot CLI is installed.
                // Keep its identity distinct so it cannot acquire CLI/App hook eligibility.
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public IEnumerable<AgentConfigurationTarget> GetTargets(AgentInitRequest request)
    {
        foreach (var target in GetPluginTargets(request))
        {
            yield return target;
        }

        var copilotDirectory = CopilotPaths.GetConfigDirectory(_executionContext, _environment);
        if (request.Assets.Mcp)
        {
            foreach (var target in Client.GetMcpConfigPaths(request.Scope, request.WorkspaceRoot, _executionContext, _environment))
            {
                if (target.ConfigureServer || File.Exists(target.Path))
                {
                    yield return McpTarget(target.Path, request.Scope, target.ServersPropertyName, target.ConfigureServer);
                }
            }
        }

        AgentConfigurationTarget McpTarget(string path, AgentConfigurationScope scope, string serversPropertyName, bool configureServer)
            => new(path, scope, AgentAssetKind.Mcp, [this], $"{serversPropertyName}:aspire", async (root, context, cancellationToken) =>
            {
                var settings = await AgentConfigurationJson.ReadSettingsAsync(context, CopilotPaths.PluginSettings(request, _executionContext, _environment), cancellationToken);
                var managed = await AgentConfigurationJson.ReadSettingsAsync(context, CopilotPaths.ManagedSettings(_executionContext, _environment), cancellationToken);
                if (AspireMcpConfiguration.CheckPolicy(settings, managed, managedAllowlistOnly: true) is { } policy)
                {
                    return policy;
                }

                if (!configureServer)
                {
                    if (AgentConfigurationJson.OptionalObject(root, serversPropertyName)?.ContainsKey(AspireMcpConfiguration.ServerName) is not true)
                    {
                        return AgentConfigurationEdit.Applied();
                    }

                    return AspireMcpConfiguration.Apply(root, serversPropertyName, commandArray: false);
                }

                var mcpFiles = new List<string> { Path.Combine(copilotDirectory, "mcp-config.json") };
                if (request.WorkspaceRoot is { } workspaceRoot)
                {
                    mcpFiles.Add(Path.Combine(workspaceRoot.FullName, ".github", "mcp.json"));
                    mcpFiles.Add(Path.Combine(workspaceRoot.FullName, ".mcp.json"));
                }

                foreach (var other in await AgentConfigurationJson.ReadSettingsAsync(context, mcpFiles, cancellationToken))
                {
                    if (AspireMcpConfiguration.CheckPolicy([other]) is { } disabled)
                    {
                        return disabled;
                    }

                    var servers = AspireMcpConfiguration.UsesBareServers(other) ? other : AgentConfigurationJson.OptionalObject(other, "mcpServers");
                    if (AspireMcpConfiguration.CheckExistingEntry(servers, commandArray: false,
                        () => AspireMcpConfiguration.UsesBareServers(root) ? root : AgentConfigurationJson.OptionalObject(root, "mcpServers")) is { } existing)
                    {
                        return existing;
                    }
                }

                if (scope is AgentConfigurationScope.Project && AspireMcpConfiguration.UsesBareServers(root))
                {
                    return AspireMcpConfiguration.Apply(root, serversPropertyName, commandArray: false, bare: true);
                }

                return AspireMcpConfiguration.Apply(root, Client, scope, path);
            });
    }

    private IEnumerable<AgentConfigurationTarget> GetPluginTargets(AgentInitRequest request)
    {
        if (request.Assets.AspireSkills)
        {
            yield return Target(request.Scope is AgentConfigurationScope.Project
                ? Path.Combine(AgentPath.RequireWorkspaceRoot(request.WorkspaceRoot).FullName, ".github", "copilot", "settings.json")
                : Path.Combine(CopilotPaths.GetConfigDirectory(_executionContext, _environment), "settings.json"), request.Scope);
        }

        AgentConfigurationTarget Target(string path, AgentConfigurationScope scope)
            => new(path, scope, AgentAssetKind.AspireSkills, [this], "plugins:aspire", async (root, context, cancellationToken) =>
                AspireSkillsPluginConfiguration.Apply(root, await AgentConfigurationJson.ReadSettingsAsync(
                    context, CopilotPaths.PluginSettings(request, _executionContext, _environment), cancellationToken)));
    }

    public AgentHookConfiguration? GetHookConfiguration(AgentInitRequest request)
        => request.Detections.Any(detection => detection.Client is AgentEnvironment.CopilotApp or AgentEnvironment.CopilotCli)
            ? new(
                Path.Combine(CopilotPaths.GetConfigDirectory(_executionContext, _environment), "hooks", "aspire-telemetry.json"),
                CopilotPaths.PluginSettings(request, _executionContext, _environment),
                CopilotPaths.ExistingHookSettings(request, _executionContext, _environment),
                ValidateHooks,
                ApplyHook)
            : null;

    public static void ValidateHooks(JsonObject root)
    {
        if (root.TryGetPropertyValue("version", out var version) &&
            (version is not JsonValue number || !number.TryGetValue<int>(out var value) || value != 1))
        {
            throw AgentConfigurationJson.Shape("version");
        }

        if (AgentConfigurationJson.OptionalObject(root, "hooks") is { } hooks &&
            hooks.TryGetPropertyValue(HookEventName, out var entries) &&
            (entries is not JsonArray array || array.Any(entry => entry is not JsonObject)))
        {
            throw AgentConfigurationJson.Shape($"hooks.{HookEventName}");
        }
    }

    public static void ApplyHook(JsonObject root, TelemetryHookScripts scripts, Func<JsonNode?, bool> isAspireHook)
    {
        root["version"] = 1;
        var hooks = AgentConfigurationJson.Object(root, "hooks");
        var entries = hooks[HookEventName] as JsonArray ?? new JsonArray();
        if (!hooks.ContainsKey(HookEventName))
        {
            hooks[HookEventName] = entries;
        }

        // Direct execution preserves all-event coverage without starting a shell for each tool call.
        // https://docs.github.com/en/copilot/reference/hooks-reference#command-hooks
        var (command, args) = AgentTelemetryHook.GetCommand(AgentTelemetryProtocol.HookOptionName);
        var desired = new JsonObject
        {
            ["type"] = "command",
            ["exec"] = command,
            ["args"] = new JsonArray(args.Select(arg => (JsonNode?)JsonValue.Create(arg)).ToArray()),
            ["timeoutSec"] = TelemetryHookConfigurator.HookTimeoutSeconds
        };
        var owned = entries.Where(isAspireHook).ToArray();
        if (owned.Length == 1 && JsonNode.DeepEquals(owned[0], desired))
        {
            return;
        }

        foreach (var entry in owned)
        {
            entries.Remove(entry);
        }

        entries.Add((JsonNode)desired);
    }
}
