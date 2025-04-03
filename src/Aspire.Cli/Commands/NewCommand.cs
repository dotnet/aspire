// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Utils;
using Semver;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly DotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;

    public NewCommand(DotNetCliRunner runner, INuGetPackageCache nuGetPackageCache) : base("new", "Create a new Aspire sample project.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        ArgumentNullException.ThrowIfNull(nuGetPackageCache, nameof(nuGetPackageCache));
        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;

        var templateArgument = new Argument<string>("template");
        templateArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(templateArgument);

        var nameOption = new Option<string>("--name", "-n");
        Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        Options.Add(outputOption);
        
        var sourceOption = new Option<string?>("--source", "-s");
        Options.Add(sourceOption);

        var templateVersionOption = new Option<string?>("--version", "-v");
        Options.Add(templateVersionOption);
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
            ("aspire-starter", "Aspire Starter App", "src") ,
            ("aspire", "Aspire Empty App", "src"),
            ("aspire-apphost", "Aspire App Host", null),
            ("aspire-servicedefaults", "Aspire Service Defaults", null),
            ("aspire-mstest", "Aspire Test Project (MSTest)", null),
            ("aspire-nunit", "Aspire Test Project (NUnit)", null),
            ("aspire-xunit", "Aspire Test Project (xUnit)", null)
            ];

        if (parseResult.GetValue<string?>("template") is { } templateName && validTemplates.SingleOrDefault(t => t.TemplateName == templateName) is { } template)
        {
            return template;
        }
        else
        {
            return await PromptUtils.PromptForSelectionAsync(
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
            name = await PromptUtils.PromptForStringAsync("Enter the project name:",
                defaultValue: defaultName,
                cancellationToken: cancellationToken);
        }

        return name;
    }

    private static async Task<string> GetOutputPathAsync(ParseResult parseResult, string? pathAppendage, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--output") is not { } outputPath)
        {
            outputPath = await PromptUtils.PromptForStringAsync(
                "Enter the output path:",
                defaultValue: Path.Combine(Environment.CurrentDirectory, pathAppendage ?? string.Empty),
                cancellationToken: cancellationToken
                );
        }

        return Path.GetFullPath(outputPath);
    }

    private static async Task<string> GetProjectTemplatesVersionAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue<string>("--version") is { } version)
        {
            return version;
        }
        else
        {
            version = await PromptUtils.PromptForStringAsync(
                "Project templates version:",
                defaultValue: VersionHelper.GetDefaultTemplateVersion(),
                validator: (string value) => {
                    if (SemVersion.TryParse(value, out var parsedVersion))
                    {
                        return ValidationResult.Success();
                    }

                    return ValidationResult.Error("Invalid version format. Please enter a valid version.");
                },
                cancellationToken);

            return version;
        }
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var template = await GetProjectTemplateAsync(parseResult, cancellationToken);
        var name = await GetProjectNameAsync(parseResult, cancellationToken);
        var outputPath = await GetOutputPathAsync(parseResult, template.PathAppendage, cancellationToken);
        var version = await GetProjectTemplatesVersionAsync(parseResult, cancellationToken);
        var source = parseResult.GetValue<string?>("--source");

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