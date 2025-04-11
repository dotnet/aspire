// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;

    public NewCommand(IDotNetCliRunner runner, INuGetPackageCache nuGetPackageCache, IInteractionService interactionService, ICertificateService certificateService)
        : base("new", "Create a new Aspire sample project.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        ArgumentNullException.ThrowIfNull(nuGetPackageCache, nameof(nuGetPackageCache));
        ArgumentNullException.ThrowIfNull(interactionService, nameof(interactionService));
        ArgumentNullException.ThrowIfNull(certificateService, nameof(certificateService));

        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _interactionService = interactionService;
        _certificateService = certificateService;

        var templateArgument = new Argument<string>("template");
        templateArgument.Description = "The name of the project template to use (e.g. aspire-starter, aspire).";
        templateArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(templateArgument);

        var nameOption = new Option<string>("--name", "-n");
        nameOption.Description = "The name of the project to create.";
        Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        outputOption.Description = "The output path for the project.";
        Options.Add(outputOption);
        
        var sourceOption = new Option<string?>("--source", "-s");
        sourceOption.Description = "The NuGet source to use for the project templates.";
        Options.Add(sourceOption);

        var templateVersionOption = new Option<string?>("--version", "-v");
        templateVersionOption.Description = "The version of the project templates to use.";
        Options.Add(templateVersionOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        prereleaseOption.Description = "Include prerelease versions when searching for project templates.";
        Options.Add(prereleaseOption);
    }

    private async Task<(string TemplateName, string TemplateDescription, string? PathAppendage)> GetProjectTemplateAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // TODO: We need to integrate with the template engine to interrogate
        //       the list of available templates. For now we will just hard-code
        //       the acceptable options.
        //
        //       Once we integrate with template engine we will also be able to
        //       interrogate the various options and add them. For now we will 
        //       keep it simple.
        (string TemplateName, string TemplateDescription, string? PathAppendage)[] validTemplates = [
            ("aspire-starter", "Aspire Starter App", "./src") ,
            ("aspire", "Aspire Empty App", "./src"),
            ("aspire-apphost", "Aspire App Host", "./"),
            ("aspire-servicedefaults", "Aspire Service Defaults", "./"),
            ("aspire-mstest", "Aspire Test Project (MSTest)", "./"),
            ("aspire-nunit", "Aspire Test Project (NUnit)", "./"),
            ("aspire-xunit", "Aspire Test Project (xUnit)", "./")
            ];

        if (parseResult.GetValue<string?>("template") is { } templateName && validTemplates.SingleOrDefault(t => t.TemplateName == templateName) is { } template)
        {
            return template;
        }
        else
        {
            return await _interactionService.PromptForSelectionAsync(
                "Select a project template:",
                validTemplates,
                t => $"{t.TemplateName} ({t.TemplateDescription})",
                cancellationToken
                );
        }
    }

    private async Task<string> GetProjectNameAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--name") is not { } name)
        {
            var defaultName = new DirectoryInfo(Environment.CurrentDirectory).Name;
            name = await _interactionService.PromptForStringAsync(
                "Enter the project name:",
                defaultValue: defaultName,
                cancellationToken: cancellationToken);
        }

        return name;
    }

    private async Task<string> GetOutputPathAsync(ParseResult parseResult, string? pathAppendage, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--output") is not { } outputPath)
        {
            outputPath = await _interactionService.PromptForStringAsync(
                "Enter the output path:",
                defaultValue: pathAppendage ?? ".",
                cancellationToken: cancellationToken
                );
        }

        return Path.GetFullPath(outputPath);
    }

    private async Task<string> GetProjectTemplatesVersionAsync(ParseResult parseResult, bool prerelease, string? source, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--version") is { } version)
        {
            return version;
        }
        else
        {
            var workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            var candidatePackages = await _interactionService.ShowStatusAsync(
                "Searching for available project template versions...",
                () => _nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, prerelease, source, cancellationToken)
                );

            var selectedPackage = await _interactionService.PromptForTemplatesVersionAsync(candidatePackages, cancellationToken);
            return selectedPackage.Version;
        }
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
        var name = await GetProjectNameAsync(parseResult, cancellationToken);
        var outputPath = await GetOutputPathAsync(parseResult, template.PathAppendage, cancellationToken);
        var prerelease = parseResult.GetValue<bool>("--prerelease");
        var source = parseResult.GetValue<string?>("--source");
        var version = await GetProjectTemplatesVersionAsync(parseResult, prerelease, source, cancellationToken);

        var templateInstallResult = await _interactionService.ShowStatusAsync(
            ":ice:  Getting latest templates...",
            () => _runner.InstallTemplateAsync("Aspire.ProjectTemplates", version, source, true, cancellationToken));

        if (templateInstallResult.ExitCode != 0)
        {
            _interactionService.DisplayError($"The template installation failed with exit code {templateInstallResult.ExitCode}. For more information run with --debug switch.");
            return ExitCodeConstants.FailedToInstallTemplates;
        }

        _interactionService.DisplayMessage($"package", $"Using project templates version: {templateInstallResult.TemplateVersion}");

        var newProjectExitCode = await _interactionService.ShowStatusAsync(
            ":rocket:  Creating new Aspire project...",
            () => _runner.NewProjectAsync(
                        template.TemplateName,
                        name,
                        outputPath,
                        cancellationToken));

        if (newProjectExitCode != 0)
        {
           _interactionService.DisplayError($"Project creation failed with exit code {newProjectExitCode}. For more information run with --debug switch.");
            return ExitCodeConstants.FailedToCreateNewProject;
        }

        try
        {
            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError($"An error occurred while trusting the certificates: {ex.Message}");
            return ExitCodeConstants.FailedToTrustCertificates;
        }

        _interactionService.DisplaySuccess($"Project created successfully in {outputPath}.");

        return ExitCodeConstants.Success;
    }
}