// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class PublishCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(PublishCommand));
    private readonly DotNetCliRunner _runner;

    public PublishCommand(DotNetCliRunner runner)
        : base("publish", "Generates deployment artifacts for an Aspire app host project.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        _runner = runner;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
        projectOption.Validators.Add(ProjectFileHelper.ValidateProjectOption);
        Options.Add(projectOption);

        var publisherOption = new Option<string>("--publisher", "-p");
        publisherOption.Description = "The name of the publisher to use.";
        Options.Add(publisherOption);

        var outputPath = new Option<string>("--output-path", "-o");
        outputPath.Description = "The output path for the generated artifacts.";
        outputPath.DefaultValueFactory = (result) => Path.Combine(Environment.CurrentDirectory);
        Options.Add(outputPath);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

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

        var appHostCompatabilityCheck = await AppHostHelper.CheckAppHostCompatabilityAsync(_runner, effectiveAppHostProjectFile, cancellationToken);

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

                    using var getPublishersActivity = _activitySource.StartActivity(
                        $"{nameof(ExecuteAsync)}-Action-GetPublishers",
                        ActivityKind.Client);

                    var backchannelCompletionSource = new TaskCompletionSource<AppHostBackchannel>();
                    var pendingInspectRun = _runner.RunAsync(
                        effectiveAppHostProjectFile,
                        false,
                        false,
                        ["--operation", "inspect"],
                        null,
                        backchannelCompletionSource,
                        cancellationToken).ConfigureAwait(false);

                    var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);
                    var publishers = await backchannel.GetPublishersAsync(cancellationToken).ConfigureAwait(false);
                    
                    await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
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

                using var generateArtifactsActivity = _activitySource.StartActivity(
                    $"{nameof(ExecuteAsync)}-Action-GenerateArtifacts",
                    ActivityKind.Internal);
                
                var backchannelCompletionSource = new TaskCompletionSource<AppHostBackchannel>();

                var launchingAppHostTask = context.AddTask("Launching apphost");
                launchingAppHostTask.IsIndeterminate();
                launchingAppHostTask.StartTask();

                var pendingRun = _runner.RunAsync(
                    effectiveAppHostProjectFile,
                    false,
                    true,
                    ["--publisher", publisher ?? "manifest", "--output-path", fullyQualifiedOutputPath],
                    env,
                    backchannelCompletionSource,
                    cancellationToken);

                var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);

                launchingAppHostTask.Value = 100;
                launchingAppHostTask.StopTask();

                var publishingActivities = backchannel.GetPublishingActivitiesAsync(cancellationToken);

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
                await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
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
    }
}
