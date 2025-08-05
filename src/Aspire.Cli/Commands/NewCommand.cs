// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.RegularExpressions;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;
using Semver;
using Spectre.Console;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;

namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly IInteractionService _interactionService;
    private readonly IEnumerable<ITemplate> _templates;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IDotNetSdkInstaller _sdkInstaller;

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
        ICliUpdateNotifier updateNotifier)
        : base("new", NewCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(nuGetPackageCache);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(prompter);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(templateProvider);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(sdkInstaller);

        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _certificateService = certificateService;
        _prompter = prompter;
        _interactionService = interactionService;
        _telemetry = telemetry;
        _sdkInstaller = sdkInstaller;

        var nameOption = new Option<string>("--name", "-n");
        nameOption.Description = NewCommandStrings.NameArgumentDescription;
        nameOption.Recursive = true;
        Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        outputOption.Description = NewCommandStrings.OutputArgumentDescription;
        outputOption.Recursive = true;
        Options.Add(outputOption);

        var sourceOption = new Option<string?>("--source", "-s");
        sourceOption.Description = NewCommandStrings.SourceArgumentDescription;
        sourceOption.Recursive = true;
        Options.Add(sourceOption);

        var templateVersionOption = new Option<string?>("--version", "-v");
        templateVersionOption.Description = NewCommandStrings.VersionArgumentDescription;
        templateVersionOption.Recursive = true;
        Options.Add(templateVersionOption);

        _templates = templateProvider.GetTemplates();

        foreach (var template in _templates)
        {
            var templateCommand = new TemplateCommand(template, ExecuteAsync, features, updateNotifier);
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
        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, _interactionService, cancellationToken))
        {
            return ExitCodeConstants.SdkNotInstalled;
        }

        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
        var templateResult = await template.ApplyTemplateAsync(parseResult, cancellationToken);
        if (templateResult.OutputPath is not null && ExtensionHelper.IsExtensionHost(_interactionService, out var extensionInteractionService, out _))
        {
            extensionInteractionService.OpenNewProject(templateResult.OutputPath);
        }

        return templateResult.ExitCode;
    }
}

internal interface INewCommandPrompter
{
    Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken);
    Task<ITemplate> PromptForTemplateAsync(ITemplate[] validTemplates, CancellationToken cancellationToken);
    Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken);
    Task<string> PromptForOutputPath(string v, CancellationToken cancellationToken);
}

internal class NewCommandPrompter(IInteractionService interactionService) : INewCommandPrompter
{
    public virtual async Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken)
    {
        var packagesGroupedByReleaseStatus = candidatePackages.GroupBy(p => SemVersion.Parse(p.Version).IsPrerelease ? "Prerelease" : "Released");
        var releasedGroup = packagesGroupedByReleaseStatus.FirstOrDefault(g => g.Key == "Released");
        var prereleaseGroup = packagesGroupedByReleaseStatus.FirstOrDefault(g => g.Key == "Prerelease");

        var selections = new List<(string SelectionText, Func<Task<NuGetPackage>> PackageSelector)>();

        foreach (var releasedPackage in releasedGroup ?? Enumerable.Empty<NuGetPackage>())
        {
            selections.Add(($"{releasedPackage.Version} ({releasedPackage.Source})", () => Task.FromResult(releasedPackage!)));
        }

        if (releasedGroup is not null && prereleaseGroup is not null)
        {
            // If we have prerelease packages (and there are released packages) we
            // want to show a sub-menu option which we will use to prompt the user.
            // To make this work the first prompt returns a function which is invoke
            // which will either return the package or trigger another prompt for
            // sub-packages. This is the sub-prompt logic.
            selections.Add((NewCommandStrings.UsePrereleaseTemplates, async () =>
            {
                return await interactionService.PromptForSelectionAsync(
                     NewCommandStrings.SelectATemplateVersion,
                     prereleaseGroup,
                     (p) => $"{p.Version} ({p.Source})",
                     cancellationToken
                     );
            }
            ));
        }
        else if (prereleaseGroup is not null)
        {
            // Fallback behavior if we happen to have NuGet feeds configured such
            // that we only have access to prerelease template packages - in this
            // case we just want to display them rather than having a special
            // expander menu.
            foreach (var prereleasePackage in prereleaseGroup)
            {
                selections.Add(($"{prereleasePackage.Version} ({prereleasePackage.Source})", () => Task.FromResult(prereleasePackage)));
            }
        }

        var selection = await interactionService.PromptForSelectionAsync(
                    NewCommandStrings.SelectATemplateVersion,
                    selections,
                    s => s.SelectionText,
                    cancellationToken
                    );

        var package = await selection.PackageSelector();
        return package;
    }

    public virtual async Task<string> PromptForOutputPath(string path, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForStringAsync(
            NewCommandStrings.EnterTheOutputPath,
            defaultValue: path,
            cancellationToken: cancellationToken
            );
    }

    public virtual async Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForStringAsync(
            NewCommandStrings.EnterTheProjectName,
            defaultValue: defaultName,
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
            t => t.Description,
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
