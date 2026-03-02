// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.RegularExpressions;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;
using Spectre.Console;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.AppCommands;

    private readonly INewCommandPrompter _prompter;
    private readonly ITemplate[] _templates;
    private readonly IFeatures _features;
    private readonly IPackagingService _packagingService;
    private readonly IConfigurationService _configurationService;

    private static readonly Option<string> s_nameOption = new("--name", "-n")
    {
        Description = NewCommandStrings.NameArgumentDescription,
        Recursive = true
    };
    private static readonly Option<string?> s_outputOption = new("--output", "-o")
    {
        Description = NewCommandStrings.OutputArgumentDescription,
        Recursive = true
    };
    private static readonly Option<string?> s_sourceOption = new("--source", "-s")
    {
        Description = NewCommandStrings.SourceArgumentDescription,
        Recursive = true
    };
    private static readonly Option<string?> s_versionOption = new("--version")
    {
        Description = NewCommandStrings.VersionArgumentDescription,
        Recursive = true
    };

    private readonly Option<string?> _channelOption;
    private readonly Option<AppHostLanguage?> _languageOption;

    /// <summary>
    /// NewCommand prefetches both template and CLI package metadata.
    /// </summary>
    public bool PrefetchesTemplatePackageMetadata => true;

    /// <summary>
    /// NewCommand prefetches CLI package metadata for update notifications.
    /// </summary>
    public bool PrefetchesCliPackageMetadata => true;

    public NewCommand(
        INewCommandPrompter prompter,
        IInteractionService interactionService,
        ITemplateProvider templateProvider,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IPackagingService packagingService,
        IConfigurationService configurationService)
        : base("new", NewCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _prompter = prompter;
        _features = features;
        _packagingService = packagingService;
        _configurationService = configurationService;

        Options.Add(s_nameOption);
        Options.Add(s_outputOption);
        Options.Add(s_sourceOption);
        Options.Add(s_versionOption);

        // Customize description based on whether staging channel is enabled
        var isStagingEnabled = _features.IsFeatureEnabled(KnownFeatures.StagingChannelEnabled, false);
        _channelOption = new Option<string?>("--channel")
        {
            Description = isStagingEnabled
                ? NewCommandStrings.ChannelOptionDescriptionWithStaging
                : NewCommandStrings.ChannelOptionDescription,
            Recursive = true
        };
        Options.Add(_channelOption);

        _languageOption = new Option<AppHostLanguage?>("--language")
        {
            Description = NewCommandStrings.LanguageOptionDescription,
            Recursive = true
        };
        Options.Add(_languageOption);

        _templates = templateProvider.GetTemplatesAsync(CancellationToken.None).GetAwaiter().GetResult().ToArray();

        foreach (var template in _templates)
        {
            var templateCommand = new TemplateCommand(template, ExecuteAsync, features, updateNotifier, executionContext, InteractionService, Telemetry);
            Subcommands.Add(templateCommand);
        }
    }

    private string? ParseExplicitLanguageId(ParseResult parseResult)
    {
        return parseResult.GetValue(_languageOption) switch
        {
            AppHostLanguage.CSharp => KnownLanguageId.CSharp,
            AppHostLanguage.TypeScript => KnownLanguageId.TypeScript,
            null => null,
            _ => null
        };
    }

    private static string NormalizeLanguageId(string languageId)
    {
        return languageId.Equals(KnownLanguageId.TypeScriptAlias, StringComparison.OrdinalIgnoreCase)
            ? KnownLanguageId.TypeScript
            : languageId;
    }

    private static string GetLanguageDisplayName(string languageId)
    {
        return NormalizeLanguageId(languageId) switch
        {
            KnownLanguageId.CSharp => KnownLanguageId.CSharpDisplayName,
            KnownLanguageId.TypeScript => "TypeScript (Node.js)",
            _ => languageId
        };
    }

    private async Task<string> PromptForAppHostLanguageAsync(IReadOnlyList<string> selectableLanguages, CancellationToken cancellationToken)
    {
        var choices = selectableLanguages
            .Select(NormalizeLanguageId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static languageId => (LanguageId: languageId, DisplayName: GetLanguageDisplayName(languageId)))
            .ToArray();

        var selected = await InteractionService.PromptForSelectionAsync(
            "Which language would you like to use?",
            choices,
            choice => choice.DisplayName.EscapeMarkup(),
            cancellationToken);

        return selected.LanguageId;
    }

    private async Task<(bool Success, string? LanguageId)> ResolveSelectedLanguageAsync(ITemplate template, ParseResult parseResult, CancellationToken cancellationToken)
    {
        var explicitLanguageId = ParseExplicitLanguageId(parseResult);

        if (template.SelectableAppHostLanguages.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(explicitLanguageId) && !template.SupportsLanguage(explicitLanguageId))
            {
                InteractionService.DisplayError($"Template '{template.Name}' does not support language '{explicitLanguageId}'.");
                return (false, null);
            }

            return (true, explicitLanguageId);
        }

        if (!string.IsNullOrWhiteSpace(explicitLanguageId))
        {
            var normalizedExplicitLanguageId = NormalizeLanguageId(explicitLanguageId);
            if (!template.SelectableAppHostLanguages.Any(l => l.Equals(normalizedExplicitLanguageId, StringComparison.OrdinalIgnoreCase)))
            {
                InteractionService.DisplayError($"Template '{template.Name}' does not support language '{explicitLanguageId}'.");
                return (false, null);
            }

            await _configurationService.SetConfigurationAsync("language", normalizedExplicitLanguageId, isGlobal: false, cancellationToken);
            return (true, normalizedExplicitLanguageId);
        }

        var configuredLanguageId = await _configurationService.GetConfigurationAsync("language", cancellationToken);
        if (!string.IsNullOrWhiteSpace(configuredLanguageId))
        {
            var normalizedConfiguredLanguageId = NormalizeLanguageId(configuredLanguageId);
            if (template.SelectableAppHostLanguages.Any(l => l.Equals(normalizedConfiguredLanguageId, StringComparison.OrdinalIgnoreCase)))
            {
                return (true, normalizedConfiguredLanguageId);
            }
        }

        var selectedLanguageId = await PromptForAppHostLanguageAsync(template.SelectableAppHostLanguages, cancellationToken);
        await _configurationService.SetConfigurationAsync("language", selectedLanguageId, isGlobal: false, cancellationToken);
        return (true, selectedLanguageId);
    }

    private ITemplate[] GetTemplatesForPrompt(ParseResult parseResult)
    {
        var explicitLanguageId = ParseExplicitLanguageId(parseResult);
        var templatesForPrompt = _templates.ToList();

        if (!string.IsNullOrWhiteSpace(explicitLanguageId))
        {
            templatesForPrompt = templatesForPrompt
                .Where(t => t.SupportsLanguage(explicitLanguageId))
                .ToList();
        }

        return templatesForPrompt.ToArray();
    }

    private async Task<ITemplate?> GetProjectTemplateAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // If a subcommand was matched (e.g., aspire new aspire-starter), find the template by command name
        if (parseResult.CommandResult.Command != this)
        {
            var subcommandTemplate = _templates.SingleOrDefault(t => t.Name.Equals(parseResult.CommandResult.Command.Name, StringComparison.OrdinalIgnoreCase));
            if (subcommandTemplate is not null)
            {
                return subcommandTemplate;
            }
        }

        var templatesForPrompt = GetTemplatesForPrompt(parseResult);
        if (templatesForPrompt.Length == 0)
        {
            InteractionService.DisplayError("No templates are available for the current environment.");
            return null;
        }

        return await _prompter.PromptForTemplateAsync(templatesForPrompt, cancellationToken);
    }

    private async Task<string?> ResolveCliTemplateVersionAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var channels = await _packagingService.GetChannelsAsync(cancellationToken);

        var configuredChannelName = parseResult.GetValue(_channelOption);
        if (string.IsNullOrWhiteSpace(configuredChannelName))
        {
            configuredChannelName = await _configurationService.GetConfigurationAsync("channel", cancellationToken);
        }

        var selectedChannel = string.IsNullOrWhiteSpace(configuredChannelName)
            ? channels.FirstOrDefault(c => c.Type is PackageChannelType.Implicit) ?? channels.FirstOrDefault()
            : channels.FirstOrDefault(c => string.Equals(c.Name, configuredChannelName, StringComparison.OrdinalIgnoreCase));

        if (selectedChannel is null)
        {
            if (string.IsNullOrWhiteSpace(configuredChannelName))
            {
                InteractionService.DisplayError("No package channels are available.");
            }
            else
            {
                InteractionService.DisplayError($"No channel found matching '{configuredChannelName}'. Valid options are: {string.Join(", ", channels.Select(c => c.Name))}");
            }

            return null;
        }

        var packages = await selectedChannel.GetTemplatePackagesAsync(ExecutionContext.WorkingDirectory, cancellationToken);
        var package = packages
            .Where(p => Semver.SemVersion.TryParse(p.Version, Semver.SemVersionStyles.Strict, out _))
            .OrderByDescending(p => Semver.SemVersion.Parse(p.Version, Semver.SemVersionStyles.Strict), Semver.SemVersion.PrecedenceComparer)
            .FirstOrDefault();

        if (package is null)
        {
            InteractionService.DisplayError($"No template versions found in channel '{selectedChannel.Name}'.");
            return null;
        }

        return package.Version;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(this.Name);

        var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
        if (template is null)
        {
            return ExitCodeConstants.InvalidCommand;
        }

        var (languageResolutionSuccess, selectedLanguageId) = await ResolveSelectedLanguageAsync(template, parseResult, cancellationToken);
        if (!languageResolutionSuccess)
        {
            return ExitCodeConstants.InvalidCommand;
        }

        var version = parseResult.GetValue(s_versionOption);
        if (ShouldResolveCliTemplateVersion(template) &&
            string.IsNullOrWhiteSpace(version))
        {
            version = await ResolveCliTemplateVersionAsync(parseResult, cancellationToken);
            if (string.IsNullOrWhiteSpace(version))
            {
                return ExitCodeConstants.InvalidCommand;
            }
        }

        var inputs = new TemplateInputs
        {
            Name = parseResult.GetValue(s_nameOption),
            Output = parseResult.GetValue(s_outputOption),
            Source = parseResult.GetValue(s_sourceOption),
            Version = version,
            Channel = parseResult.GetValue(_channelOption),
            Language = selectedLanguageId
        };
        var templateResult = await template.ApplyTemplateAsync(inputs, parseResult, cancellationToken);
        if (templateResult.OutputPath is not null && ExtensionHelper.IsExtensionHost(InteractionService, out var extensionInteractionService, out _))
        {
            extensionInteractionService.OpenEditor(templateResult.OutputPath);
        }

        return templateResult.ExitCode;
    }

    private static bool ShouldResolveCliTemplateVersion(ITemplate template)
    {
        return template.Runtime is TemplateRuntime.Cli;
    }

    private enum AppHostLanguage
    {
        CSharp,
        TypeScript
    }
}

internal interface INewCommandPrompter
{
    Task<ITemplate> PromptForTemplateAsync(ITemplate[] validTemplates, CancellationToken cancellationToken);
    Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken);
    Task<string> PromptForOutputPath(string v, CancellationToken cancellationToken);
}

internal interface ITemplateVersionPrompter
{
    /// <summary>
    /// Prompts the user to select a templates package version.
    /// </summary>
    /// <param name="candidatePackages">The available templates package candidates grouped across channels.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The selected templates package and channel.</returns>
    Task<(NuGetPackage Package, PackageChannel Channel)> PromptForTemplatesVersionAsync(IEnumerable<(NuGetPackage Package, PackageChannel Channel)> candidatePackages, CancellationToken cancellationToken);
}

internal class NewCommandPrompter(IInteractionService interactionService) : INewCommandPrompter, ITemplateVersionPrompter
{
    public virtual async Task<(NuGetPackage Package, PackageChannel Channel)> PromptForTemplatesVersionAsync(IEnumerable<(NuGetPackage Package, PackageChannel Channel)> candidatePackages, CancellationToken cancellationToken)
    {
        // Check if we should skip the channel selection prompt
        // Skip prompt if there are no explicit channels (only the implicit/default channel)
        var byChannel = candidatePackages
            .GroupBy(cp => cp.Channel)
            .ToArray();

        var implicitGroup = byChannel.FirstOrDefault(g => g.Key.Type is Packaging.PackageChannelType.Implicit);
        var explicitGroups = byChannel
            .Where(g => g.Key.Type is Packaging.PackageChannelType.Explicit)
            .ToArray();

        // If there are no explicit channels, automatically select from the implicit channel
        if (explicitGroups.Length == 0 && implicitGroup is not null)
        {
            // Return the highest version from the implicit channel
            return implicitGroup.OrderByDescending(p => Semver.SemVersion.Parse(p.Package.Version), Semver.SemVersion.PrecedenceComparer).First();
        }

        // Create a hierarchical selection experience:
        // - Top-level: all packages from the implicit channel (if any)
        // - Then: one entry per remaining channel that opens a sub-menu with that channel's packages

        // Local helpers
        static string FormatPackageLabel((NuGetPackage Package, PackageChannel Channel) item)
        {
            // Keep it concise: "Version (source)"
            return $"{item.Package.Version.EscapeMarkup()} ({item.Channel.SourceDetails.EscapeMarkup()})";
        }

        async Task<(NuGetPackage Package, PackageChannel Channel)> PromptForChannelPackagesAsync(
            PackageChannel channel,
            IEnumerable<(NuGetPackage Package, PackageChannel Channel)> items,
            CancellationToken ct)
        {
            // Show a sub-menu for this channel's packages
            var packageChoices = items
                .Select(i => (
                    Label: FormatPackageLabel(i),
                    Result: i
                ))
                .ToArray();

            var selection = await interactionService.PromptForSelectionAsync(
                NewCommandStrings.SelectATemplateVersion,
                packageChoices,
                c => c.Label,
                ct);

            return selection.Result;
        }

        // Build the root menu as tuples of (label, action)
        var rootChoices = new List<(string Label, Func<CancellationToken, Task<(NuGetPackage, PackageChannel)>> Action)>();

        if (implicitGroup is not null)
        {
            // Add each implicit package directly to the root
            foreach (var item in implicitGroup)
            {
                var captured = item; // avoid modified-closure issues
                rootChoices.Add((
                    Label: FormatPackageLabel((captured.Package, captured.Channel)),
                    Action: ct => Task.FromResult((captured.Package, captured.Channel))
                ));
            }
        }

        // Add a submenu entry for each explicit channel
        foreach (var channelGroup in explicitGroups)
        {
            var channel = channelGroup.Key;
            var items = channelGroup.ToArray();

            rootChoices.Add((
                Label: channel.Name.EscapeMarkup(),
                Action: ct => PromptForChannelPackagesAsync(channel, items, ct)
            ));
        }

        // If for some reason we have no choices, fall back to the first candidate
        if (rootChoices.Count == 0)
        {
            return candidatePackages.First();
        }

        // Prompt user for the top-level selection
        var topSelection = await interactionService.PromptForSelectionAsync(
            NewCommandStrings.SelectATemplateVersion,
            rootChoices,
            c => c.Label,
            cancellationToken);

        return await topSelection.Action(cancellationToken);
    }

    public virtual async Task<string> PromptForOutputPath(string path, CancellationToken cancellationToken)
    {
        // Escape markup characters in the path to prevent Spectre.Console from trying to parse them as markup
        // when displaying it as the default value in the prompt
        return await interactionService.PromptForStringAsync(
            NewCommandStrings.EnterTheOutputPath,
            defaultValue: path.EscapeMarkup(),
            cancellationToken: cancellationToken
            );
    }

    public virtual async Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken)
    {
        // Escape markup characters in the default name to prevent Spectre.Console from trying to parse them as markup
        // when displaying it as the default value in the prompt
        return await interactionService.PromptForStringAsync(
            NewCommandStrings.EnterTheProjectName,
            defaultValue: defaultName.EscapeMarkup(),
            validator: name => ProjectNameValidator.IsProjectNameValid(name)
                ? ValidationResult.Success()
                : ValidationResult.Error(NewCommandStrings.InvalidProjectName),
            cancellationToken: cancellationToken);
    }

    public virtual async Task<ITemplate> PromptForTemplateAsync(ITemplate[] validTemplates, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForSelectionAsync(
            NewCommandStrings.SelectAProjectTemplate,
            validTemplates,
            t => t.Description.EscapeMarkup(),
            cancellationToken
        );
    }
}

internal static partial class ProjectNameValidator
{
    // Regex for project name validation:
    // - Can be any characters except path separators (/ and \)
    // - Length: 1-254 characters
    // - Must not be empty or whitespace only
    [GeneratedRegex(@"^[^/\\]{1,254}$", RegexOptions.Compiled)]
    internal static partial Regex GetProjectNameRegex();

    public static bool IsProjectNameValid(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return false;
        }

        var regex = GetProjectNameRegex();
        return regex.IsMatch(projectName);
    }
}
