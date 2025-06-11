// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Templating;
using Spectre.Console;
namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly IInteractionService _interactionService;
    private readonly IEnumerable<ITemplate> _templates;    public NewCommand(IDotNetCliRunner runner, INuGetPackageCache nuGetPackageCache, INewCommandPrompter prompter, IInteractionService interactionService, ICertificateService certificateService, ITemplateProvider templateProvider)
        : base("new", CliStrings.NewCommand_Description)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(nuGetPackageCache);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(prompter);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(templateProvider);

        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _certificateService = certificateService;
        _prompter = prompter;
        _interactionService = interactionService;        var nameOption = new Option<string>("--name", "-n");
        nameOption.Description = CliStrings.NewCommand_NameOption_Description;
        nameOption.Recursive = true;
        Options.Add(nameOption);        var outputOption = new Option<string?>("--output", "-o");
        outputOption.Description = CliStrings.NewCommand_OutputOption_Description;
        outputOption.Recursive = true;
        Options.Add(outputOption);        var sourceOption = new Option<string?>("--source", "-s");
        sourceOption.Description = CliStrings.NewCommand_SourceOption_Description;
        sourceOption.Recursive = true;
        Options.Add(sourceOption);        var templateVersionOption = new Option<string?>("--version", "-v");
        templateVersionOption.Description = CliStrings.NewCommand_VersionOption_Description;
        templateVersionOption.Recursive = true;
        Options.Add(templateVersionOption);

        _templates = templateProvider.GetTemplates();

        foreach (var template in _templates)
        {
            var templateCommand = new TemplateCommand(template, ExecuteAsync);
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
        using var activity = _activitySource.StartActivity();

        var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
        var exitCode = await template.ApplyTemplateAsync(parseResult, cancellationToken);
        return exitCode;
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
    {        return await interactionService.PromptForSelectionAsync(
            CliStrings.NewCommand_SelectTemplateVersion,
            candidatePackages,
            (p) => $"{p.Version} ({p.Source})",
            cancellationToken
            );
    }

    public virtual async Task<string> PromptForOutputPath(string path, CancellationToken cancellationToken)
    {        return await interactionService.PromptForStringAsync(
            CliStrings.NewCommand_EnterOutputPath,
            defaultValue: path,
            cancellationToken: cancellationToken
            );
    }

    public virtual async Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken)
    {        return await interactionService.PromptForStringAsync(
            CliStrings.NewCommand_EnterProjectName,
            defaultValue: defaultName,
            validator: (name) => {
                return ProjectNameValidator.IsProjectNameValid(name)
                    ? ValidationResult.Success()
                    : ValidationResult.Error(CliStrings.NewCommand_InvalidProjectName);
            },
            cancellationToken: cancellationToken);
    }

    public virtual async Task<ITemplate> PromptForTemplateAsync(ITemplate[] validTemplates, CancellationToken cancellationToken)
    {        return await interactionService.PromptForSelectionAsync(
            CliStrings.NewCommand_SelectProjectTemplate,
            validTemplates,
            t => $"{t.Name} ({t.Description})",
            cancellationToken
        );
    }
}

internal static partial class ProjectNameValidator
{
    [GeneratedRegex(@"^[a-zA-Z0-9_][a-zA-Z0-9_.]{0,253}[a-zA-Z0-9_]$", RegexOptions.Compiled)]
    internal static partial Regex GetAssemblyNameRegex();

    public static bool IsProjectNameValid(string projectName)
    {
        var regex = GetAssemblyNameRegex();
        return regex.IsMatch(projectName);
    }
}