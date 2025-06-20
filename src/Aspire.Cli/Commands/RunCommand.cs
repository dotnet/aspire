// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Rendering.Dashboard;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;
using StreamJsonRpc;

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
        Action finalSteps = () =>
        {
            _ansiConsole.Write(new ControlCode("\u001b[?1049l"));
        };
        int exitCode = 1;

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

            var useRichConsole = forceUseRichConsole || !debug;

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

            var dashboardState = new DashboardState();

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = (output) =>
                {
                    runOutputCollector.AppendOutput(output);
                    dashboardState.Updates.Writer.TryWrite((state, cancellationToken) =>
                    {
                        state.AppendOutput(output);
                        return Task.CompletedTask;
                    });
                },

                StandardErrorCallback = (error) => {
                    runOutputCollector.AppendError(error);
                    dashboardState.Updates.Writer.TryWrite((state, cancellationToken) =>
                    {
                        state.AppendError(error);
                        return Task.CompletedTask;
                    });
                }
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

            if (useRichConsole)
            {
                // We wait for the back channel to be created to signal that
                // the AppHost is ready to accept requests.
                var backchannel = await _interactionService.ShowStatusAsync(
                    $":linked_paperclips:  {RunCommandStrings.StartingAppHost}",
                    async () =>
                    {

                        // If we use the --wait-for-debugger option we print out the process ID
                        // of the apphost so that the user can attach to it.
                        if (waitForDebugger)
                        {
                            _interactionService.DisplayMessage("bug", InteractionServiceStrings.WaitingForDebuggerToAttachToAppHost);
                        }

                        // The wait for the debugger in the apphost is done inside the CreateBuilder(...) method
                        // before the backchannel is created, therefore waiting on the backchannel is a
                        // good signal that the debugger was attached (or timed out).
                        var backchannel = await backchannelCompletitionSource.Task.WaitAsync(cancellationToken);
                        return backchannel;
                    });

                var dashboardRenderable = new DashboardRenderable(dashboardState);

                // Start up an alternate console.
                _ansiConsole.Write(new ControlCode("\u001b[?1049h\u001b[H"));
                _ansiConsole.Clear();

                // Background job to get dashboard URLs and update state.
                StartRequestingDashboardUrls(dashboardState, backchannel, cancellationToken);
                StartProcessingKeyboardInput(dashboardState, cancellationToken);
                StartStreamingResourceStates(dashboardState, backchannel, cancellationToken);

                await _ansiConsole.Live(dashboardRenderable).StartAsync(async context =>
                {
                    // If we are running an apphost that has no
                    // resources in it then we want to display
                    // the message that there are no resources.
                    // That is why we immediately do a refresh.
                    context.Refresh();

                    try
                    {
                        while (true)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var update = await dashboardState.Updates.Reader.ReadAsync(cancellationToken);
                            await update(dashboardState, cancellationToken);
                            context.Refresh();
                        }
                    }
                    catch (ConnectionLostException ex) when (ex.InnerException is OperationCanceledException)
                    {
                        // This exception will be thrown if the cancellation request reaches the WaitForExitAsync
                        // call on the process and shuts down the apphost before the JsonRpc connection gets it meaning
                        // that the apphost side of the RPC connection will be closed. Therefore if we get a
                        // ConnectionLostException AND the inner exception is an OperationCancelledException we can
                        // asume that the apphost was shutdown and we can ignore it.
                    }
                    catch (OperationCanceledException)
                    {
                        // This exception will be thrown if the cancellation request reaches the our side
                        // of the backchannel side first and the connection is torn down on our-side
                        // gracefully. We can ignore this exception as well.
                    }
                });

                var result = await pendingRun;
                if (result != 0)
                {
                    _interactionService.DisplayLines(runOutputCollector.GetLines());
                    _interactionService.DisplayError(RunCommandStrings.ProjectCouldNotBeRun);
                    return result;
                }
                else
                {
                    return ExitCodeConstants.Success;
                }
            }
            else
            {
                return await pendingRun;
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            finalSteps += _interactionService.DisplayCancellationMessage;
            exitCode = ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.ProjectFileDoesntExist, StringComparisons.CliInputOrOutput))
        {
            finalSteps += () =>
            {
                _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionDoesntExist);
            };
            exitCode = ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.MultipleProjectFilesFound, StringComparisons.CliInputOrOutput))
        {
            finalSteps += () =>
            {
                _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedMultipleAppHostsFound);
            };
            exitCode = ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
        {
            finalSteps += () =>
            {
                _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedNoCsprojFound);
            };
            exitCode = ExitCodeConstants.FailedToFindProject;
        }
        catch (AppHostIncompatibleException ex)
        {
            finalSteps += () =>
            {
                exitCode = _interactionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
            };
        }
        catch (CertificateServiceException ex)
        {
            finalSteps += () =>
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message));
            };
            exitCode = ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            finalSteps += () =>
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message));
                _interactionService.DisplayLines(runOutputCollector.GetLines());
            };
            exitCode = ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            finalSteps += () =>
            {
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                _ansiConsole.WriteException(ex, ExceptionFormats.Default);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message));
                _interactionService.DisplayLines(runOutputCollector.GetLines());
            };
            exitCode = ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        finally
        {
            finalSteps();
        }

        return exitCode;
    }

    private static void StartRequestingDashboardUrls(DashboardState dashboardState, IAppHostBackchannel backchannel, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var dashboardUrls = await backchannel.GetDashboardUrlsAsync(cancellationToken);
            await dashboardState.Updates.Writer.WriteAsync((state, cancellationToken) =>
            {
                state.DirectDashboardUrl = dashboardUrls.BaseUrlWithLoginToken;
                state.CodespacesDashboardUrl = dashboardUrls.CodespacesUrlWithLoginToken;
                return Task.CompletedTask;
            });
        }, cancellationToken);
    }

    private static void StartProcessingKeyboardInput(DashboardState dashboardState, CancellationToken cancellationToken)
    {
        // Keyboard input loop.
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                await dashboardState.Updates.Writer.WriteAsync((state, cancellationToken) =>
                {
                    // If the user presses V, show the logs view.
                    if (key.Key == ConsoleKey.V)
                    {
                        state.ShowAppHostLogs = !state.ShowAppHostLogs;
                    }
                    else
                    {
                        // Suppress all other inputs for now.
                    }

                    return Task.CompletedTask;
                });
            }
        }, cancellationToken);
    }

    private static void StartStreamingResourceStates(DashboardState dashboardState, IAppHostBackchannel backchannel, CancellationToken cancellationToken)
    {
        // Background job to get resource states and update state.
        _ = Task.Run(async () =>
        {
            try
            {
                var resourceStates = backchannel.GetResourceStatesAsync(cancellationToken);

                await foreach (var resourceState in resourceStates.WithCancellation(cancellationToken))
                {
                    await dashboardState.Updates.Writer.WriteAsync((state, cancellationToken) =>
                    {
                        state.ResourceStates[resourceState.Resource] = resourceState;
                        return Task.CompletedTask;
                    });
                }
            }
            catch (Exception ex)
            {
                // Here any exceptions we get from the background stream we
                // propogate up to the UI thread so our normal error handling
                // logic can take care of it. There are no-non fatal errors if
                // resource state streaming fails - but there are some cases
                // where we want to just silently exit (such as the case of
                // cancellation).
                dashboardState.Updates.Writer.Complete(ex);
            }
        }, cancellationToken);
    }
}
