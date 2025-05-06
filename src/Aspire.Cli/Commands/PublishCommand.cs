// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal interface IPublishCommandPrompter
{
    Task<string> PromptForPublisherAsync(IEnumerable<string> publishers, CancellationToken cancellationToken);
}

internal class PublishCommandPrompter(IInteractionService interactionService) : IPublishCommandPrompter
{
    public virtual async Task<string> PromptForPublisherAsync(IEnumerable<string> publishers, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForSelectionAsync(
            "Select a publisher:",
            publishers,
            p => p,
            cancellationToken
        );
    }
}

internal sealed class PublishCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(PublishCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
    private readonly IPublishCommandPrompter _prompter;

    public PublishCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, IPublishCommandPrompter prompter)
        : base("publish", "Generates deployment artifacts for an Aspire app host project.")
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(prompter);

        _runner = runner;
        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _prompter = prompter;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
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
        var buildOutputCollector = new OutputCollector();
        var inspectOutputCollector = new OutputCollector();
        var publishOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingSdkVersion)? appHostCompatibilityCheck = null;

        try
        {
            using var activity = _activitySource.StartActivity();

            var effectiveAppHostProjectFile = await _interactionService.ShowStatusAsync("Locating app host project...", async () =>
            {
                var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
                return await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);
            });

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            var waitForDebugger = parseResult.GetValue<bool?>("--wait-for-debugger") ?? false;
            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException("IsCompatibleAppHost is null"))
            {
                return ExitCodeConstants.AppHostIncompatible;
            }

            var buildOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = buildOutputCollector.AppendOutput,
                StandardErrorCallback = buildOutputCollector.AppendError
            };

            var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, _interactionService, effectiveAppHostProjectFile, buildOptions, cancellationToken);

            if (buildExitCode != 0)
            {
                _interactionService.DisplayLines(buildOutputCollector.GetLines());
                _interactionService.DisplayError("The project could not be built. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            var publisher = parseResult.GetValue<string>("--publisher");
            var outputPath = parseResult.GetValue<string>("--output-path");
            var fullyQualifiedOutputPath = Path.GetFullPath(outputPath ?? ".");

            var publishersResult = await _interactionService.ShowStatusAsync<(int ExitCode, string[] Publishers)>(
                publisher is { } ? ":package:  Getting publisher..." : ":package:  Getting publishers...",
                async () => {
                    using var getPublishersActivity = _activitySource.StartActivity(
                        $"{nameof(ExecuteAsync)}-Action-GetPublishers",
                        ActivityKind.Client);

                    var getPublishersRunOptions = new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = inspectOutputCollector.AppendOutput,
                        StandardErrorCallback = inspectOutputCollector.AppendError,
                    };

                    var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();
                    var pendingInspectRun = _runner.RunAsync(
                        effectiveAppHostProjectFile,
                        false,
                        true,
                        ["--operation", "inspect"],
                        env,
                        backchannelCompletionSource,
                        getPublishersRunOptions,
                        cancellationToken).ConfigureAwait(false);

                    if (waitForDebugger)
                    {
                        _interactionService.DisplayMessage("bug", $"Waiting for debugger to attach to app host process.");
                    }

                    var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);
                    var publishers = await backchannel.GetPublishersAsync(cancellationToken).ConfigureAwait(false);
                    
                    await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
                    var exitCode = await pendingInspectRun;

                    return (exitCode, publishers);
                }
            );

            if (publishersResult.ExitCode != 0)
            {
                _interactionService.DisplayLines(inspectOutputCollector.GetLines());
                _interactionService.DisplayError($"The publisher inspection failed with exit code {publishersResult.ExitCode}. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            var publishers = publishersResult.Publishers;
            if (publishers is null || publishers.Length == 0)
            {
                _interactionService.DisplayError($"No publishers were found.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            if (publishers?.Contains(publisher) != true)
            {
                if (publisher is not null)
                {
                    _interactionService.DisplayMessage("warning", $"[yellow bold]The specified publisher '{publisher}' was not found.[/]");
                }

                publisher = await _prompter.PromptForPublisherAsync(publishers!, cancellationToken);
            }

            _interactionService.DisplayMessage($"hammer_and_wrench", $"Generating artifacts for '{publisher}' publisher...");

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
                    
                    var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();

                    var launchingAppHostTask = context.AddTask(":play_button:  Launching apphost");
                    launchingAppHostTask.IsIndeterminate();
                    launchingAppHostTask.StartTask();

                    var publishRunOptions = new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = publishOutputCollector.AppendOutput,
                        StandardErrorCallback = publishOutputCollector.AppendError,
                    };

                    var pendingRun = _runner.RunAsync(
                        effectiveAppHostProjectFile,
                        false,
                        true,
                        ["--operation", "publish", "--publisher", publisher ?? "manifest", "--output-path", fullyQualifiedOutputPath],
                        env,
                        backchannelCompletionSource,
                        publishRunOptions,
                        cancellationToken);

                    ProgressTask? attachDebuggerTask = null;
                    if (waitForDebugger)
                    {
                        attachDebuggerTask = context.AddTask($":bug:  Waiting for debugger to attach to app host process");
                        attachDebuggerTask.IsIndeterminate();
                        attachDebuggerTask.StartTask();
                    }

                    var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);

                    if (attachDebuggerTask is not null)
                    {
                        attachDebuggerTask.Description = $":check_mark:  Debugger attached (or timed out)";
                        attachDebuggerTask.Value = 100;
                        attachDebuggerTask.StopTask();
                    }

                    launchingAppHostTask.Description = $":check_mark:  Launching apphost";
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

                        progressTask.Description = $":play_button:  {publishingActivity.StatusText}";

                        if (publishingActivity.IsComplete && !publishingActivity.IsError)
                        {
                            progressTask.Description = $":check_mark:  {publishingActivity.StatusText}";
                            progressTask.Value = 100;
                            progressTask.StopTask();
                        }
                        else if (publishingActivity.IsError)
                        {
                            progressTask.Description = $"[red bold]:cross_mark:  {publishingActivity.StatusText}[/]";
                            progressTask.Value = 0;
                            break;
                        }
                        else
                        {
                            // Keep going man!
                        }
                    }

                    // When we are running in publish mode we don't want the app host to
                    // stop itself while we might still be streaming data back across
                    // the RPC backchannel. So we need to take responsibility for stopping
                    // the app host. If the CLI exits/crashes without explicitly stopping
                    // the app host the orphan detector in the app host will kick in.
                    if (progressTasks.Any(kvp => !kvp.Value.IsFinished))
                    {
                        // Depending on the failure the publisher may return a zero
                        // exit code.
                        await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
                        var exitCode = await pendingRun;

                        // If we are in the state where we've detected an error because there
                        // is an incomplete task then we stop the app host, but depending on
                        // where/how the failure occured, we might still get a zero exit
                        // code. If we get a non-zero exit code we want to return that
                        // as it might be useful for diagnostic purposes, however if we don't
                        // get a non-zero exit code we want to return our built-in exit code
                        // for failed artifact build.
                        return exitCode == 0 ? ExitCodeConstants.FailedToBuildArtifacts : exitCode;
                    }
                    else
                    {
                        // If we are here then all the tasks are finished and we can
                        // stop the app host.
                        await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
                        var exitCode = await pendingRun;
                        return exitCode; // should be zero for orderly shutdown but we pass it along anyway.
                    }
                });

            if (exitCode != 0)
            {
                _interactionService.DisplayLines(publishOutputCollector.GetLines());
                _interactionService.DisplayError($"Publishing artifacts failed with exit code {exitCode}. For more information run with --debug switch.");
                return ExitCodeConstants.FailedToBuildArtifacts;
            }
            else
            {
                _interactionService.DisplaySuccess($"Successfully published artifacts to: {fullyQualifiedOutputPath}");
                return ExitCodeConstants.Success;
            }
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayError("The operation was canceled.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (ProjectLocatorException ex) when (ex.Message == "Project file is not an Aspire app host project.")
        {
            _interactionService.DisplayError("The specified project file is not an Aspire app host project.");
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (ex.Message == "Project file does not exist.")
        {
            _interactionService.DisplayError("The --project option specified a project that does not exist.");
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (ex.Message.Contains("Multiple project files found."))
        {
            _interactionService.DisplayError("The --project option was not specified and multiple app host project files were detected.");
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (ex.Message.Contains("No project file"))
        {
            _interactionService.DisplayError("The project argument was not specified and no *.csproj files were detected.");
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (AppHostIncompatibleException ex)
        {
            return _interactionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingSdkVersion ?? throw new InvalidOperationException("AspireHostingSdkVersion is null")
                );
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError($"An error occurred while connecting to the app host backchannel. The app host possibly crashed before it was available: {ex.Message}");

            var operationArgumentIndex = ex.Process.StartInfo.ArgumentList.IndexOf("--operation");
            var operation = ex.Process.StartInfo.ArgumentList[operationArgumentIndex + 1];

            // This particular error can occur both when we are in inspect mode or in publish mode
            // depending on where the code is that is causing the apphost process to crash
            // before the backchannel is avaialble. When we remove publisher selection from the
            // CLI this code can be simplified again.
            Func<IEnumerable<(string Stream, string Line)>> linesCallback = operation switch {
                "inspect" => inspectOutputCollector.GetLines,
                "publish" => publishOutputCollector.GetLines,
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };

            _interactionService.DisplayLines(linesCallback());

            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError($"An unexpected error occurred: {ex.Message}");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
    }
}
