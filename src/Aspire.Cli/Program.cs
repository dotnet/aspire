// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli;

public class Program
{
    private static IHost BuildApplication(ParseResult parseResult)
    {
        var builder = Host.CreateApplicationBuilder();

        var debugOption = parseResult.GetValue<bool>("--debug");

        if (!debugOption)
        {
            builder.Logging.ClearProviders();
        }

        builder.Services.AddTransient<AppHostRunner>();
        builder.Services.AddTransient<DotNetCliRunner>();
        builder.Services.AddSingleton<CliRpcTarget>();
        builder.Services.AddTransient<IIntegrationLookup, IntegrationLookup>();
        var app = builder.Build();
        return app;
    }

    private static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand(".NET Aspire CLI");
        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Recursive = true;
        rootCommand.Options.Add(debugOption);
        ConfigureDevCommand(rootCommand);
        ConfigurePublishCommand(rootCommand);
        ConfigureNewCommand(rootCommand);
        ConfigureAddCommand(rootCommand);
        return rootCommand;
    }

    private static void ValidateProjectOption(OptionResult result)
    {
        var value = result.GetValueOrDefault<FileInfo?>();

        if (result.Implicit)
        {
            // Having no value here is fine, but there has to
            // be a single csproj file in the current
            // working directory.
            var csprojFiles = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");

            if (csprojFiles.Length > 1)
            {
                result.AddError("The --project option was not specified and multiple *.csproj files were detected.");
                return;
            }
            else if (csprojFiles.Length == 0)
            {
                result.AddError("The --project option was not specified and no *.csproj files were detected.");
                return;
            }

            return;
        }

        if (value is null)
        {
            result.AddError("The --project option was specified but no path was provided.");
            return;
        }

        if (!File.Exists(value.FullName))
        {
            result.AddError("The specified project file does not exist.");
            return;
        }
    }

    private static void ConfigureDevCommand(Command parentCommand)
    {
        var command = new Command("dev", "Run a .NET Aspire AppHost project in development mode.");

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var runner = app.Services.GetRequiredService<AppHostRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);

            var exitCode = await runner.RunAppHostAsync(effectiveAppHostProjectFile, Array.Empty<string>(), ct).ConfigureAwait(false);

            return exitCode;
        });

        parentCommand.Subcommands.Add(command);
    }

    private static FileInfo UseOrFindAppHostProjectFile(FileInfo? projectFile)
    {
        if (projectFile is not null)
        {
            // If the project file is passed, just use it.
            return projectFile;
        }

        var projectFilePaths = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");
        try 
        {
            var projectFilePath = projectFilePaths?.SingleOrDefault();
            if (projectFilePath is null)
            {
                throw new InvalidOperationException("No project file found.");
            }
            else
            {
                return new FileInfo(projectFilePath);
            }
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            if (projectFilePaths.Length > 1)
            {
                AnsiConsole.MarkupLine("[red bold]The --project option was not specified and multiple *.csproj files were detected.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red bold]The --project option was not specified and no *.csproj files were detected.[/]");
            }
            return new FileInfo(Environment.CurrentDirectory);
        };
    }

    private static void ConfigurePublishCommand(Command parentCommand)
    {
        var command = new Command("publish", "Publish a .NET Aspire AppHost project.");

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var targetOption = new Option<string>("--target", "-t");
        command.Options.Add(targetOption);

        var outputPath = new Option<string>("--output-path", "-o");
        command.Options.Add(outputPath);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var runner = app.Services.GetRequiredService<AppHostRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            var target = parseResult.GetValue<string>("--target");
            var outputPath = parseResult.GetValue<string>("--output-path");
            var exitCode = await runner.RunAppHostAsync(effectiveAppHostProjectFile, ["--publisher", target ?? "manifest", "--output-path", outputPath ?? "."], ct).ConfigureAwait(false);

            return exitCode;
        });

        parentCommand.Subcommands.Add(command);
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

    private static void ConfigureNewCommand(Command parentCommand)
    {
        var command = new Command("new", "Create a new .NET Aspire-related project.");
        var templateArgument = new Argument<string>("template");
        templateArgument.Validators.Add(ValidateProjectTemplate);
        templateArgument.Arity = ArgumentArity.ZeroOrOne;
        command.Arguments.Add(templateArgument);

        var nameOption = new Option<string>("--name", "-n");
        command.Options.Add(nameOption);

        var outputOption = new Option<string?>("--output", "-o");
        command.Options.Add(outputOption);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication(parseResult);
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var cliRunner = app.Services.GetRequiredService<DotNetCliRunner>();
            var templateInstallExitCode = await cliRunner.InstallTemplateAsync("Aspire.ProjectTemplates", "*-*", true, ct).ConfigureAwait(false);

            if (templateInstallExitCode != 0)
            {
                return ExitCodeConstants.FailedToInstallTemplates;
            }

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

            var newProjectExitCode = await cliRunner.NewProjectAsync(
                templateName,
                name,
                outputPath,
                ct).ConfigureAwait(false);

            if (newProjectExitCode != 0)
            {
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            return 0;
        });

        parentCommand.Subcommands.Add(command);
    }

    private static Integration GetIntegrationByInteractiveFlow(IEnumerable<Integration> knownIntegrations)
    {
        // HACK: Will be adding interactivity soon.
        return knownIntegrations.First();
    }

    private static void ConfigureAddCommand(Command parentCommand)
    {
        var command = new Command("add", "Add a resource to the .NET Aspire project.");
        var resourceArgument = new Argument<string>("resource");
        command.Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var nameOption = new Option<string?>("--name", "-n");
        command.Options.Add(nameOption);

        command.SetAction(async (parseResult, ct) => {
            var app = BuildApplication(parseResult);
            var integrationLookup = app.Services.GetRequiredService<IIntegrationLookup>();

            var integrationName = parseResult.GetValue<string>("resource");
            var integrations = integrationLookup.GetIntegrations();
            var selectedIntegration = integrations.SingleOrDefault(i => i.PackageShortName == integrationName || i.PackageName == integrationName);

            if (selectedIntegration is null)
            {
                selectedIntegration = GetIntegrationByInteractiveFlow(integrations);
            }

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = UseOrFindAppHostProjectFile(passedAppHostProjectFile);

            var runner = app.Services.GetRequiredService<DotNetCliRunner>();
            var addPackageResult = await runner.AddPackageAsync(
                effectiveAppHostProjectFile.FullName,
                selectedIntegration.PackageName,
                selectedIntegration.PackageVersion,
                ct
                ).ConfigureAwait(false);

            if (addPackageResult != 0)
            {
                return ExitCodeConstants.FailedToAddPackage;
            }

            // HACK: This is really crude, we should use Roslyn here but this is
            //       just for this spike.
            var resourceName = parseResult.GetValue<string?>("--name");
            var snippet = selectedIntegration.AppHostSnippet(resourceName);

            var appHostEntryPoint = Path.Combine(
                effectiveAppHostProjectFile.DirectoryName!,
                "Program.cs"
            );

            if (File.Exists(appHostEntryPoint))
            {
                var lines = File.ReadAllLines(appHostEntryPoint);
                var newLines = new List<string>(lines.Length + 1);
                foreach (var line in lines)
                {
                    newLines.Add(line);
                    if (line.Contains("DistributedApplication.CreateBuilder"))
                    {
                        newLines.Add(snippet);
                    }
                }

                File.WriteAllLines(appHostEntryPoint, newLines);
            }

            return 0;
        });

        parentCommand.Subcommands.Add(command);
    }

    public static async Task<int> Main(string[] args)
    {
        var rootCommand = GetRootCommand();
        var result = rootCommand.Parse(args);
        var exitCode = await result.InvokeAsync().ConfigureAwait(false);
        return exitCode;
    }
}
