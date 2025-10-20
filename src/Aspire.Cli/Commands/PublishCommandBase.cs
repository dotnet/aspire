// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal abstract class PublishCommandBase : BaseCommand
{
    private const string CustomChoiceValue = "__CUSTOM_CHOICE";

    protected readonly IDotNetCliRunner _runner;
    protected readonly IProjectLocator _projectLocator;
    protected readonly AspireCliTelemetry _telemetry;
    protected readonly IDotNetSdkInstaller _sdkInstaller;

    private readonly IFeatures _features;
    private readonly ICliHostEnvironment _hostEnvironment;

    protected abstract string OperationCompletedPrefix { get; }
    protected abstract string OperationFailedPrefix { get; }

    private static bool IsCompletionStateComplete(string completionState) =>
        completionState is CompletionStates.Completed or CompletionStates.CompletedWithWarning or CompletionStates.CompletedWithError;

    private static bool IsCompletionStateError(string completionState) =>
        completionState == CompletionStates.CompletedWithError;

    private static bool IsCompletionStateWarning(string completionState) =>
        completionState == CompletionStates.CompletedWithWarning;

    protected PublishCommandBase(string name, string description, IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment)
        : base(name, description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(sdkInstaller);
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        _runner = runner;
        _projectLocator = projectLocator;
        _telemetry = telemetry;
        _sdkInstaller = sdkInstaller;
        _features = features;
        _hostEnvironment = hostEnvironment;

        var projectOption = new Option<FileInfo?>("--project")
        {
            Description = PublishCommandStrings.ProjectArgumentDescription
        };
        Options.Add(projectOption);

        var outputPath = new Option<string?>("--output-path", "-o")
        {
            Description = GetOutputPathDescription()
        };
        Options.Add(outputPath);

        // In the publish and deploy commands we forward all unrecognized tokens
        // through to the underlying tooling when we launch the app host.
        TreatUnmatchedTokensAsErrors = false;
    }

    protected abstract string GetOutputPathDescription();
    protected abstract string[] GetRunArguments(string? fullyQualifiedOutputPath, string[] unmatchedTokens, ParseResult parseResult);
    protected abstract string GetCanceledMessage();
    protected abstract string GetProgressMessage();

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Send terminal infinite progress bar start sequence
        StartTerminalProgressBar();

        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, InteractionService, cancellationToken))
        {
            // Send terminal progress bar stop sequence
            StopTerminalProgressBar();
            return ExitCodeConstants.SdkNotInstalled;
        }

        var buildOutputCollector = new OutputCollector();
        var operationOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;

        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

            if (effectiveAppHostFile is null)
            {
                // Send terminal progress bar stop sequence
                StopTerminalProgressBar();
                return ExitCodeConstants.FailedToFindProject;
            }

            var isSingleFileAppHost = effectiveAppHostFile.Extension != ".csproj";

            // Validate that single file AppHost feature is enabled if we detected a .cs file
            if (isSingleFileAppHost && !_features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
            {
                // Send terminal progress bar stop sequence
                StopTerminalProgressBar();
                InteractionService.DisplayError(ErrorStrings.SingleFileAppHostFeatureNotEnabled);
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            // Set interactivity enabled based on host environment capabilities
            if (!_hostEnvironment.SupportsInteractiveInput)
            {
                env[KnownConfigNames.InteractivityEnabled] = "false";
            }

            var waitForDebugger = parseResult.GetValue<bool?>("--wait-for-debugger") ?? false;
            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            if (isSingleFileAppHost)
            {
                // TODO: Add logic to read SDK version from *.cs file.
                appHostCompatibilityCheck = (true, true, VersionHelper.GetDefaultTemplateVersion());
            }
            else
            {
                appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, InteractionService, effectiveAppHostFile, _telemetry, ExecutionContext.WorkingDirectory, cancellationToken);
            }

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException("IsCompatibleAppHost is null"))
            {
                // Send terminal progress bar stop sequence
                StopTerminalProgressBar();
                return ExitCodeConstants.AppHostIncompatible;
            }

            var buildOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = buildOutputCollector.AppendOutput,
                StandardErrorCallback = buildOutputCollector.AppendError,
            };

            if (!isSingleFileAppHost)
            {
                var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, InteractionService, effectiveAppHostFile, buildOptions, ExecutionContext.WorkingDirectory, cancellationToken);

                if (buildExitCode != 0)
                {
                    // Send terminal progress bar stop sequence
                    StopTerminalProgressBar();
                    InteractionService.DisplayLines(buildOutputCollector.GetLines());
                    InteractionService.DisplayError(InteractionServiceStrings.ProjectCouldNotBeBuilt);
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }

            var outputPath = parseResult.GetValue<string?>("--output-path");
            var fullyQualifiedOutputPath = outputPath != null ? Path.GetFullPath(outputPath) : null;

            var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var operationRunOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = operationOutputCollector.AppendOutput,
                StandardErrorCallback = operationOutputCollector.AppendError,
                NoLaunchProfile = true,
                NoExtensionLaunch = true
            };

            var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostFile,
                false,
                true,
                GetRunArguments(fullyQualifiedOutputPath, unmatchedTokens, parseResult),
                env,
                backchannelCompletionSource,
                operationRunOptions,
                cancellationToken);

            // If we use the --wait-for-debugger option we print out the process ID
            // of the apphost so that the user can attach to it.
            if (waitForDebugger)
            {
                InteractionService.DisplayMessage("bug", InteractionServiceStrings.WaitingForDebuggerToAttachToAppHost);
            }

            var backchannel = await InteractionService.ShowStatusAsync($":hammer_and_wrench:  {GetProgressMessage()}", async () =>
            {
                return await backchannelCompletionSource.Task.ConfigureAwait(false);
            });

            var publishingActivities = backchannel.GetPublishingActivitiesAsync(cancellationToken);

            var debugMode = parseResult.GetValue<bool?>("--debug") ?? false;

            var noFailuresReported = debugMode switch
            {
                true => await ProcessPublishingActivitiesDebugAsync(publishingActivities, backchannel, cancellationToken),
                false => await ProcessAndDisplayPublishingActivitiesAsync(publishingActivities, backchannel, cancellationToken),
            };

            // Send terminal progress bar stop sequence
            StopTerminalProgressBar();

            await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
            var exitCode = await pendingRun;

            if (exitCode == 0 && noFailuresReported)
            {
                return ExitCodeConstants.Success;
            }

            if (debugMode)
            {
                InteractionService.DisplayLines(operationOutputCollector.GetLines());
            }

            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (OperationCanceledException)
        {
            // Send terminal progress bar stop sequence on cancellation
            StopTerminalProgressBar();
            InteractionService.DisplayError(GetCanceledMessage());
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (ProjectLocatorException ex)
        {
            // Send terminal progress bar stop sequence on exception
            StopTerminalProgressBar();
            return HandleProjectLocatorException(ex, InteractionService);
        }
        catch (AppHostIncompatibleException ex)
        {
            // Send terminal progress bar stop sequence on exception
            StopTerminalProgressBar();
            return InteractionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            // Send terminal progress bar stop sequence on exception
            StopTerminalProgressBar();
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message));
            InteractionService.DisplayLines(operationOutputCollector.GetLines());
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (Exception ex)
        {
            // Send terminal progress bar stop sequence on exception
            StopTerminalProgressBar();
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
    }

    public async Task<bool> ProcessPublishingActivitiesDebugAsync(IAsyncEnumerable<PublishingActivity> publishingActivities, IAppHostBackchannel backchannel, CancellationToken cancellationToken)
    {
        var stepCounter = 1;
        var steps = new Dictionary<string, string>();
        PublishingActivity? publishingActivity = null;

        await foreach (var activity in publishingActivities.WithCancellation(cancellationToken))
        {
            StartTerminalProgressBar();
            if (activity.Type == PublishingActivityTypes.PublishComplete)
            {
                publishingActivity = activity;
                break;
            }
            else if (activity.Type == PublishingActivityTypes.Step)
            {
                if (!steps.TryGetValue(activity.Data.Id, out var stepStatus))
                {
                    // New step - log it
                    InteractionService.DisplaySubtleMessage($"[DEBUG] Step {stepCounter++}: {activity.Data.StatusText}");
                    steps[activity.Data.Id] = activity.Data.CompletionState;
                }
                else if (IsCompletionStateComplete(activity.Data.CompletionState))
                {
                    // Step completed - log completion
                    var status = IsCompletionStateError(activity.Data.CompletionState) ? "FAILED" :
                        IsCompletionStateWarning(activity.Data.CompletionState) ? "WARNING" : "COMPLETED";
                    InteractionService.DisplaySubtleMessage($"[DEBUG] Step {activity.Data.Id}: {status} - {activity.Data.StatusText}");
                    steps[activity.Data.Id] = activity.Data.CompletionState;
                }
            }
            else if (activity.Type == PublishingActivityTypes.Prompt)
            {
                await HandlePromptActivityAsync(activity, backchannel, cancellationToken);
            }
            else
            {
                // Task activity - log it
                var stepId = activity.Data.StepId;
                if (IsCompletionStateComplete(activity.Data.CompletionState))
                {
                    var status = IsCompletionStateError(activity.Data.CompletionState) ? "FAILED" :
                        IsCompletionStateWarning(activity.Data.CompletionState) ? "WARNING" : "COMPLETED";
                    InteractionService.DisplaySubtleMessage($"[DEBUG] Task {activity.Data.Id} ({stepId}): {status} - {activity.Data.StatusText}");
                    if (!string.IsNullOrEmpty(activity.Data.CompletionMessage))
                    {
                        InteractionService.DisplaySubtleMessage($"[DEBUG]   {activity.Data.CompletionMessage}");
                    }
                }
                else
                {
                    InteractionService.DisplaySubtleMessage($"[DEBUG] Task {activity.Data.Id} ({stepId}): {activity.Data.StatusText}");
                }
            }
        }

        var hasErrors = publishingActivity is not null && IsCompletionStateError(publishingActivity.Data.CompletionState);
        var hasWarnings = publishingActivity is not null && IsCompletionStateWarning(publishingActivity.Data.CompletionState);

        if (publishingActivity is not null)
        {
            var status = hasErrors ? "FAILED" : hasWarnings ? "WARNING" : "COMPLETED";
            InteractionService.DisplaySubtleMessage($"[DEBUG] {OperationCompletedPrefix}: {status} - {publishingActivity.Data.StatusText}");

            // Send visual bell notification when operation is complete
            Console.Write("\a");
            Console.Out.Flush();
        }

        return !hasErrors;
    }

    public async Task<bool> ProcessAndDisplayPublishingActivitiesAsync(IAsyncEnumerable<PublishingActivity> publishingActivities, IAppHostBackchannel backchannel, CancellationToken cancellationToken)
    {
        var stepCounter = 1;
        var steps = new Dictionary<string, StepInfo>();
        var logger = new ConsoleActivityLogger(_hostEnvironment);
        logger.StartSpinner();
        PublishingActivity? publishingActivity = null;

        try
        {
            await foreach (var activity in publishingActivities.WithCancellation(cancellationToken))
            {
                StartTerminalProgressBar();
                if (activity.Type == PublishingActivityTypes.PublishComplete)
                {
                    publishingActivity = activity;
                    break;
                }
                else if (activity.Type == PublishingActivityTypes.Step)
                {
                    if (!steps.TryGetValue(activity.Data.Id, out var stepInfo))
                    {
                        stepInfo = new StepInfo
                        {
                            Id = activity.Data.Id,
                            Title = activity.Data.StatusText,
                            Number = stepCounter++,
                            StartTime = DateTime.UtcNow,
                            CompletionState = activity.Data.CompletionState
                        };

                        steps[activity.Data.Id] = stepInfo;
                        // Use the stable step Id for logger state tracking (prevents duplicate counting when titles repeat)
                        logger.StartTask(stepInfo.Id, stepInfo.Title, $"Starting {stepInfo.Title}...");
                    }
                    else if (IsCompletionStateComplete(activity.Data.CompletionState))
                    {
                        stepInfo.CompletionState = activity.Data.CompletionState;
                        stepInfo.CompletionText = activity.Data.StatusText;
                        stepInfo.EndTime = DateTime.UtcNow;
                        if (IsCompletionStateError(stepInfo.CompletionState))
                        {
                            logger.Failure(stepInfo.Id, stepInfo.CompletionText);
                        }
                        else if (IsCompletionStateWarning(stepInfo.CompletionState))
                        {
                            logger.Warning(stepInfo.Id, stepInfo.CompletionText);
                        }
                        else
                        {
                            logger.Success(stepInfo.Id, stepInfo.CompletionText);
                        }
                    }
                }
                else if (activity.Type == PublishingActivityTypes.Prompt)
                {
                    await logger.StopSpinnerAsync();
                    await HandlePromptActivityAsync(activity, backchannel, cancellationToken);
                    logger.StartSpinner();
                }
                else
                {
                    var stepId = activity.Data.StepId;
                    Debug.Assert(stepId != null, "Activity data should have a StepId for task activities.");

                    if (!steps.TryGetValue(stepId, out var stepInfo))
                    {
                        throw new InvalidOperationException($"Step '{stepId}' not found for task '{activity.Data.Id}'");
                    }

                    var tasks = stepInfo.Tasks;

                    if (!tasks.TryGetValue(activity.Data.Id, out var task))
                    {
                        task = new TaskInfo
                        {
                            Id = activity.Data.Id,
                            StatusText = activity.Data.StatusText,
                            StartTime = DateTime.UtcNow,
                            CompletionState = activity.Data.CompletionState
                        };

                        tasks[activity.Data.Id] = task;
                        logger.Progress(stepInfo.Id, activity.Data.StatusText);
                    }

                    task.StatusText = activity.Data.StatusText;
                    task.CompletionState = activity.Data.CompletionState;

                    if (IsCompletionStateComplete(activity.Data.CompletionState))
                    {
                        task.CompletionMessage = activity.Data.CompletionMessage;

                        var duration = DateTime.UtcNow - task.StartTime;
                        var durationStr = $"({duration.TotalSeconds:F1}s)";

                        var message = !string.IsNullOrEmpty(task.CompletionMessage)
                            ? $"{task.CompletionMessage} {durationStr}"
                            : $"{task.StatusText} {durationStr}";

                        if (IsCompletionStateError(task.CompletionState))
                        {
                            logger.Failure(stepInfo.Id, message);
                        }
                        else if (IsCompletionStateWarning(task.CompletionState))
                        {
                            logger.Warning(stepInfo.Id, message);
                        }
                        else
                        {
                            logger.Success(stepInfo.Id, message);
                        }

                        // If this task caused the step to fail, record a candidate failure reason if not already set.
                        if (IsCompletionStateError(task.CompletionState) && string.IsNullOrEmpty(stepInfo.FailureReason))
                        {
                            stepInfo.FailureReason = task.CompletionMessage ?? task.StatusText;
                        }
                    }
                }
            }

            if (publishingActivity is not null)
            {
                var hasErrors = IsCompletionStateError(publishingActivity.Data.CompletionState);
                var hasWarnings = IsCompletionStateWarning(publishingActivity.Data.CompletionState);
                // Determine first failed step (if any) for failure detail.
                string? failedStepTitle = null;
                string? failedStepMessage = null;
                if (hasErrors)
                {
                    var failedStep = steps.Values.FirstOrDefault(s => IsCompletionStateError(s.CompletionState));
                    if (failedStep is not null)
                    {
                        failedStepTitle = failedStep.Title;
                        failedStepMessage = failedStep.FailureReason ?? failedStep.CompletionText;
                    }
                }

                // Build duration breakdown (sorted by duration desc)
                var now = DateTime.UtcNow;
                var durationRecords = steps.Values.Select(s =>
                {
                    var end = s.EndTime ?? now;
                    var state = s.CompletionState switch
                    {
                        var cs when IsCompletionStateError(cs) => ConsoleActivityLogger.ActivityState.Failure,
                        var cs when IsCompletionStateWarning(cs) => ConsoleActivityLogger.ActivityState.Warning,
                        var cs when cs == CompletionStates.Completed => ConsoleActivityLogger.ActivityState.Success,
                        _ => ConsoleActivityLogger.ActivityState.InProgress
                    };
                    return new ConsoleActivityLogger.StepDurationRecord(
                        s.Id,
                        s.Title,
                        state,
                        end - s.StartTime,
                        s.FailureReason);
                })
                .OrderByDescending(r => r.Duration)
                .ToList();
                logger.SetStepDurations(durationRecords);

                // Provide final result to logger and print its structured summary.
                logger.SetFinalResult(!hasErrors);
                logger.WriteSummary();

                // Visual bell
                Console.Write("\a");
                Console.Out.Flush();
                return !hasErrors;
            }

            return true;
        }
        finally
        {
            await logger.StopSpinnerAsync();
        }
    }

    private static string BuildPromptText(PublishingPromptInput input, int inputCount, string statusText)
    {
        if (inputCount > 1)
        {
            // Multi-input: just show the label with markdown conversion
            var labelText = MarkdownToSpectreConverter.ConvertToSpectre($"{input.Label}: ");
            return labelText;
        }

        // Single-input: show both StatusText and Label
        var header = statusText ?? string.Empty;
        var label = input.Label ?? string.Empty;

        // If StatusText equals Label (case-insensitive), show only the label once
        if (header.Equals(label, StringComparison.OrdinalIgnoreCase))
        {
            return $"[bold]{MarkdownToSpectreConverter.ConvertToSpectre(label)}[/]";
        }

        // Show StatusText as header (converted from markdown), then Label on new line
        var convertedHeader = MarkdownToSpectreConverter.ConvertToSpectre(header);
        var convertedLabel = MarkdownToSpectreConverter.ConvertToSpectre(label);
        return $"[bold]{convertedHeader}[/]\n{convertedLabel}: ";
    }

    private async Task HandlePromptActivityAsync(PublishingActivity activity, IAppHostBackchannel backchannel, CancellationToken cancellationToken)
    {
        if (activity.Data.IsComplete)
        {
            // Prompt is already completed, nothing to do
            return;
        }

        // Check if we have input information
        if (activity.Data.Inputs is not { Count: > 0 } inputs)
        {
            throw new InvalidOperationException("Prompt provided without input data.");
        }

        // Check for validation errors. If there are errors then this isn't the first time the user has been prompted.
        var hasValidationErrors = inputs.Any(input => input.ValidationErrors is { Count: > 0 });

        // For multiple inputs, display the activity status text as a header.
        // Don't display if there are validation errors. Validation errors means the header has already been displayed.
        if (!hasValidationErrors && inputs.Count > 1)
        {
            var headerText = MarkdownToSpectreConverter.ConvertToSpectre(activity.Data.StatusText);
            AnsiConsole.MarkupLine($"[bold]{headerText}[/]");
        }

        // Handle multiple inputs
        var answers = new PublishingPromptInputAnswer[inputs.Count];
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];

            string? result;

            // Get prompt for input if there are no validation errors (first time we've asked)
            // or there are validation errors and this input has an error.
            if (!hasValidationErrors || input.ValidationErrors is { Count: > 0 })
            {
                // Build the prompt text based on number of inputs
                var promptText = BuildPromptText(input, inputs.Count, activity.Data.StatusText);

                result = await HandleSingleInputAsync(input, promptText, cancellationToken);
            }
            else
            {
                result = input.Value;
            }

            answers[i] = new PublishingPromptInputAnswer
            {
                Value = result
            };
        }

        // Send all results as an array
        await backchannel.CompletePromptResponseAsync(activity.Data.Id, answers, cancellationToken);
    }

    private async Task<string?> HandleSingleInputAsync(PublishingPromptInput input, string promptText, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<InputType>(input.InputType, ignoreCase: true, out var inputType))
        {
            // Fallback to text if unknown type
            inputType = InputType.Text;
        }

        // Display any validation errors.
        if (input.ValidationErrors is { Count: > 0 } errors)
        {
            foreach (var error in errors)
            {
                InteractionService.DisplayError(error);
            }
        }

        return inputType switch
        {
            InputType.Text => await InteractionService.PromptForStringAsync(
                promptText,
                defaultValue: input.Value,
                required: input.Required,
                cancellationToken: cancellationToken),

            InputType.SecretText => await InteractionService.PromptForStringAsync(
                promptText,
                defaultValue: input.Value,
                isSecret: true,
                required: input.Required,
                cancellationToken: cancellationToken),

            InputType.Choice => await HandleSelectInputAsync(input, promptText, cancellationToken),

            InputType.Boolean => (await InteractionService.ConfirmAsync(promptText, defaultValue: ParseBooleanValue(input.Value), cancellationToken: cancellationToken)).ToString().ToLowerInvariant(),

            InputType.Number => await HandleNumberInputAsync(input, promptText, cancellationToken),

            _ => await InteractionService.PromptForStringAsync(promptText, defaultValue: input.Value, required: input.Required, cancellationToken: cancellationToken)
        };
    }

    private async Task<string?> HandleSelectInputAsync(PublishingPromptInput input, string promptText, CancellationToken cancellationToken)
    {
        if (input.Options is null || input.Options.Count == 0)
        {
            return await InteractionService.PromptForStringAsync(promptText, defaultValue: input.Value, required: input.Required, cancellationToken: cancellationToken);
        }

        // If AllowCustomChoice is enabled then add an "Other" option to the list.
        // CLI doesn't support custom values directly in selection prompts. Instead an "Other" option is added.
        // If "Other" is selected then the user is prompted to enter a custom value as text.
        var options = input.Options.ToList();
        if (input.AllowCustomChoice)
        {
            options.Add(KeyValuePair.Create(CustomChoiceValue, InteractionServiceStrings.CustomChoiceLabel));
        }

        // For Choice inputs, we can't directly set a default in PromptForSelectionAsync,
        // but we can reorder the options to put the default first or use a different approach
        var (value, displayText) = await InteractionService.PromptForSelectionAsync(
            promptText,
            options,
            choice => choice.Value,
            cancellationToken);

        if (value == CustomChoiceValue)
        {
            return await InteractionService.PromptForStringAsync(promptText, defaultValue: input.Value, required: input.Required, cancellationToken: cancellationToken);
        }

        AnsiConsole.MarkupLine($"{promptText} {displayText.EscapeMarkup()}");

        return value;
    }

    private async Task<string?> HandleNumberInputAsync(PublishingPromptInput input, string promptText, CancellationToken cancellationToken)
    {
        static ValidationResult Validator(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !double.TryParse(value, out _))
            {
                return ValidationResult.Error("Please enter a valid number.");
            }

            return ValidationResult.Success();
        }

        return await InteractionService.PromptForStringAsync(
            promptText,
            defaultValue: input.Value,
            validator: Validator,
            required: input.Required,
            cancellationToken: cancellationToken);
    }

    private static bool ParseBooleanValue(string? value)
    {
        return bool.TryParse(value, out var result) && result;
    }

    private class StepInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Number { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string CompletionState { get; set; } = CompletionStates.InProgress;
        public string CompletionText { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public Dictionary<string, TaskInfo> Tasks { get; } = [];
    }

    private class TaskInfo
    {
        public string Id { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public string CompletionState { get; set; } = CompletionStates.InProgress;
        public string? CompletionMessage { get; set; }
    }

    // Removed legacy PublishingOutputRenderer and ProgressContextInfo (spinner & step coloring now handled by ConsoleActivityLogger).

    /// <summary>
    /// Starts the terminal infinite progress bar.
    /// </summary>
    private void StartTerminalProgressBar()
    {
        // Skip terminal progress bar in non-interactive environments
        if (!_hostEnvironment.SupportsInteractiveOutput)
        {
            return;
        }
        Console.Write("\u001b]9;4;3\u001b\\");
    }

    /// <summary>
    /// Stops the terminal progress bar.
    /// </summary>
    private void StopTerminalProgressBar()
    {
        // Skip terminal progress bar in non-interactive environments
        if (!_hostEnvironment.SupportsInteractiveOutput)
        {
            return;
        }
        Console.Write("\u001b]9;4;0\u001b\\");
    }
}
