// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class RunCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;
    private readonly AspireCliTelemetry _telemetry;

    public RunCommand(IDotNetCliRunner runner, IInteractionService interactionService, ICertificateService certificateService, IProjectLocator projectLocator, IAnsiConsole ansiConsole, AspireCliTelemetry telemetry)
        : base("run", RunCommandStrings.Description)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(telemetry);

        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _projectLocator = projectLocator;
        _ansiConsole = ansiConsole;
        _telemetry = telemetry;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = RunCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        var watchOption = new Option<bool>("--watch", "-w");
        watchOption.Description = RunCommandStrings.WatchArgumentDescription;
        Options.Add(watchOption);

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

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

            var debug = parseResult.GetValue<bool>("--debug");

            var waitForDebugger = parseResult.GetValue<bool>("--wait-for-debugger");

            var forceUseRichConsole = Environment.GetEnvironmentVariable(KnownConfigNames.ForceRichConsole) == "true";

            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);

            var watch = parseResult.GetValue<bool>("--watch");

            if (!watch)
            {
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
            }

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, _telemetry, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
            };

            var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostProjectFile,
                watch,
                !watch,
                unmatchedTokens,
                env,
                backchannelCompletitionSource,
                runOptions,
                cancellationToken);

            // Wait for the backchannel to be established.
            var backchannel = await _interactionService.ShowStatusAsync("Connecting to app host...", async () =>
            {
                return await backchannelCompletitionSource.Task.WaitAsync(cancellationToken);
            });

            var dashboardUrls = await _interactionService.ShowStatusAsync("Starting dashboard...", async () =>
            {
                return await backchannel.GetDashboardUrlsAsync(cancellationToken);
            });

            _interactionService.DisplayDashboardUrls(dashboardUrls);
            
            var logEntires = backchannel.GetAppHostLogEntriesAsync(cancellationToken);
            await foreach (var entry in logEntires.WithCancellation(cancellationToken))
            {
                _ansiConsole.MarkupLine($"[[{entry.Timestamp:HH:mm:ss}]] [[{entry.LogLevel}]] {entry.CategoryName}: {entry.Message}");
            }

            return await pendingRun;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
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
        catch (CertificateServiceException ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message));
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message));
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }
}
