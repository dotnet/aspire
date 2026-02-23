// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Scaffolding;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly IEnumerable<ITemplate> _templates;
    private readonly IDotNetSdkInstaller _sdkInstaller;
    private readonly ICliHostEnvironment _hostEnvironment;
    private readonly IFeatures _features;
    private readonly ICliUpdateNotifier _updateNotifier;
    private readonly CliExecutionContext _executionContext;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IScaffoldingService _scaffoldingService;

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
    private static readonly Option<string?> s_versionOption = new("--version", "-v")
    {
        Description = NewCommandStrings.VersionArgumentDescription,
        Recursive = true
    };

    private readonly Option<string?> _channelOption;
    private readonly Option<string?>? _languageOption;
    private readonly Option<string?>? _templateOption;
    private readonly IGitTemplateService? _gitTemplateService;
    private readonly ILogger<NewCommand> _logger;

    /// <summary>
    /// NewCommand prefetches both template and CLI package metadata.
    /// </summary>
    public bool PrefetchesTemplatePackageMetadata => true;

    /// <summary>
    /// NewCommand prefetches CLI package metadata for update notifications.
    /// </summary>
    public bool PrefetchesCliPackageMetadata => true;

    public NewCommand(
        IDotNetCliRunner runner,
        INuGetPackageCache nuGetPackageCache,
        INewCommandPrompter prompter,
        IInteractionService interactionService,
        ICertificateService certificateService,
        ITemplateProvider templateProvider,
        AspireCliTelemetry telemetry,
        IDotNetSdkInstaller sdkInstaller,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ICliHostEnvironment hostEnvironment,
        ILanguageDiscovery languageDiscovery,
        IScaffoldingService scaffoldingService,
        IGitTemplateService gitTemplateService,
        ILogger<NewCommand> logger)
        : base("new", NewCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _certificateService = certificateService;
        _prompter = prompter;
        _sdkInstaller = sdkInstaller;
        _hostEnvironment = hostEnvironment;
        _features = features;
        _updateNotifier = updateNotifier;
        _executionContext = executionContext;
        _languageDiscovery = languageDiscovery;
        _scaffoldingService = scaffoldingService;
        _gitTemplateService = gitTemplateService;
        _logger = logger;

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

        // Only add --language option when polyglot support is enabled
        if (_features.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false))
        {
            _languageOption = new Option<string?>("--language", "-l")
            {
                Description = "The programming language for the AppHost (csharp, typescript)"
            };
            Options.Add(_languageOption);
        }

        // Only add --template option when git templates feature is enabled
        if (_features.IsFeatureEnabled(KnownFeatures.GitTemplatesEnabled, false))
        {
            _templateOption = new Option<string?>("--template")
            {
                Description = "A local path or Git URL to use as a project template"
            };
            Options.Add(_templateOption);
        }

        _templates = templateProvider.GetTemplates();

        foreach (var template in _templates)
        {
            var templateCommand = new TemplateCommand(template, ExecuteAsync, _features, _updateNotifier, _executionContext, InteractionService, Telemetry);
            Subcommands.Add(templateCommand);
        }
    }

    private async Task<ITemplate> GetProjectTemplateAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // NOTE: I am using Single(...) here because if we get to this point and we are not running the 'aspire new' without a template
        //       specified then we should have errored out with the help text. If we get there then someting is really wrong and we should
        //       throw an exception.
        if (parseResult.CommandResult.Command != this && _templates.Single(t => t.Name.Equals(parseResult.CommandResult.Command.Name, StringComparison.OrdinalIgnoreCase)) is ITemplate template)
        {
            // If the command is not this NewCommand instance then we assume
            // that we are using a generated TemplateCommand. If this is the case
            // we return the template based on the command name - otherwise we prompt for it.
            return template;
        }

        return await _prompter.PromptForTemplateAsync(_templates.ToArray(), cancellationToken);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(this.Name);

        // Check for git template mode first (highest priority when enabled)
        if (_features.IsFeatureEnabled(KnownFeatures.GitTemplatesEnabled, false) && _templateOption is not null)
        {
            var templateValue = parseResult.GetValue(_templateOption);
            if (!string.IsNullOrWhiteSpace(templateValue))
            {
                return await ApplyGitTemplateAsync(parseResult, templateValue, cancellationToken);
            }
        }

        // Only check for language option when polyglot support is enabled
        if (_features.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false))
        {
            // Check if language is explicitly specified
            Debug.Assert(_languageOption is not null);
            var explicitLanguage = parseResult.GetValue(_languageOption);

            // If a non-C# language is specified, create polyglot apphost
            if (!string.IsNullOrWhiteSpace(explicitLanguage) &&
                !explicitLanguage.Equals(KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase))
            {
                var language = _languageDiscovery.GetLanguageById(explicitLanguage);
                if (language is null)
                {
                    InteractionService.DisplayError($"Unknown language: '{explicitLanguage}'");
                    return ExitCodeConstants.InvalidCommand;
                }
                return await CreatePolyglotProjectAsync(parseResult, language, cancellationToken);
            }
        }

        // For C# or unspecified language, use the existing template system
        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, InteractionService, _features, Telemetry, _hostEnvironment, cancellationToken))
        {
            return ExitCodeConstants.SdkNotInstalled;
        }

        var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
        var inputs = new TemplateInputs
        {
            Name = parseResult.GetValue(s_nameOption),
            Output = parseResult.GetValue(s_outputOption),
            Source = parseResult.GetValue(s_sourceOption),
            Version = parseResult.GetValue(s_versionOption),
            Channel = parseResult.GetValue(_channelOption)
        };
        var templateResult = await template.ApplyTemplateAsync(inputs, parseResult, cancellationToken);
        if (templateResult.OutputPath is not null && ExtensionHelper.IsExtensionHost(InteractionService, out var extensionInteractionService, out _))
        {
            extensionInteractionService.OpenEditor(templateResult.OutputPath);
        }

        return templateResult.ExitCode;
    }

    private async Task<int> ApplyGitTemplateAsync(ParseResult parseResult, string templatePathOrUrl, CancellationToken cancellationToken)
    {
        Debug.Assert(_gitTemplateService is not null);

        var outputPath = parseResult.GetValue(s_outputOption);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = await _prompter.PromptForDestinationPathAsync(Environment.CurrentDirectory, cancellationToken);
        }

        _logger.LogDebug("Applying git template from '{TemplatePathOrUrl}' to '{OutputPath}'", templatePathOrUrl, outputPath);

        var result = await _gitTemplateService.ApplyGitTemplateAsync(templatePathOrUrl, outputPath, cancellationToken);

        if (result == ExitCodeConstants.Success &&
            ExtensionHelper.IsExtensionHost(InteractionService, out var extensionInteractionService, out _))
        {
            extensionInteractionService.OpenEditor(outputPath);
        }

        return result;
    }

    private async Task<int> CreatePolyglotProjectAsync(ParseResult parseResult, LanguageInfo language, CancellationToken cancellationToken)
    {
        // Get project name
        var projectName = parseResult.GetValue(s_nameOption);
        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = await _prompter.PromptForProjectNameAsync("AspireApp", cancellationToken);
        }

        // Get output directory
        var outputPath = parseResult.GetValue(s_outputOption);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(_executionContext.WorkingDirectory.FullName, projectName);
        }
        else if (!Path.IsPathRooted(outputPath))
        {
            outputPath = Path.Combine(_executionContext.WorkingDirectory.FullName, outputPath);
        }

        // Create the output directory
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var directory = new DirectoryInfo(outputPath);

        // Scaffold the apphost files
        var context = new ScaffoldContext(language, directory, projectName);
        await _scaffoldingService.ScaffoldAsync(context, cancellationToken);

        InteractionService.DisplaySuccess($"Created {language.DisplayName} project at {outputPath}");
        InteractionService.DisplayMessage("information", "Run 'aspire run' to start your AppHost.");

        if (ExtensionHelper.IsExtensionHost(InteractionService, out var extensionInteractionService, out _))
        {
            extensionInteractionService.OpenEditor(outputPath);
        }

        return ExitCodeConstants.Success;
    }
}

internal interface INewCommandPrompter
{
    Task<(NuGetPackage Package, PackageChannel Channel)> PromptForTemplatesVersionAsync(IEnumerable<(NuGetPackage Package, PackageChannel Channel)> candidatePackages, CancellationToken cancellationToken);
    Task<ITemplate> PromptForTemplateAsync(ITemplate[] validTemplates, CancellationToken cancellationToken);
    Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken);
    Task<string> PromptForOutputPath(string v, CancellationToken cancellationToken);
    Task<string> PromptForDestinationPathAsync(string defaultPath, CancellationToken cancellationToken);
}

internal class NewCommandPrompter(IInteractionService interactionService) : INewCommandPrompter
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

    public virtual async Task<string> PromptForDestinationPathAsync(string defaultPath, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForStringAsync(
            "Enter the destination path",
            defaultValue: defaultPath.EscapeMarkup(),
            cancellationToken: cancellationToken);
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
