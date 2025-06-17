// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;

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

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
        Options.Add(projectOption);

        var outputPath = new Option<string>("--output-path", "-o");
        outputPath.Description = GetOutputPathDescription();
        outputPath.DefaultValueFactory = GetDefaultOutputPath;
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
                _interactionService.DisplayError("The project could not be built. For more information run with --debug switch.");
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
                _interactionService.DisplayMessage("bug", $"Waiting for debugger to attach to app host process");
            }

            var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);
            var publishingActivities = backchannel.GetPublishingActivitiesAsync(cancellationToken);

            var debugMode = parseResult.GetValue<bool?>("--debug") ?? false;

            var noFailuresReported = debugMode switch {
                true => await ProcessPublishingActivitiesAsync(publishingActivities, cancellationToken),
                false => await ProcessAndDisplayPublishingActivitiesAsync(publishingActivities, _interactionService, backchannel, cancellationToken),
            };

            await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
            var exitCode = await pendingRun;

            if (exitCode == 0 && noFailuresReported)
            {
                _interactionService.DisplaySuccess(GetSuccessMessage(fullyQualifiedOutputPath));
                return ExitCodeConstants.Success;
            }

            _interactionService.DisplayLines(operationOutputCollector.GetLines());
            _interactionService.DisplayError(GetFailureMessage(exitCode));
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayError(GetCanceledMessage());
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
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException("AspireHostingVersion is null")
                );
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError($"An error occurred while connecting to the app host. The app host possibly crashed before it was available: {ex.Message}");
            _interactionService.DisplayLines(operationOutputCollector.GetLines());
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError($"An unexpected error occurred: {ex.Message}");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
    }

    public static async Task<bool> ProcessPublishingActivitiesAsync(IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> publishingActivities, CancellationToken cancellationToken)
    {
        // Convert old format to new format for compatibility
        return await ProcessPublishingActivitiesAsync(ConvertToExtendedFormat(publishingActivities), cancellationToken);
    }

    public static async Task<bool> ProcessPublishingActivitiesAsync(IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError, string? PromptType, string? PromptData)> publishingActivities, CancellationToken cancellationToken)
    {
        var lastActivityUpdateLookup = new Dictionary<string, (string Id, string StatusText, bool IsComplete, bool IsError, bool IsPrompt)>();
        await foreach (var publishingActivity in publishingActivities.WithCancellation(cancellationToken))
        {
            lastActivityUpdateLookup[publishingActivity.Id] = (publishingActivity.Id, publishingActivity.StatusText, publishingActivity.IsComplete, publishingActivity.IsError, publishingActivity.PromptType != null);

            if (lastActivityUpdateLookup.Any(kvp => kvp.Value.IsError) || lastActivityUpdateLookup.All(kvp => kvp.Value.IsComplete))
            {
                // If we have an error or all tasks are complete then we can stop
                // processing the publishing activities. Return true if there are no errors.
                return lastActivityUpdateLookup.All(kvp => !kvp.Value.IsError);
            }
        }

        return true;
    }

    public static async Task<bool> ProcessAndDisplayPublishingActivitiesAsync(IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> publishingActivities, CancellationToken cancellationToken)
    {
        // Convert old format to new format for compatibility
        return await ProcessAndDisplayPublishingActivitiesAsync(ConvertToExtendedFormat(publishingActivities), cancellationToken);
    }

    private static async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError, string? PromptType, string? PromptData)> ConvertToExtendedFormat(IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> publishingActivities)
    {
        await foreach (var activity in publishingActivities)
        {
            yield return (activity.Id, activity.StatusText, activity.IsComplete, activity.IsError, null, null);
        }
    }

    public static async Task<bool> ProcessAndDisplayPublishingActivitiesAsync(IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError, string? PromptType, string? PromptData)> publishingActivities, CancellationToken cancellationToken)
    {
        return await ProcessAndDisplayPublishingActivitiesAsync(publishingActivities, null, null, cancellationToken);
    }

    public static async Task<bool> ProcessAndDisplayPublishingActivitiesAsync(IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError, string? PromptType, string? PromptData)> publishingActivities, IInteractionService? interactionService, IAppHostBackchannel? backchannel, CancellationToken cancellationToken)
    {
        var activityStates = new Dictionary<string, (string StatusText, bool IsComplete, bool IsError, bool IsPrompt, bool HasBeenDisplayed)>();
        var lastEllipsesUpdate = DateTime.UtcNow;
        var ellipsesCount = 0;
        var lastDisplayedActivity = string.Empty;

        await foreach (var publishingActivity in publishingActivities.WithCancellation(cancellationToken))
        {
            // Update the activity state
            var isPrompt = !string.IsNullOrEmpty(publishingActivity.PromptType);
            var previousState = activityStates.TryGetValue(publishingActivity.Id, out var prev) ? prev : default;
            activityStates[publishingActivity.Id] = (publishingActivity.StatusText, publishingActivity.IsComplete, publishingActivity.IsError, isPrompt, previousState.HasBeenDisplayed);

            // Handle different activity types
            if (publishingActivity.IsComplete && !publishingActivity.IsError)
            {
                // Activity completed successfully - clear any previous progress and show completion
                if (lastDisplayedActivity == publishingActivity.Id)
                {
                    // Clear the previous line by printing carriage return, spaces, and carriage return
                    Console.Write($"\r{new string(' ', 100)}\r");
                    lastDisplayedActivity = string.Empty;
                }
                AnsiConsole.MarkupLine($"[green]✓[/] {publishingActivity.StatusText}");
            }
            else if (publishingActivity.IsError)
            {
                // Activity failed - clear any previous progress and show error
                if (lastDisplayedActivity == publishingActivity.Id)
                {
                    Console.Write($"\r{new string(' ', 100)}\r");
                    lastDisplayedActivity = string.Empty;
                }
                AnsiConsole.MarkupLine($"[red]✗[/] {publishingActivity.StatusText}");
                return false;
            }
            else if (!string.IsNullOrEmpty(publishingActivity.PromptType) &&
                     !string.IsNullOrEmpty(publishingActivity.PromptData) &&
                     interactionService is not null &&
                     backchannel is not null)
            {
                try
                {
                    var response = await HandlePromptActivityAsync(
                        publishingActivity.PromptType,
                        publishingActivity.PromptData,
                        interactionService,
                        cancellationToken);

                    // Send the response back to the AppHost
                    await backchannel.SendPromptResponseAsync(publishingActivity.Id, response, cancellationToken);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]✗[/] Prompt failed: {ex.Message}");
                    return false;
                }
            }
            else if (!isPrompt)
            {
                // Activity is in progress (non-prompt) - show with animated ellipses and update inline
                var now = DateTime.UtcNow;
                if ((now - lastEllipsesUpdate).TotalMilliseconds > 500) // Update ellipses every 500ms
                {
                    ellipsesCount = (ellipsesCount + 1) % 4; // Cycle through 0, 1, 2, 3
                    lastEllipsesUpdate = now;
                }

                var ellipses = new string('.', ellipsesCount);
                var padding = new string(' ', 3 - ellipsesCount); // Pad to keep line length consistent

                // Create the progress message without ANSI markup for inline display
                var progressText = $"⏳ {publishingActivity.StatusText}{ellipses}{padding}";

                if (lastDisplayedActivity == publishingActivity.Id)
                {
                    // Update the same line in place
                    Console.Write($"\r{progressText}");
                }
                else
                {
                    // Clear any previous progress line if it was a different activity
                    if (!string.IsNullOrEmpty(lastDisplayedActivity))
                    {
                        Console.Write($"\r{new string(' ', 100)}\r");
                    }

                    // Show new activity - either first time or new activity
                    if (!previousState.HasBeenDisplayed)
                    {
                        Console.Write(progressText);
                        activityStates[publishingActivity.Id] = (publishingActivity.StatusText, publishingActivity.IsComplete, publishingActivity.IsError, isPrompt, true);
                    }
                    else
                    {
                        Console.Write(progressText);
                    }
                    lastDisplayedActivity = publishingActivity.Id;
                }
            }
        }

        // Ensure we end with a newline if there was any in-progress activity
        if (!string.IsNullOrEmpty(lastDisplayedActivity))
        {
            Console.WriteLine();
        }

        return true;
    }

    private static async Task<string?> HandlePromptActivityAsync(string promptType, string promptData, IInteractionService interactionService, CancellationToken cancellationToken)
    {
        try
        {
            return promptType switch
            {
                nameof(PromptActivityType.PromptForString) => await HandleStringPromptAsync(promptData, interactionService, cancellationToken),
                nameof(PromptActivityType.PromptForConfirmation) => await HandleConfirmationPromptAsync(promptData, interactionService, cancellationToken),
                nameof(PromptActivityType.PromptForSelection) => await HandleSelectionPromptAsync(promptData, interactionService, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown prompt type: {promptType}")
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to handle prompt of type {promptType}: {ex.Message}", ex);
        }
    }

    private static async Task<string?> HandleStringPromptAsync(string promptDataJson, IInteractionService interactionService, CancellationToken cancellationToken)
    {
#pragma warning disable IL2026, IL3050 // Suppress JSON serialization warnings for experimental feature
        var promptData = JsonSerializer.Deserialize<PromptForStringData>(promptDataJson)
            ?? throw new InvalidOperationException("Invalid prompt data for string prompt");
#pragma warning restore IL2026, IL3050

        return await interactionService.PromptForStringAsync(promptData.PromptText, promptData.DefaultValue, cancellationToken: cancellationToken);
    }

    private static async Task<string> HandleConfirmationPromptAsync(string promptDataJson, IInteractionService interactionService, CancellationToken cancellationToken)
    {
#pragma warning disable IL2026, IL3050 // Suppress JSON serialization warnings for experimental feature
        var promptData = JsonSerializer.Deserialize<PromptForConfirmationData>(promptDataJson)
            ?? throw new InvalidOperationException("Invalid prompt data for confirmation prompt");
#pragma warning restore IL2026, IL3050

        var result = await interactionService.ConfirmAsync(promptData.PromptText, promptData.DefaultValue, cancellationToken);
        return result.ToString().ToLowerInvariant();
    }

    private static async Task<string> HandleSelectionPromptAsync(string promptDataJson, IInteractionService interactionService, CancellationToken cancellationToken)
    {
#pragma warning disable IL2026, IL3050 // Suppress JSON serialization warnings for experimental feature
        var promptData = JsonSerializer.Deserialize<PromptForSelectionData>(promptDataJson)
            ?? throw new InvalidOperationException("Invalid prompt data for selection prompt");
#pragma warning restore IL2026, IL3050

        if (promptData.AllowMultiple)
        {
            // For now, let's use single selection and extend later for multi-select
            var selected = await interactionService.PromptForSelectionAsync(promptData.PromptText, promptData.Choices, choice => choice, cancellationToken);
            return selected ?? string.Empty;
        }
        else
        {
            var selected = await interactionService.PromptForSelectionAsync(promptData.PromptText, promptData.Choices, choice => choice, cancellationToken);
            return selected ?? string.Empty;
        }
    }
}
