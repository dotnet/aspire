// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Commands;

internal abstract class PublishCommandBase : BaseCommand
{
    protected readonly IDotNetCliRunner _runner;
    protected readonly IInteractionService _interactionService;
    protected readonly IProjectLocator _projectLocator;
    protected readonly AspireCliTelemetry _telemetry;

    protected PublishCommandBase(string name, string description, IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry)
        : base(name, description)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(telemetry);

        _runner = runner;
        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _telemetry = telemetry;

        var projectOption = new Option<FileInfo?>("--project")
        {
            Description = PublishCommandStrings.ProjectArgumentDescription
        };
        Options.Add(projectOption);

        var outputPath = new Option<string>("--output-path", "-o")
        {
            Description = GetOutputPathDescription(),
            DefaultValueFactory = GetDefaultOutputPath
        };
        Options.Add(outputPath);

        // In the publish and deploy commands we forward all unrecognized tokens
        // through to the underlying tooling when we launch the app host.
        TreatUnmatchedTokensAsErrors = false;
    }

    protected abstract string GetOutputPathDescription();
    protected abstract string GetDefaultOutputPath(ArgumentResult result);
    protected abstract string[] GetRunArguments(string fullyQualifiedOutputPath, string[] unmatchedTokens);
    protected abstract string GetSuccessMessage(string fullyQualifiedOutputPath);
    protected abstract string GetFailureMessage(int exitCode);
    protected abstract string GetCanceledMessage();
    protected abstract string GetProgressMessage();

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var buildOutputCollector = new OutputCollector();
        var operationOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;

        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

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

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, _telemetry, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException("IsCompatibleAppHost is null"))
            {
                return ExitCodeConstants.AppHostIncompatible;
            }

            var buildOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = buildOutputCollector.AppendOutput,
                StandardErrorCallback = buildOutputCollector.AppendError,
            };

            var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, _interactionService, effectiveAppHostProjectFile, buildOptions, cancellationToken);

            if (buildExitCode != 0)
            {
                _interactionService.DisplayLines(buildOutputCollector.GetLines());
                _interactionService.DisplayError(InteractionServiceStrings.ProjectCouldNotBeBuilt);
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            var outputPath = parseResult.GetValue<string>("--output-path");
            var fullyQualifiedOutputPath = Path.GetFullPath(outputPath ?? ".");

            _interactionService.DisplayMessage($"hammer_and_wrench", GetProgressMessage());

            var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var operationRunOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = operationOutputCollector.AppendOutput,
                StandardErrorCallback = operationOutputCollector.AppendError,
                NoLaunchProfile = true
            };

            var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostProjectFile,
                false,
                true,
                GetRunArguments(fullyQualifiedOutputPath, unmatchedTokens),
                env,
                backchannelCompletionSource,
                operationRunOptions,
                cancellationToken);

            // If we use the --wait-for-debugger option we print out the process ID
            // of the apphost so that the user can attach to it.
            if (waitForDebugger)
            {
                _interactionService.DisplayMessage("bug", InteractionServiceStrings.WaitingForDebuggerToAttachToAppHost);
            }

            var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);
            var publishingActivities = backchannel.GetPublishingActivitiesAsync(cancellationToken);

            var debugMode = parseResult.GetValue<bool?>("--debug") ?? false;

            var noFailuresReported = debugMode switch
            {
                true => await ProcessPublishingActivitiesAsync(publishingActivities, cancellationToken),
                false => await ProcessAndDisplayPublishingActivitiesAsync(publishingActivities, cancellationToken),
            };

            await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
            var exitCode = await pendingRun;

            if (exitCode == 0 && noFailuresReported)
            {
                return ExitCodeConstants.Success;
            }

            if (debugMode)
            {
                _interactionService.DisplayLines(operationOutputCollector.GetLines());
            }

            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayError(GetCanceledMessage());
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.ProjectFileNotAppHostProject, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.SpecifiedProjectFileNotAppHostProject);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.ProjectFileDoesntExist, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionDoesntExist);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.MultipleProjectFilesFound, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedMultipleAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedNoCsprojFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (AppHostIncompatibleException ex)
        {
            return _interactionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message));
            _interactionService.DisplayLines(operationOutputCollector.GetLines());
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
    }

    public static async Task<bool> ProcessPublishingActivitiesAsync(IAsyncEnumerable<PublishingActivity> publishingActivities, CancellationToken cancellationToken)
    {
        await foreach (var publishingActivity in publishingActivities.WithCancellation(cancellationToken))
        {
            if (publishingActivity.Type == PublishingActivityTypes.PublishComplete)
            {
                return !publishingActivity.Data.IsError;
            }
        }

        return true;
    }

    public static async Task<bool> ProcessAndDisplayPublishingActivitiesAsync(IAsyncEnumerable<PublishingActivity> publishingActivities, CancellationToken cancellationToken)
    {
        var stepCounter = 1;
        var steps = new Dictionary<string, StepInfo>();
        PublishingActivity? publishingActivity = null;
        var currentStepProgress = new ProgressContextInfo();

        await foreach (var activity in publishingActivities.WithCancellation(cancellationToken))
        {
            // PublishComplete is emitted at the end of the publishing process
            // by the DistributedApplicationRunner. Display the final status and
            // cancel any in-progress tasks when this happens.
            if (activity.Type == PublishingActivityTypes.PublishComplete)
            {
                publishingActivity = activity;

                break;
            }
            else if (activity.Type == PublishingActivityTypes.Step)
            {
                // If this is our first time encountering this step, initialize it by
                // display the step header and configuring a new ProgressContext for the
                // tasks that will be parented to this step.
                if (!steps.TryGetValue(activity.Data.Id, out var stepInfo))
                {
                    if (currentStepProgress.Step is not null)
                    {
                        throw new InvalidOperationException($"Step activity with ID '{currentStepProgress.Step?.Id}' is not complete. Expected it to be complete before processing tasks.");
                    }

                    stepInfo = new StepInfo
                    {
                        Id = activity.Data.Id,
                        Title = activity.Data.StatusText,
                        Number = stepCounter++,
                        StartTime = DateTime.UtcNow
                    };

                    steps[activity.Data.Id] = stepInfo;

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[bold]Step {stepInfo.Number}: {stepInfo.Title.EscapeMarkup()}[/]");

                    currentStepProgress = new ProgressContextInfo { Step = stepInfo };
                }
                // If the step is complete, update the step info, clear out any pending progress tasks, and
                // display the completion status associated with the the step.
                else if (activity.Data.IsComplete)
                {
                    stepInfo.IsComplete = true;
                    stepInfo.IsError = activity.Data.IsError;
                    stepInfo.CompletionText = activity.Data.StatusText;

                    await currentStepProgress.DisposeAsync();

                    if (stepInfo.IsError)
                    {
                        AnsiConsole.MarkupLine($"[red bold]❌ FAILED:[/] {stepInfo.CompletionText.EscapeMarkup()}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[green bold]✅ COMPLETED:[/] {stepInfo.CompletionText.EscapeMarkup()}");
                    }

                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(new Rule().RuleStyle(Style.Parse("grey")).DoubleBorder().LeftJustified());
                    AnsiConsole.WriteLine();

                    // Clean up the current progress context and reset the step ID so that it
                    // can be reused by the next step.
                    currentStepProgress = new ProgressContextInfo();
                }
                else
                {
                    throw new InvalidOperationException($"Step activity with ID '{activity.Data.Id}' is not complete. Expected it to be complete before processing tasks.");
                }
            }
            else
            {
                var stepId = activity.Data.StepId;
                Debug.Assert(stepId != null, "Activity data should have a StepId for task activities.");

                if (currentStepProgress.Step?.Id != stepId)
                {
                    throw new InvalidOperationException($"Task activity with ID '{activity.Data.Id}' is not associated with the current step '{currentStepProgress.Step?.Id}'.");
                }

                var tasks = currentStepProgress.Step.Tasks;

                await StartProgressForStep(currentStepProgress, cancellationToken);

                if (!tasks.TryGetValue(activity.Data.Id, out var task))
                {
                    task = new TaskInfo
                    {
                        Id = activity.Data.Id,
                        StatusText = activity.Data.StatusText,
                        StartTime = DateTime.UtcNow
                    };

                    tasks[activity.Data.Id] = task;

                    // Start progress context on first task for this step
                    task.ProgressTask = currentStepProgress.Ctx!.AddTask($"  {activity.Data.StatusText.EscapeMarkup()}");
                    task.ProgressTask.IsIndeterminate = true;
                }

                if (task.ProgressTask is null)
                {
                    throw new InvalidOperationException($"Task with ID '{activity.Data.Id}' does not have an associated ProgressTask.");
                }

                task.StatusText = activity.Data.StatusText;
                task.IsComplete = activity.Data.IsComplete;
                task.IsError = activity.Data.IsError;
                task.IsWarning = activity.Data.IsWarning;

                if (task.IsError || task.IsWarning || task.IsComplete)
                {
                    var prefix = task.IsError ? "[red]✗ FAILED:[/]" :
                        task.IsWarning ? "[yellow]⚠ WARNING:[/]" : "[green]✓ DONE:[/]";
                    task.ProgressTask.Description = $"  {prefix} {task.StatusText.EscapeMarkup()}";
                    task.CompletionMessage = activity.Data.CompletionMessage;

                    // Add completion message to the shared dictionary so that it can be displayed after the status text in the column view.
                    if (currentStepProgress.TaskCompletionMessages != null && !string.IsNullOrEmpty(activity.Data.CompletionMessage))
                    {
                        currentStepProgress.TaskCompletionMessages[task.ProgressTask.Id] = activity.Data.CompletionMessage;
                    }

                    // We don't set hasErrors = true on task errors to avoid early exits. We only
                    // process errors captured at the step-level or publish complete level.
                    task.ProgressTask.StopTask();
                }
                else
                {
                    task.ProgressTask.Description = $"  {task.StatusText.EscapeMarkup()}";
                }
            }
        }

        var hasErrors = publishingActivity?.Data.IsError ?? false;

        if (publishingActivity is not null)
        {
            if (hasErrors)
            {
                AnsiConsole.MarkupLine($"[red bold]❌ PUBLISHING FAILED:[/] {publishingActivity.Data.StatusText.EscapeMarkup()}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green bold]✅ PUBLISHING COMPLETED:[/] {publishingActivity.Data.StatusText.EscapeMarkup()}");
            }
        }

        return !hasErrors;
    }

    private static async Task StartProgressForStep(ProgressContextInfo progressContext, CancellationToken cancellationToken)
    {
        if (progressContext.Context is not null)
        {
            // If the context is already started, we don't need to do anything.
            return;
        }

        progressContext.Context = AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
            [
                new SpinnerColumn(Spinner.Known.BouncingBar) { Style = Style.Parse("yellow") },
                new TaskDescriptionWithCompletionColumn(progressContext.TaskCompletionMessages),
                new ElapsedTimeColumn() { Style = Style.Parse("grey") }
            ]);

        // Use a TaskCompletionSource to signal when the context is ready
        var contextReadySource = new TaskCompletionSource<ProgressContext>(TaskCreationOptions.RunContinuationsAsynchronously);

        progressContext.ContextTask = progressContext.Context.StartAsync(async ctx =>
        {
            // Signal that the context is ready so that the invoker can start to populate
            // it with tasks
            progressContext.Ctx = ctx;
            contextReadySource.SetResult(ctx);

            // Cancel the Spectre progress context when a cancellation is requested
            // explicitly by the ProgressContext.CancellationTokenSource.
            await progressContext.KeepProgressContextAliveTcs.Task.WaitAsync(cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        });

        // Wait for the context to be ready before returning
        await contextReadySource.Task;
    }

    private class StepInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Number { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsComplete { get; set; }
        public bool IsError { get; set; }
        public string CompletionText { get; set; } = string.Empty;
        public Dictionary<string, TaskInfo> Tasks { get; } = [];
    }

    private class TaskInfo
    {
        public string Id { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public bool IsComplete { get; set; }
        public bool IsError { get; set; }
        public bool IsWarning { get; set; }
        public string? CompletionMessage { get; set; }
        public ProgressTask? ProgressTask { get; set; }
    }

    private class ProgressContextInfo : IAsyncDisposable
    {
        public StepInfo? Step { get; set; }
        public Progress? Context { get; set; }
        public Task? ContextTask { get; set; }
        public ProgressContext? Ctx { get; set; }
        public TaskCompletionSource KeepProgressContextAliveTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        // Dictionary to track completion messages for tasks so that they can be rendered
        // below the task description in the progress view using the custom column implementation.
        public ConcurrentDictionary<int, string> TaskCompletionMessages { get; } = [];

        public async ValueTask DisposeAsync()
        {
            KeepProgressContextAliveTcs.TrySetResult();

            if (ContextTask is not null)
            {
                await ContextTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }

    // Custom column type to display the status text associated with task
    // and the optional completion message if the task has completed below
    // it.
    private class TaskDescriptionWithCompletionColumn(ConcurrentDictionary<int, string> completionMessages) : ProgressColumn
    {
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var description = task.Description ?? string.Empty;

            if (completionMessages.TryGetValue(task.Id, out var completionMessage) && !string.IsNullOrEmpty(completionMessage))
            {
                List<IRenderable> items =
                [
                    new Markup(description),
                    new Markup($"    [dim]{completionMessage.EscapeMarkup()}[/]")
                ];

                return new Rows(items);
            }

            return new Markup(description);
        }
    }
}
