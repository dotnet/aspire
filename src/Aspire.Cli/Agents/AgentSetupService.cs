// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Completions;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Agents;

/// <summary>
/// Shares native agent registration between installation onboarding and explicit initialization.
/// </summary>
internal sealed class AgentSetupService(
    IEnumerable<IAgentEnvironmentScanner> environments,
    AgentConfigurationWriter writer,
    IAgentSkillInstaller skillInstaller,
    IInteractionService interactionService,
    CliExecutionContext executionContext,
    IEnvironment environment,
    IConfigurationService configurationService,
    ILogger<AgentSetupService> logger)
{
    public IReadOnlyList<IAgentEnvironmentScanner> Environments { get; } = Array.AsReadOnly(environments.ToArray());

    public static Option<AgentConfirmation?> CreateAssetOption(string name, string description)
    {
        // A bool option tokenizes "--aspire-skills n" as a bare flag plus an unmatched
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
        var supportedClients = string.Join(",", AgentClient.All.Select(static client => client.DisplayName));
        return new Option<string?>("--agents")
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
                var selected = ConsoleInteractionService.MatchChoices(value, AgentClient.All, static client => client.DisplayName);
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
    }

    public async Task<bool> PromptForPluginAsync(PromptBinding<bool> binding, string prompt, CancellationToken cancellationToken)
    {
        var (wasProvided, requested) = binding.Resolve();
        if (!wasProvided || requested)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning, AgentCommandStrings.InitCommand_PluginRiskWarning);
        }
        interactionService.DisplaySubtleMessage(string.Format(CultureInfo.CurrentCulture,
            AgentCommandStrings.InitCommand_DirectSkillsAlternative, AspireSkillsPluginConfiguration.RepositoryUrl));

        return await interactionService.PromptConfirmAsync(prompt, binding: binding, cancellationToken: cancellationToken);
    }

    public Task<AgentDiscovery> ScanAsync(DirectoryInfo workingDirectory, DirectoryInfo? workspaceRoot, CancellationToken cancellationToken)
        => interactionService.ShowStatusAsync(
            AgentCommandStrings.InitCommand_DetectingAgents,
            async () =>
            {
                var detected = new List<IAgentEnvironmentScanner>();
                var clients = new List<AgentClientDetection>();
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var scanner in Environments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var context = new AgentEnvironmentScanContext(workingDirectory, workspaceRoot);
                    await scanner.ScanAsync(context, cancellationToken);
                    if (context.DetectedClients.Count > 0)
                    {
                        detected.Add(scanner);
                        clients.AddRange(context.DetectedClients);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                return new AgentDiscovery(Array.AsReadOnly(detected.ToArray()), Array.AsReadOnly(clients.Distinct().ToArray()));
            },
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
            var selected = ConsoleInteractionService.MatchChoices(requested, AgentClient.All, static client => client.DisplayName);
            if (selected is null)
            {
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, AgentCommandStrings.InitCommand_InvalidEnvironments,
                    requested, string.Join(",", AgentClient.All.Select(static client => client.DisplayName)),
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

    public async Task WarnAboutLocalSkillsAsync(
        DirectoryInfo workingDirectory,
        AgentInitRequest request,
        CancellationToken cancellationToken)
    {
        var localSkills = await LocalAspireSkills.FindAsync(workingDirectory, request.WorkspaceRoot,
            request.Environments, executionContext, environment, cancellationToken);
        foreach (var error in localSkills.Errors)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning, error);
        }
        if (localSkills.Files.Count > 0)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning, string.Format(CultureInfo.CurrentCulture,
                AgentCommandStrings.InitCommand_LocalSkillsConflict, string.Join(Environment.NewLine, localSkills.Files.Select(file => file.Path))));
        }
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

        IEnumerable<AgentConfigurationTarget> nativeTargets = request.Assets.Mcp || request.Assets.AspireSkills
            ? request.Environments.Distinct().SelectMany(client => client.GetTargets(request))
            : [];
        // Native settings and hooks can share a file and must be committed together.
        var results = new List<AgentTargetResult>(
            await writer.ApplyAsync(nativeTargets.Concat(hooks?.Plan(request) ?? []), cancellationToken));

        cancellationToken.ThrowIfCancellationRequested();
        if (request.Assets.Playwright || request.Assets.DotnetInspect)
        {
            results.AddRange(await skillInstaller.InstallAsync(request, cancellationToken));
        }

        return new AgentInitResult(results);
    }

    /// <summary>
    /// Offers user-only plugin registration without making optional failure an installation failure.
    /// </summary>
    public async Task OfferUserPluginSetupAsync(
        PromptBinding<bool> pluginBinding,
        PromptBinding<string?> agentsBinding,
        bool interactive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (wasProvided, requested) = pluginBinding.Resolve();
        var (agentsProvided, agents) = agentsBinding.Resolve();
        var none = agentsProvided && string.Equals(agents, ConsoleInteractionService.NoneChoice, StringComparison.OrdinalIgnoreCase);
        if (!wasProvided && !none)
        {
            // Piped installer input is executable script text, not consent. Leave the
            // offer pending until a caller has a real interactive input stream.
            if (!interactive)
            {
                return;
            }

            try
            {
                // An unreadable completion flag is not evidence that consent is still pending.
                var global = await configurationService.GetGlobalConfigurationAsync(throwOnError: true, cancellationToken);
                if (global.TryGetValue(CliConfigNames.AgentSetupCompleted, out var value) &&
                    string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Could not read agent setup completion.");
                interactionService.DisplayMessage(KnownEmojis.Warning, string.Format(CultureInfo.CurrentCulture,
                    AgentCommandStrings.SetupCommand_OfferStateReadFailed, ex.Message));
                return;
            }
        }

        try
        {
            if (!none && (wasProvided && !requested ||
                !await PromptForPluginAsync(pluginBinding, AgentCommandStrings.SetupCommand_PluginOffer, cancellationToken)))
            {
                interactionService.DisplaySubtleMessage(AgentCommandStrings.InitCommand_NoConfigurationSelected);
            }
            else if (!none)
            {
                var discovery = await ScanAsync(executionContext.WorkingDirectory, workspaceRoot: null, cancellationToken);
                var selected = await SelectAgentsAsync(agentsBinding, discovery, interactive, cancellationToken);
                if (selected is null)
                {
                    return;
                }
                if (selected.Count == 0)
                {
                    interactionService.DisplayMessage(KnownEmojis.Information, AgentCommandStrings.SetupCommand_NoDetectedAgents);
                }
                else
                {
                    var request = new AgentInitRequest(null, new(false, false, false, true),
                        AgentConfigurationScope.Global, selected, discovery.Detections);
                    await WarnAboutLocalSkillsAsync(executionContext.WorkingDirectory, request, cancellationToken);
                    var result = await ConfigureAsync(request, hooks: null, cancellationToken);
                    DisplayResults(result);
                    if (result.HasErrors)
                    {
                        interactionService.DisplayMessage(KnownEmojis.Warning, AgentCommandStrings.SetupCommand_PluginRetry);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is AgentConfigurationException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            logger.LogWarning(ex, "Optional Aspire plugin setup failed.");
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture,
                AgentCommandStrings.Configuration_ReadWriteFailed, ex.Message));
            interactionService.DisplayMessage(KnownEmojis.Warning, AgentCommandStrings.SetupCommand_PluginRetry);
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await configurationService.SetConfigurationAsync(CliConfigNames.AgentSetupCompleted, "true", isGlobal: true, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Could not save agent setup completion.");
            interactionService.DisplayMessage(KnownEmojis.Warning, string.Format(CultureInfo.CurrentCulture,
                AgentCommandStrings.SetupCommand_OfferStateWriteFailed, ex.Message));
        }
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
                AgentAssetKind.TelemetryHooks => AgentCommandStrings.InitCommand_TelemetryHooksAsset,
                _ => throw new UnreachableException()
            };
            var format = target.Status switch
            {
                AgentConfigurationStatus.Configured when target.Asset is AgentAssetKind.AspireSkills => AgentCommandStrings.InitCommand_RegisteredTarget,
                AgentConfigurationStatus.Configured when target.Asset is AgentAssetKind.Playwright or AgentAssetKind.DotnetInspect => AgentCommandStrings.InitCommand_InstalledTarget,
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
