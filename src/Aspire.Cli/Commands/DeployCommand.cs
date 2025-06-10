// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
using Aspire.Hosting;

namespace Aspire.Cli.Commands;

internal sealed class DeployCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(DeployCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;

    public DeployCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator)
        : base("deploy", "Deploy an Aspire app host project to the supported targets.")
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

        var outputPathOption = new Option<string?>("--output-path", "-o");
        outputPathOption.Description = "The output path for deployment artifacts.";
        outputPathOption.DefaultValueFactory = (result) => Path.Combine(Environment.CurrentDirectory, "deploy");
        Options.Add(outputPathOption);

        // In deploy commands we forward all unrecognized tokens through to the underlying deployment tooling
        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var buildOutputCollector = new OutputCollector();
        var deployOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingSdkVersion)? appHostCompatibilityCheck = null;

        try
        {
            using var activity = _activitySource.StartActivity();

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var outputPath = parseResult.GetValue<string?>("--output-path") ?? Path.Combine(Environment.CurrentDirectory, "deploy");

            var fullyQualifiedOutputPath = Path.GetFullPath(outputPath);

            // Check app host compatibility
            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException("IsCompatibleAppHost is null"))
            {
                return ExitCodeConstants.AppHostIncompatible;
            }

            // Build the project
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

            var env = new Dictionary<string, string>();

            var waitForDebugger = parseResult.GetValue<bool?>("--wait-for-debugger") ?? false;
            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            var backchannelCompletionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var deployRunOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = deployOutputCollector.AppendOutput,
                StandardErrorCallback = deployOutputCollector.AppendError,
                NoLaunchProfile = true
            };

            var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostProjectFile,
                false,
                true,
                ["--operation", "publish", "--publisher", "default", "--output-path", fullyQualifiedOutputPath, "--deploy", "true", .. unmatchedTokens],
                env,
                backchannelCompletionSource,
                deployRunOptions,
                cancellationToken);

            // If we use the --wait-for-debugger option we print out the process ID
            if (waitForDebugger)
            {
                _interactionService.DisplayMessage("bug", "Waiting for debugger to attach to app host process");
            }

            var backchannel = await backchannelCompletionSource.Task.ConfigureAwait(false);

            // Reuse the Publishing activities activity source for the backchannel
            var deploymentActivities = backchannel.GetPublishingActivitiesAsync(cancellationToken);

            var debugMode = parseResult.GetValue<bool?>("--debug") ?? false;

            var noFailuresReported = debugMode switch
            {
                true => await PublishCommand.ProcessPublishingActivitiesAsync(deploymentActivities, cancellationToken),
                false => await PublishCommand.ProcessAndDisplayPublishingActivitiesAsync(deploymentActivities, cancellationToken),
            };

            await backchannel.RequestStopAsync(cancellationToken).ConfigureAwait(false);
            var exitCode = await pendingRun;

            if (exitCode == 0)
            {
                _interactionService.DisplaySuccess($"Successfully deployed. Artifacts available at: {fullyQualifiedOutputPath}");
                return ExitCodeConstants.Success;
            }

            _interactionService.DisplayLines(deployOutputCollector.GetLines());
            _interactionService.DisplayError($"Deployment failed with exit code {exitCode}. For more information run with --debug switch.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayError("The deployment was canceled.");
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
            _interactionService.DisplayLines(deployOutputCollector.GetLines());
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError($"An unexpected error occurred: {ex.Message}");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }
    }
}
