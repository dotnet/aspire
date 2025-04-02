// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using Spectre.Console;

namespace Aspire.Cli;

public class Program
{
    private static readonly ActivitySource s_activitySource = new ActivitySource(nameof(Aspire.Cli.Program));

    private static IHost BuildApplication(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();

        // Always configure OpenTelemetry.
        builder.Logging.AddOpenTelemetry(logging => {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            });

        var otelBuilder = builder.Services.AddOpenTelemetry()
                                          .WithTracing(tracing => {
                                            tracing.AddSource(
                                                nameof(Aspire.Cli.NuGetPackageCache),
                                                nameof(Aspire.Cli.Backchannel.AppHostBackchannel),
                                                nameof(Aspire.Cli.DotNetCliRunner),
                                                nameof(Aspire.Cli.Program));
                                            });

        if (builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is {})
        {
            // NOTE: If we always enable the OTEL exporter it dramatically
            //       impacts the CLI in terms of exiting quickly because it
            //       has to finish sending telemetry.
            otelBuilder.UseOtlpExporter();
        }

        var debugMode = args?.Any(a => a == "--debug" || a == "-d") ?? false;

        if (debugMode)
        {
            builder.Logging.AddFilter("Aspire.Cli", LogLevel.Debug);
            builder.Logging.AddConsole();
        }

        // Shared services.
        builder.Services.AddTransient<DotNetCliRunner>();
        builder.Services.AddTransient<AppHostBackchannel>();
        builder.Services.AddSingleton<CliRpcTarget>();
        builder.Services.AddTransient<INuGetPackageCache, NuGetPackageCache>();

        // Commands.
        builder.Services.AddTransient<NewCommand>();
        builder.Services.AddTransient<RunCommand>();
        builder.Services.AddTransient<AddCommand>();

        var app = builder.Build();
        return app;
    }

    private static RootCommand GetRootCommand(IHost app)
    {
        var rootCommand = new RootCommand("Aspire CLI");

        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Recursive = true;
        rootCommand.Options.Add(debugOption);
        
        var waitForDebuggerOption = new Option<bool>("--wait-for-debugger", "-w");
        waitForDebuggerOption.Recursive = true;
        waitForDebuggerOption.DefaultValueFactory = (result) => false;

        #if DEBUG
        waitForDebuggerOption.Validators.Add((result) => {

            var waitForDebugger = result.GetValueOrDefault<bool>();

            if (waitForDebugger)
            {
                AnsiConsole.Status().Start(
                    $":bug:  Waiting for debugger to attach to process ID: {Environment.ProcessId}",
                    context => {
                        while (!Debugger.IsAttached)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                );
            }
        });
        #endif

        rootCommand.Options.Add(waitForDebuggerOption);

        ConfigureRunCommand(rootCommand, app);
        ConfigurePublishCommand(rootCommand, app);
        ConfigureNewCommand(rootCommand, app);
        ConfigureAddCommand(rootCommand, app);
        return rootCommand;
    }

    private static void ConfigureRunCommand(Command parentCommand, IHost app)
    {
        var command = app.Services.GetRequiredService<RunCommand>();
        parentCommand.Add(command);
    }

    private static void ConfigurePublishCommand(Command parentCommand, IHost app)
    {
        var command = new Command("publish", "Generates deployment artifacts for a .NET Aspire AppHost project.");

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Validators.Add(ProjectFileHelper.ValidateProjectOption);
        command.Options.Add(projectOption);

        var publisherOption = new Option<string>("--publisher", "-p");
        command.Options.Add(publisherOption);

        var outputPath = new Option<string>("--output-path", "-o");
        outputPath.DefaultValueFactory = (result) => Path.Combine(Environment.CurrentDirectory);
        command.Options.Add(outputPath);

        command.SetAction(async (parseResult, ct) => {
            using var activity = s_activitySource.StartActivity($"{nameof(ConfigurePublishCommand)}-Action", ActivityKind.Internal);

            var runner = app.Services.GetRequiredService<DotNetCliRunner>();
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = ProjectFileHelper.UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            if (parseResult.GetValue<bool?>("--wait-for-debugger") ?? false)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            var appHostCompatabilityCheck = await AppHostHelper.CheckAppHostCompatabilityAsync(runner, effectiveAppHostProjectFile, ct);

            if (!appHostCompatabilityCheck.IsCompatableAppHost)
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var publisher = parseResult.GetValue<string>("--publisher");
            var outputPath = parseResult.GetValue<string>("--output-path");
            var fullyQualifiedOutputPath = Path.GetFullPath(outputPath ?? ".");

            var publishersResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots3)
                .SpinnerStyle(Style.Parse("purple"))
                .StartAsync<(int ExitCode, string[]? Publishers)>(
                    publisher is { } ? ":package:  Getting publisher..." : ":package:  Getting publishers...",
                    async context => {

                        using var getPublishersActivity = s_activitySource.StartActivity(
                            $"{nameof(ConfigurePublishCommand)}-Action-GetPublishers",
                            ActivityKind.Client);

                        var backchannelCompletionSource = new TaskCompletionSource<AppHostBackchannel>();
                        var pendingInspectRun = runner.RunAsync(
                            effectiveAppHostProjectFile,
                            false,
                            false,
                            ["--operation", "inspect"],
                            null,
                            backchannelCompletionSource,
                            ct).ConfigureAwait(false);

                        var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);
                        var publishers = await backchannel.GetPublishersAsync(ct).ConfigureAwait(false);
                        
                        await backchannel.RequestStopAsync(ct).ConfigureAwait(false);
                        var exitCode = await pendingInspectRun;

                        return (exitCode, publishers);

                    }).ConfigureAwait(false);

            if (publishersResult.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  The publisher inspection failed with exit code {publishersResult.ExitCode}. For more information run with --debug switch.[/]");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            var publishers = publishersResult.Publishers;
            if (publishers is null || publishers.Length == 0)
            {
                AnsiConsole.MarkupLine("[red bold]:thumbs_down:  No publishers were found.[/]");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            if (publishers?.Contains(publisher) != true)
            {
                if (publisher is not null)
                {
                    AnsiConsole.MarkupLine($"[red bold]:warning:  The specified publisher '{publisher}' was not found.[/]");
                }

                var publisherPrompt = new SelectionPrompt<string>()
                    .Title("Select a publisher:")
                    .UseConverter(p => p)
                    .PageSize(10)
                    .EnableSearch()
                    .HighlightStyle(Style.Parse("darkmagenta"))
                    .AddChoices(publishers!);

                publisher = AnsiConsole.Prompt(publisherPrompt);
            }

            AnsiConsole.MarkupLine($":hammer_and_wrench:  Generating artifacts for '{publisher}' publisher...");

            var exitCode = await AnsiConsole.Progress()
                .AutoRefresh(true)
                .Columns(
                    new TaskDescriptionColumn() { Alignment = Justify.Left },
                    new ProgressBarColumn() { Width = 10 },
                    new ElapsedTimeColumn())
                .StartAsync(async context => {

                    using var generateArtifactsActivity = s_activitySource.StartActivity(
                        $"{nameof(ConfigurePublishCommand)}-Action-GenerateArtifacts",
                        ActivityKind.Internal);
                    
                    var backchannelCompletionSource = new TaskCompletionSource<AppHostBackchannel>();

                    var launchingAppHostTask = context.AddTask("Launching apphost");
                    launchingAppHostTask.IsIndeterminate();
                    launchingAppHostTask.StartTask();

                    var pendingRun = runner.RunAsync(
                        effectiveAppHostProjectFile,
                        false,
                        true,
                        ["--publisher", publisher ?? "manifest", "--output-path", fullyQualifiedOutputPath],
                        env,
                        backchannelCompletionSource,
                        ct);

                    var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);

                    launchingAppHostTask.Value = 100;
                    launchingAppHostTask.StopTask();

                    var publishingActivities = backchannel.GetPublishingActivitiesAsync(ct);

                    var progressTasks = new Dictionary<string, ProgressTask>();

                    await foreach (var publishingActivity in publishingActivities)
                    {
                        if (!progressTasks.TryGetValue(publishingActivity.Id, out var progressTask))
                        {
                            progressTask = context.AddTask(publishingActivity.Id);
                            progressTask.StartTask();
                            progressTask.IsIndeterminate();
                            progressTasks.Add(publishingActivity.Id, progressTask);
                        }

                        progressTask.Description = $"{publishingActivity.StatusText}";

                        if (publishingActivity.IsComplete)
                        {
                            progressTask.Value = 100;
                            progressTask.StopTask();
                        }
                        else if (publishingActivity.IsError)
                        {
                            progressTask.Value = 100;
                            progressTask.StopTask();
                        }
                    }

                    // When we are running in publish mode we don't want the app host to
                    // stop itself while we might still be streaming data back across
                    // the RPC backchannel. So we need to take responsibility for stopping
                    // the app host. If the CLI exits/crashes without explicitly stopping
                    // the app host the orphan detector in the app host will kick in.
                    await backchannel.RequestStopAsync(ct).ConfigureAwait(false);
                    return await pendingRun;
                });

            if (exitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  The build failed with exit code {exitCode}. For more information run with --debug switch.[/]");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green bold]:thumbs_up:  The build completed successfully to: {fullyQualifiedOutputPath}[/]");
                return ExitCodeConstants.Success;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    private static void ConfigureNewCommand(Command parentCommand, IHost app)
    {
        var command = app.Services.GetRequiredService<NewCommand>();
        parentCommand.Add(command);
    }

    private static void ConfigureAddCommand(Command parentCommand, IHost app)
    {
        var command = app.Services.GetRequiredService<AddCommand>();
        parentCommand.Add(command);
    }

    public static async Task<int> Main(string[] args)
    {
        System.Console.OutputEncoding = Encoding.UTF8;

        using var app = BuildApplication(args);

        await app.StartAsync().ConfigureAwait(false);

        var rootCommand = GetRootCommand(app);
        var config = new CommandLineConfiguration(rootCommand);
        config.EnableDefaultExceptionHandler = true;
        
        using var activity = s_activitySource.StartActivity(nameof(Main), ActivityKind.Internal);
        var exitCode = await config.InvokeAsync(args);

        await app.StopAsync().ConfigureAwait(false);

        return exitCode;
    }
}
