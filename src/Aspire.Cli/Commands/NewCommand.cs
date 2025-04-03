// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class NewCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(NewCommand));
    private readonly DotNetCliRunner _runner;

    public NewCommand(DotNetCliRunner runner) : base("new", "Create a new Aspire sample project.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        _runner = runner;

        var templateArgument = new Argument<string>("template");
        templateArgument.Validators.Add(ValidateProjectTemplate);
        templateArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(templateArgument);

        var nameOption = new Option<string>("--name", "-n");
        Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        Options.Add(outputOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        Options.Add(prereleaseOption);
        
        var sourceOption = new Option<string?>("--source", "-s");
        Options.Add(sourceOption);

        var templateVersionOption = new Option<string?>("--version", "-v");
        Options.Add(templateVersionOption);
    }

    private static void ValidateProjectTemplate(ArgumentResult result)
    {
        // TODO: We need to integrate with the template engine to interrogate
        //       the list of available templates. For now we will just hard-code
        //       the acceptable options.
        //
        //       Once we integrate with template engine we will also be able to
        //       interrogate the various options and add them. For now we will 
        //       keep it simple.
        string[] validTemplates = [
            "aspire-starter",
            "aspire",
            "aspire-apphost",
            "aspire-servicedefaults",
            "aspire-mstest",
            "aspire-nunit",
            "aspire-xunit"
            ];

        var value = result.GetValueOrDefault<string>();

        if (value is null)
        {
            // This is OK, for now we will use the default
            // template of aspire-starter, but we might
            // be able to do more intelligent selection in the
            // future based on what is already in the working directory.
            return;
        }

        if (value is { } templateName && !validTemplates.Contains(templateName))
        {
            result.AddError($"The specified template '{templateName}' is not valid. Valid templates are [{string.Join(", ", validTemplates)}].");
            return;
        }
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var templateVersion = parseResult.GetValue<string>("--version");
        var prerelease = parseResult.GetValue<bool>("--prerelease");

        if (templateVersion is not null && prerelease)
        {
            AnsiConsole.MarkupLine("[red bold]:thumbs_down:  The --version and --prerelease options are mutually exclusive.[/]");
            return ExitCodeConstants.FailedToCreateNewProject;
        }
        else if (prerelease)
        {
            templateVersion = "*-*";
        }
        else if (templateVersion is null)
        {
            templateVersion = VersionHelper.GetDefaultTemplateVersion();
        }

        var source = parseResult.GetValue<string?>("--source");

        var templateInstallResult = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(
                ":ice:  Getting latest templates...",
                async context => {
                    return await _runner.InstallTemplateAsync("Aspire.ProjectTemplates", templateVersion!, source, true, cancellationToken);
                });

        if (templateInstallResult.ExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down: The template installation failed with exit code {templateInstallResult.ExitCode}. For more information run with --debug switch.[/]");
            return ExitCodeConstants.FailedToInstallTemplates;
        }

        AnsiConsole.MarkupLine($":package: Using project templates version: {templateInstallResult.TemplateVersion}");

        var templateName = parseResult.GetValue<string>("template") ?? "aspire-starter";

        if (parseResult.GetValue<string>("--output") is not { } outputPath)
        {
            outputPath = Environment.CurrentDirectory;
        }
        else
        {
            outputPath = Path.GetFullPath(outputPath);
        }

        if (parseResult.GetValue<string>("--name") is not { } name)
        {
            var outputPathDirectoryInfo = new DirectoryInfo(outputPath);
            name = outputPathDirectoryInfo.Name;
        }

        int newProjectExitCode = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots3)
            .SpinnerStyle(Style.Parse("purple"))
            .StartAsync(
                ":rocket:  Creating new Aspire project...",
                async context => {
                    return await _runner.NewProjectAsync(
                templateName,
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