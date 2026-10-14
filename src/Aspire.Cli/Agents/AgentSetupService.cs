// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Completions;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.Cli.Agents.ClaudeCode;
using Aspire.Cli.Agents.Copilot;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Agents.OpenCode;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Agents;

/// <summary>
/// Selects agents and applies their requested native configuration and copied skills.
/// </summary>
internal sealed class AgentSetupService(
    IEnumerable<IAgentEnvironmentScanner> environments,
    IAgentEnvironmentDetector detector,
    AgentConfigurationWriter writer,
    IAgentSkillInstaller skillInstaller,
    IInteractionService interactionService,
    CliExecutionContext executionContext,
    IEnvironment environment,
    ILogger<AgentSetupService> logger)
{
    public IReadOnlyList<IAgentEnvironmentScanner> Environments { get; } = Array.AsReadOnly(environments.ToArray());

    public static Option<AgentConfirmation?> CreateAssetOption(string name, string description)
    {
        // A bool option tokenizes "--mcp n" as a bare flag plus an unmatched
        // token. A typed confirmation supports y/n, true/false and a bare affirmative.
        var option = new Option<AgentConfirmation?>(name)
        {
            Description = description,
            Recursive = true,
            Arity = ArgumentArity.ZeroOrOne,
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return AgentConfirmation.Yes;
                }

                var value = result.Tokens[0].Value;
                if (string.Equals(value, "y", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return AgentConfirmation.Yes;
                }

                if (string.Equals(value, "n", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return AgentConfirmation.No;
                }

                result.AddError(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InvalidAssetValue, name, value));
                return null;
            }
        };
        option.CompletionSources.Clear();
        option.CompletionSources.Add(static _ =>
        [
            new CompletionItem("y"),
            new CompletionItem("n"),
            new CompletionItem("true"),
            new CompletionItem("false")
        ]);

        return option;
    }

    public static PromptBinding<bool> CreateAssetBinding(ParseResult parseResult, Option<AgentConfirmation?> option, bool defaultValue)
        => new(parseResult, $"'{option.Name}'",
            result => (result.GetResult(option) is { Implicit: false }, result.GetValue(option) is AgentConfirmation.Yes),
            defaultValue, hasExplicitDefault: true);

    public static Option<string?> CreateAgentsOption()
    {
        var supportedClients = string.Join(",", AgentClient.All.Select(static client => client.Id));
        var option = new Option<string?>("--agents")
        {
            Description = string.Format(CultureInfo.InvariantCulture, AgentCommandStrings.InitCommand_EnvironmentsOptionDescription,
                supportedClients, ConsoleInteractionService.AllChoice, ConsoleInteractionService.NoneChoice),
            Recursive = true,
            Arity = ArgumentArity.ExactlyOne,
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return null;
                }

                var value = result.Tokens[0].Value;
                var selected = ConsoleInteractionService.MatchChoices(value, AgentClient.All, static client => client.Id);
                if (selected is null || selected.Count == 0 &&
                    !string.Equals(value, ConsoleInteractionService.NoneChoice, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, ConsoleInteractionService.AllChoice, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InvalidEnvironments,
                        value, supportedClients, ConsoleInteractionService.AllChoice, ConsoleInteractionService.NoneChoice));
                }

                return value;
            }
        };
        option.CompletionSources.Clear();
        option.CompletionSources.Add(static _ =>
        [
            .. AgentClient.All.Select(static client => new CompletionItem(client.Id)),
            new CompletionItem(ConsoleInteractionService.AllChoice),
            new CompletionItem(ConsoleInteractionService.NoneChoice)
        ]);

        return option;
    }

    public async Task<AspireSkillsMode> PromptForAspireSkillsAsync(
        PromptBinding<AspireSkillsMode> binding, bool interactive, CancellationToken cancellationToken)
    {
        var (wasProvided, requested) = binding.Resolve();
        if (!wasProvided)
        {
            requested = !interactive ? binding.DefaultValue : await interactionService.PromptForSelectionAsync(
                AgentCommandStrings.InitCommand_ConfigureAspireSkillsPrompt,
                new[] { binding.DefaultValue, AspireSkillsMode.Bundled, AspireSkillsMode.Native, AspireSkillsMode.None }.Distinct().ToArray(),
                static mode => mode switch
                {
                    AspireSkillsMode.Bundled => AgentCommandStrings.InitCommand_BundledSkillsChoice,
                    AspireSkillsMode.Native => AgentCommandStrings.InitCommand_NativeSkillsChoice,
                    AspireSkillsMode.None => AgentCommandStrings.InitCommand_NoSkillsChoice,
                    _ => throw new UnreachableException()
                },
                cancellationToken: cancellationToken);
        }

        if (requested is AspireSkillsMode.Native)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning, AgentCommandStrings.InitCommand_PluginRiskWarning);
            interactionService.DisplaySubtleMessage(string.Format(CultureInfo.CurrentCulture,
                AgentCommandStrings.InitCommand_DirectSkillsAlternative, AspireSkillsPluginConfiguration.RepositoryUrl));
        }

        return requested;
    }

    public Task<AgentDiscovery> ScanAsync(DirectoryInfo workingDirectory, DirectoryInfo? workspaceRoot, CancellationToken cancellationToken)
        => interactionService.ShowStatusAsync(
            AgentCommandStrings.InitCommand_DetectingAgents,
            () => detector.DetectAsync(new AgentEnvironmentScanContext
                {
                    WorkingDirectory = workingDirectory,
                    RepositoryRoot = workspaceRoot
                }, cancellationToken),
            emoji: KnownEmojis.Robot);

    public async Task<IReadOnlyList<IAgentEnvironmentScanner>?> SelectAgentsAsync(
        PromptBinding<string?> binding,
        AgentDiscovery discovery,
        bool interactive,
        CancellationToken cancellationToken)
    {
        interactionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_EnvironmentSelectionNotice);
        var (wasProvided, requested) = binding.Resolve();
        if (wasProvided && requested is not null)
        {
            var selected = ConsoleInteractionService.MatchChoices(requested, AgentClient.All, static client => client.Id);
            if (selected is null)
            {
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InvalidEnvironments,
                    requested, string.Join(",", AgentClient.All.Select(static client => client.Id)),
                    ConsoleInteractionService.AllChoice, ConsoleInteractionService.NoneChoice));
            }

            return selected is null ? null : GetHandlers(selected);
        }

        var detected = AgentClient.All.Where(client => client.Includes(discovery.DetectedEnvironments)).ToArray();
        IReadOnlyList<AgentClient> defaults = detected.Length > 0 ? Array.AsReadOnly(detected) : [AgentClient.Copilot];
        if (!interactive)
        {
            return GetHandlers(defaults);
        }

        var choices = await interactionService.PromptForSelectionsAsync(
            AgentCommandStrings.InitCommand_SelectAgentsPrompt,
            AgentClient.All,
            static client => client.DisplayName.EscapeMarkup(),
            preSelected: defaults,
            optional: true,
            cancellationToken: cancellationToken);
        return GetHandlers(choices);
    }

    private IReadOnlyList<IAgentEnvironmentScanner> GetHandlers(IReadOnlyList<AgentClient> clients)
        => Array.AsReadOnly(clients.Select(client => Environments.Single(scanner => scanner.Client == client)).ToArray());

    public string DescribeScope(AgentInitRequest request)
    {
        var label = request.Scope is AgentConfigurationScope.Project
            ? AgentCommandStrings.InitCommand_ProjectScope
            : AgentCommandStrings.InitCommand_GlobalScope;
        try
        {
            var paths = request.Environments.SelectMany(scanner => GetScopePaths(scanner, request))
                .Select(path => DisplayPath(path, request)).Distinct(AgentPath.Comparer);
            var description = string.Join(", ", paths);
            return label.EscapeMarkup() + (description.Length > 0
                ? " [dim](" + description.EscapeMarkup() + ")[/]"
                : string.Empty);
        }
        catch (Exception ex) when (ex is AgentConfigurationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            logger.LogWarning(ex, "Could not resolve selected agent asset locations for {Scope}.", request.Scope);
            var message = string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.Configuration_ReadWriteFailed, ex.Message);
            return label.EscapeMarkup() + " [dim](" + message.EscapeMarkup() + ")[/]";
        }
    }

    private IEnumerable<string> GetScopePaths(IAgentEnvironmentScanner scanner, AgentInitRequest request)
    {
        // Native Aspire skills show the registration file, not a client-owned download/cache path.
        foreach (var target in scanner.GetTargets(request))
        {
            yield return AgentPath.Resolve(target.Path);
        }
        if (request.Assets.BundledSkills)
        {
            yield return AgentPath.Resolve(AgentSkillInstaller.GetSkillBaseDirectory(
                scanner, request.Scope, request.WorkspaceRoot, executionContext, environment));
        }
        foreach (var asset in new[] { AgentAssetKind.Playwright, AgentAssetKind.DotnetInspect })
        {
            if ((asset is AgentAssetKind.Playwright && request.Assets.Playwright) ||
                (asset is AgentAssetKind.DotnetInspect && request.Assets.DotnetInspect))
            {
                yield return AgentPath.Resolve(Path.Combine(
                    AgentSkillInstaller.GetSkillBaseDirectory(scanner, request.Scope, request.WorkspaceRoot, executionContext, environment),
                    AgentSkillInstaller.GetSkillName(asset)));
            }
        }
    }

    private string DisplayPath(string path, AgentInitRequest request)
    {
        var root = request.Scope is AgentConfigurationScope.Project
            ? AgentPath.RequireWorkspaceRoot(request.WorkspaceRoot).FullName
            : executionContext.HomeDirectory.FullName;
        var relative = Path.GetRelativePath(AgentPath.Resolve(root), path);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return path;
        }

        return request.Scope is AgentConfigurationScope.Project ? relative : Path.Combine("~", relative);
    }

    /// <summary>
    /// Composes native settings and optional hooks atomically before acquiring selected tool skills.
    /// </summary>
    public async Task<AgentInitResult> ConfigureAsync(
        AgentInitRequest request,
        ITelemetryHookConfigurator? hooks,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.Assets.HasAssets || request.Environments.Count == 0)
        {
            return new AgentInitResult([]);
        }

        var conflicts = await CheckSkillsModeAsync(request, cancellationToken);
        if (conflicts.Count > 0)
        {
            return new AgentInitResult(conflicts);
        }

        var nativeTargets = new List<AgentConfigurationTarget>();
        if (request.Assets.Mcp || request.Assets.AspireSkills)
        {
            foreach (var client in request.Environments.Distinct())
            {
                nativeTargets.AddRange(await client.GetTargetsAsync(request, cancellationToken));
            }
        }
        // Native settings and hooks can share a file and must be committed together.
        var results = new List<AgentTargetResult>(
            await writer.ApplyAsync(nativeTargets.Concat(hooks?.Plan(request) ?? []), cancellationToken));

        cancellationToken.ThrowIfCancellationRequested();
        if (request.Assets.BundledSkills || request.Assets.Playwright || request.Assets.DotnetInspect)
        {
            results.AddRange(await skillInstaller.InstallAsync(request, cancellationToken));
        }

        return new AgentInitResult(results);
    }

    private async Task<IReadOnlyList<AgentTargetResult>> CheckSkillsModeAsync(
        AgentInitRequest request, CancellationToken cancellationToken)
    {
        if (request.Assets.AspireSkills)
        {
            var local = await LocalAspireSkills.FindAsync(executionContext.WorkingDirectory, request.WorkspaceRoot,
                request.Environments, executionContext, environment, cancellationToken);
            if (local.Errors.Count > 0 || local.Files.Count > 0)
            {
                var message = string.Join(Environment.NewLine, local.Errors.Concat(local.Files.Count > 0
                    ? [string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_LocalSkillsConflict,
                        string.Join(Environment.NewLine, local.Files.Select(file => file.Path)))]
                    : []));
                return [new(AgentAssetKind.AspireSkills, request.Environments,
                    request.WorkspaceRoot?.FullName ?? executionContext.HomeDirectory.FullName,
                    request.Scope, local.Errors.Count > 0 ? AgentConfigurationStatus.Failed : AgentConfigurationStatus.Blocked, message)];
            }
        }
        else if (request.Assets.BundledSkills)
        {
            // A native plugin registered in either visible scope would compete with copied
            // skills. Inspect its registration without acquiring, disabling, or migrating it.
            var reader = new AgentConfigurationWriter.ReadContext();
            foreach (var client in request.Environments)
            {
                var path = request.WorkspaceRoot?.FullName ?? executionContext.HomeDirectory.FullName;
                try
                {
                    // OpenCode can register a catalog entirely through inline settings.
                    if (client.Client == AgentClient.OpenCode &&
                        environment.GetEnvironmentVariable("OPENCODE_CONFIG_CONTENT") is { Length: > 0 } inline)
                    {
                        path = "OPENCODE_CONFIG_CONTENT";
                        if (OpenCodeAgentEnvironmentScanner.HasAspireCatalog(AgentConfigurationJson.ParseObject(Encoding.UTF8.GetBytes(inline))))
                        {
                            return [new(AgentAssetKind.BundledAspireSkills, request.Environments, path,
                                request.Scope, AgentConfigurationStatus.Blocked, AgentCommandStrings.InitCommand_NativeSkillsConflict)];
                        }
                    }
                    var paths = client.Client == AgentClient.Copilot
                        ? CopilotPaths.PluginSettings(request, executionContext, environment)
                        : client.Client == AgentClient.ClaudeCode
                            ? ClaudeCodeAgentEnvironmentScanner.PluginSettings(request, executionContext, environment)
                            : OpenCodeAgentEnvironmentScanner.PluginSettings(request, executionContext, environment);
                    foreach (var candidate in paths.Distinct(AgentPath.Comparer))
                    {
                        path = candidate;
                        var config = await reader.ReadOptionalAsync(path, cancellationToken);
                        if (config is not null && (client.Client == AgentClient.OpenCode
                            ? OpenCodeAgentEnvironmentScanner.HasAspireCatalog(config)
                            : AspireSkillsPluginConfiguration.HasPlugin(config)))
                        {
                            return [new(AgentAssetKind.BundledAspireSkills, request.Environments, path,
                                request.Scope, AgentConfigurationStatus.Blocked, AgentCommandStrings.InitCommand_NativeSkillsConflict)];
                        }
                    }
                }
                catch (Exception ex) when (ex is AgentConfigurationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    logger.LogWarning(ex, "Could not inspect native Aspire skills at {Path}.", path);
                    return [new(AgentAssetKind.BundledAspireSkills, request.Environments, path,
                        request.Scope, AgentConfigurationStatus.Failed, ex.Message)];
                }
            }
        }

        return [];
    }

    public void DisplayResults(AgentInitResult result)
    {
        foreach (var target in result.Targets)
        {
            var clients = string.Join(", ", target.Environments.Select(client => client.DisplayName));
            var scope = target.Scope is AgentConfigurationScope.Project ? AgentCommandStrings.InitCommand_ProjectScope : AgentCommandStrings.InitCommand_GlobalScope;
            var asset = target.Asset switch
            {
                AgentAssetKind.Mcp => AgentCommandStrings.InitCommand_McpAsset,
                AgentAssetKind.Playwright => AgentCommandStrings.InitCommand_PlaywrightAsset,
                AgentAssetKind.DotnetInspect => AgentCommandStrings.InitCommand_DotnetInspectAsset,
                AgentAssetKind.AspireSkills => AgentCommandStrings.InitCommand_AspireSkillsAsset,
                AgentAssetKind.BundledAspireSkills => AgentCommandStrings.InitCommand_AspireSkillsAsset,
                AgentAssetKind.TelemetryHooks => AgentCommandStrings.InitCommand_TelemetryHooksAsset,
                _ => throw new UnreachableException()
            };
            var format = target.Status switch
            {
                AgentConfigurationStatus.Configured when target.Asset is AgentAssetKind.AspireSkills => AgentCommandStrings.InitCommand_RegisteredTarget,
                AgentConfigurationStatus.Configured when target.Asset is AgentAssetKind.Playwright or AgentAssetKind.DotnetInspect or AgentAssetKind.BundledAspireSkills => AgentCommandStrings.InitCommand_InstalledTarget,
                AgentConfigurationStatus.Configured => AgentCommandStrings.InitCommand_ConfiguredTarget,
                AgentConfigurationStatus.Unchanged => AgentCommandStrings.InitCommand_UnchangedTarget,
                AgentConfigurationStatus.Skipped => AgentCommandStrings.InitCommand_SkippedTarget,
                AgentConfigurationStatus.Blocked => AgentCommandStrings.InitCommand_BlockedTarget,
                AgentConfigurationStatus.Failed => AgentCommandStrings.InitCommand_FailedTarget,
                _ => throw new UnreachableException()
            };
            var message = string.Format(CultureInfo.CurrentCulture, format, asset, clients, scope, target.TargetPath);
            if (target.Status is AgentConfigurationStatus.Unchanged)
            {
                interactionService.DisplaySubtleMessage(message);
            }
            else if (target.Status is AgentConfigurationStatus.Blocked or AgentConfigurationStatus.Failed &&
                target.Asset is not AgentAssetKind.TelemetryHooks)
            {
                interactionService.DisplayError(message);
            }
            else
            {
                interactionService.DisplayMessage(
                    target.Status is AgentConfigurationStatus.Configured ? KnownEmojis.CheckMarkButton : KnownEmojis.Warning, message);
            }

            if (target.Message is not null)
            {
                interactionService.DisplaySubtleMessage(target.Message);
            }
        }

        if (result.Targets.Any(target =>
            target.Asset is AgentAssetKind.AspireSkills or AgentAssetKind.Mcp &&
            target.Status is AgentConfigurationStatus.Configured or AgentConfigurationStatus.Unchanged))
        {
            interactionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_ClientAcquisitionNotice);
        }

        if (result.HasErrors || result.HasWarnings)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning,
                result.HasErrors ? AgentCommandStrings.ConfigurationCompletedWithErrors : AgentCommandStrings.ConfigurationCompletedWithWarnings);
        }
        else
        {
            interactionService.DisplaySuccess(AgentCommandStrings.InitCommand_ConfigurationComplete);
        }
    }
}
