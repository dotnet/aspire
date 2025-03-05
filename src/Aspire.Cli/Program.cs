// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Cli;

public class Program
{
    private static IHost BuildApplication()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddTransient<AppHostRunner>();
        var app = builder.Build();
        return app;
    }

    private static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand(".NET Aspire CLI");
        ConfigureDevCommand(rootCommand);
        ConfigurePackCommand(rootCommand);
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
            using var app = BuildApplication();
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
        var projectFilePath = projectFilePaths.Single();
        return new FileInfo(projectFilePath);
    }

    private static void ConfigurePackCommand(Command parentCommand)
    {
        var command = new Command("pack", "Pack a .NET Aspire AppHost project.");

        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Validators.Add(ValidateProjectOption);
        command.Options.Add(projectOption);

        var targetOption = new Option<string>("--target", "-t");
        command.Options.Add(targetOption);

        var outputPath = new Option<string>("--output-path", "-o");
        command.Options.Add(outputPath);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication();
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

    public static async Task<int> Main(string[] args)
    {
        var rootCommand = GetRootCommand();
        var result = rootCommand.Parse(args);
        var exitCode = await result.InvokeAsync().ConfigureAwait(false);
        return exitCode;
    }
}
