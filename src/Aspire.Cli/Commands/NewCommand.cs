// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.Utils;
using Semver;
namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly IInteractionService _interactionService;

    public NewCommand(IDotNetCliRunner runner, INuGetPackageCache nuGetPackageCache, INewCommandPrompter prompter, IInteractionService interactionService, ICertificateService certificateService)
        : base("new", "Create a new Aspire sample project.")
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(nuGetPackageCache);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(prompter);
        ArgumentNullException.ThrowIfNull(interactionService);

        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _certificateService = certificateService;
        _prompter = prompter;
        _interactionService = interactionService;

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

        if (parseResult.GetValue<string?>("template") is { } templateName &&
            validTemplates.SingleOrDefault(t => t.TemplateName == templateName) is { TemplateName: not null } template)
        {
            return template;
        }
        else
        {
            return await _prompter.PromptForTemplateAsync(validTemplates, cancellationToken);
        }
    }

    private async Task<string> GetProjectNameAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--name") is not { } name)
        {
            var defaultName = new DirectoryInfo(Environment.CurrentDirectory).Name;
            name = await _prompter.PromptForProjectNameAsync(defaultName, cancellationToken);
        }

        return name;
    }

    private async Task<string> GetOutputPathAsync(ParseResult parseResult, string? pathAppendage, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--output") is not { } outputPath)
        {
            outputPath = await _prompter.PromptForOutputPath(pathAppendage ?? ".", cancellationToken);
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

            var orderedCandidatePackages = candidatePackages.OrderByDescending(p => SemVersion.Parse(p.Version), SemVersion.PrecedenceComparer);
            var selectedPackage = await _prompter.PromptForTemplatesVersionAsync(orderedCandidatePackages, cancellationToken);
            return selectedPackage.Version;
        }
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        try
        {
            var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
            var name = await GetProjectNameAsync(parseResult, cancellationToken);
            var outputPath = await GetOutputPathAsync(parseResult, template.PathAppendage, cancellationToken);
            var prerelease = parseResult.GetValue<bool>("--prerelease");
            var source = parseResult.GetValue<string?>("--source");
            var version = await GetProjectTemplatesVersionAsync(parseResult, prerelease, source, cancellationToken);

            var templateInstallCollector = new OutputCollector();
            var templateInstallResult = await _interactionService.ShowStatusAsync<(int ExitCode, string? TemplateVersion)>(
                ":ice:  Getting latest templates...",
                async () => {
                    var options = new DotNetCliRunnerInvocationOptions()
                    {
                        StandardOutputCallback = templateInstallCollector.AppendOutput,
                        StandardErrorCallback = templateInstallCollector.AppendOutput,
                    };

                    var result = await _runner.InstallTemplateAsync("Aspire.ProjectTemplates", version, source, true, options, cancellationToken);
                    return result;
                });

            if (templateInstallResult.ExitCode != 0)
            {
                _interactionService.DisplayLines(templateInstallCollector.GetLines());
                _interactionService.DisplayError($"The template installation failed with exit code {templateInstallResult.ExitCode}. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToInstallTemplates;
            }

            _interactionService.DisplayMessage($"package", $"Using project templates version: {templateInstallResult.TemplateVersion}");

            var newProjectCollector = new OutputCollector();
            var newProjectExitCode = await _interactionService.ShowStatusAsync(
                ":rocket:  Creating new Aspire project...",
                async () => {
                    var options = new DotNetCliRunnerInvocationOptions()
                    {
                        StandardOutputCallback = newProjectCollector.AppendOutput,
                        StandardErrorCallback = newProjectCollector.AppendOutput,
                    };
                    var result = await _runner.NewProjectAsync(
                                template.TemplateName,
                                name,
                                outputPath,
                                options,
                                cancellationToken);
                    return result;
                });

            if (newProjectExitCode != 0)
            {
                _interactionService.DisplayLines(newProjectCollector.GetLines());
                _interactionService.DisplayError($"Project creation failed with exit code {newProjectExitCode}. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);

            _interactionService.DisplaySuccess($"Project created successfully in {outputPath}.");

            return ExitCodeConstants.Success;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.FailedToCreateNewProject;
        }
        catch (CertificateServiceException ex)
        {
            _interactionService.DisplayError($"An error occurred while trusting the certificates: {ex.Message}");
            return ExitCodeConstants.FailedToTrustCertificates;
        }
    }
}

internal interface INewCommandPrompter
{
    Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken);
    Task<(string TemplateName, string TemplateDescription, string? PathAppendage)> PromptForTemplateAsync((string TemplateName, string TemplateDescription, string? PathAppendage)[] validTemplates, CancellationToken cancellationToken);
    Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken);
    Task<string> PromptForOutputPath(string v, CancellationToken cancellationToken);
}

internal class NewCommandPrompter(IInteractionService interactionService) : INewCommandPrompter
{
    public virtual async Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForSelectionAsync(
            "Select a template version:",
            candidatePackages,
            (p) => $"{p.Version} ({p.Source})",
            cancellationToken
            );
    }

    public virtual async Task<string> PromptForOutputPath(string path, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForStringAsync(
            "Enter the output path:",
            defaultValue: path,
            cancellationToken: cancellationToken
            );
    }

    public virtual async Task<string> PromptForProjectNameAsync(string defaultName, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForStringAsync(
            "Enter the project name:",
            defaultValue: defaultName,
            cancellationToken: cancellationToken);
    }

    public virtual async Task<(string TemplateName, string TemplateDescription, string? PathAppendage)> PromptForTemplateAsync((string TemplateName, string TemplateDescription, string? PathAppendage)[] validTemplates, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForSelectionAsync(
            "Select a project template:",
            validTemplates,
            t => $"{t.TemplateName} ({t.TemplateDescription})",
            cancellationToken
        );
    }
}
