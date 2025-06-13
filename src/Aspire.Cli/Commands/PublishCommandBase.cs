// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
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

    protected PublishCommandBase(string name, string description, IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator)
        : base(name, description)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);

        _runner = runner;
        _interactionService = interactionService;
        _projectLocator = projectLocator;

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

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingSdkVersion)? appHostCompatibilityCheck = null;

        try
        {
            using var activity = AspireCliActivitySource.Instance.StartActivity();

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

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, cancellationToken);

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
                false => await ProcessAndDisplayPublishingActivitiesAsync(publishingActivities, cancellationToken),
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
                appHostCompatibilityCheck?.AspireHostingSdkVersion ?? throw new InvalidOperationException("AspireHostingSdkVersion is null")
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
        var lastActivityUpdateLookup = new Dictionary<string, (string Id, string StatusText, bool IsComplete, bool IsError)>();
        await foreach (var publishingActivity in publishingActivities.WithCancellation(cancellationToken))
        {
            lastActivityUpdateLookup[publishingActivity.Id] = publishingActivity;

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
        return await AnsiConsole.Progress()
            .AutoRefresh(true)
            .Columns(
                new TaskDescriptionColumn() { Alignment = Justify.Left },
                new ProgressBarColumn() { Width = 10 },
                new ElapsedTimeColumn())
            .StartAsync<bool>(async context => {

                var progressTasks = new Dictionary<string, ProgressTask>();

                await foreach (var publishingActivity in publishingActivities.WithCancellation(cancellationToken))
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
                        return false;
                    }
                }

                return true;
            });
    }
}
