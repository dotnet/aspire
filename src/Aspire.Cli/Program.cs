﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
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

    private static void ConfigureDevCommand(Command parentCommand)
    {
        var command = new Command("dev", "Run a .NET Aspire AppHost project in development mode.");

        var projectArgument = new Argument<FileInfo>("project").AcceptExistingOnly();
        command.Arguments.Add(projectArgument);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication();
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var runner = app.Services.GetRequiredService<AppHostRunner>();
            var appHostProjectFile = parseResult.GetValue<FileInfo>("project");
            var exitCode = await runner.RunAppHostAsync(appHostProjectFile!, [], ct).ConfigureAwait(false);

            return exitCode;
        });

        parentCommand.Subcommands.Add(command);
    }

    private static void ConfigurePackCommand(Command parentCommand)
    {
        var command = new Command("pack", "Pack a .NET Aspire AppHost project.");

        var projectArgument = new Argument<FileInfo>("project").AcceptExistingOnly();
        command.Arguments.Add(projectArgument);

        var targetOption = new Option<string>("--target", "-t");
        command.Options.Add(targetOption);

        var outputPath = new Option<string>("--output-path", "-o");
        command.Options.Add(outputPath);

        command.SetAction(async (parseResult, ct) => {
            using var app = BuildApplication();
            _ = app.RunAsync(ct).ConfigureAwait(false);

            var runner = app.Services.GetRequiredService<AppHostRunner>();
            var appHostProjectFile = parseResult.GetValue<FileInfo>("project");
            var target = parseResult.GetValue<string>("--target");
            var outputPath = parseResult.GetValue<string>("--output-path");
            var exitCode = await runner.RunAppHostAsync(appHostProjectFile!, ["--publisher", target ?? "manifest", "--output-path", outputPath ?? "."], ct).ConfigureAwait(false);

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
