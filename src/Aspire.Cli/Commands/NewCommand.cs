// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly DotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;

    public NewCommand(DotNetCliRunner runner, INuGetPackageCache nuGetPackageCache)
        : base("new", "Create a new Aspire sample project.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        ArgumentNullException.ThrowIfNull(nuGetPackageCache, nameof(nuGetPackageCache));
        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;

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

    private static async Task<(string TemplateName, string TemplateDescription, string? PathAppendage)> GetProjectTemplateAsync(ParseResult parseResult, CancellationToken cancellationToken)
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
            return await InteractionUtils.PromptForSelectionAsync(
                "Select a project template:",
                validTemplates,
                t => $"{t.TemplateName} ({t.TemplateDescription})",
                cancellationToken
                );
        }
    }

    private static async Task<string> GetProjectNameAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--name") is not { } name)
        {
            var defaultName = new DirectoryInfo(Environment.CurrentDirectory).Name;
            name = await InteractionUtils.PromptForStringAsync("Enter the project name:",
                defaultValue: defaultName,
                cancellationToken: cancellationToken);
        }

        return name;
    }

    private static async Task<string> GetOutputPathAsync(ParseResult parseResult, string? pathAppendage, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--output") is not { } outputPath)
        {
            outputPath = await InteractionUtils.PromptForStringAsync(
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

            var candidatePackages = await InteractionUtils.ShowStatusAsync(
                "Searching for available project template versions...",
                () => _nuGetPackageCache.GetTemplatePackagesAsync(workingDirectory, prerelease, source, cancellationToken)
                );

            var selectedPackage = await InteractionUtils.PromptForTemplatesVersionAsync(candidatePackages, cancellationToken);
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

        var templateInstallResult = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(
                ":ice:  Getting latest templates...",
                async context => {
                    return await _runner.InstallTemplateAsync("Aspire.ProjectTemplates", version, source, true, cancellationToken);
                });

        if (templateInstallResult.ExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down: The template installation failed with exit code {templateInstallResult.ExitCode}. For more information run with --debug switch.[/]");
            return ExitCodeConstants.FailedToInstallTemplates;
        }

        AnsiConsole.MarkupLine($":package: Using project templates version: {templateInstallResult.TemplateVersion}");

        int newProjectExitCode = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(
                ":rocket:  Creating new Aspire project...",
                async context => {
                    return await _runner.NewProjectAsync(
                        template.TemplateName,
                        name,
                        outputPath,
                        cancellationToken);
                });

        if (newProjectExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down: Project creation failed with exit code {newProjectExitCode}. For more information run with --debug switch.[/]");
            return ExitCodeConstants.FailedToCreateNewProject;
        }

        try
        {
            await CertificatesHelper.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  An error occurred while trusting the certificates: {ex.Message}[/]");
            return ExitCodeConstants.FailedToTrustCertificates;
        }

        AnsiConsole.MarkupLine($":thumbs_up: Project created successfully in {outputPath}.");

        return ExitCodeConstants.Success;
    }
}