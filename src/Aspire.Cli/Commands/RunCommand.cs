// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;
using Spectre.Console.Rendering;
using StreamJsonRpc;

namespace Aspire.Cli.Commands;

internal sealed class RunCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;
    private readonly AspireCliActivityTelemetry _telemetry;

    public RunCommand(IDotNetCliRunner runner, IInteractionService interactionService, ICertificateService certificateService, IProjectLocator projectLocator, IAnsiConsole ansiConsole, AspireCliActivityTelemetry telemetry)
        : base("run", "Run an Aspire app host in development mode.")
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
        projectOption.Description = "The path to the Aspire app host project file.";
        Options.Add(projectOption);

        var watchOption = new Option<bool>("--watch", "-w");
        watchOption.Description = "Start project resources in watch mode.";
        Options.Add(watchOption);

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingSdkVersion)? appHostCompatibilityCheck = null;
        try
        {
            using var activity = _telemetry.ActivitySource.StartActivity();

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
                    _interactionService.DisplayError($"The project could not be built. For more information run with --debug switch.");
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }
            
            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, _telemetry, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException("IsCompatibleAppHost is null"))
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

            if (useRichConsole)
            {
                // We wait for the back channel to be created to signal that
                // the AppHost is ready to accept requests.
                var backchannel = await _interactionService.ShowStatusAsync(
                    ":linked_paperclips:  Starting Aspire app host...",
                    async () => {

                        // If we use the --wait-for-debugger option we print out the process ID
                        // of the apphost so that the user can attach to it.
                        if (waitForDebugger)
                        {
                            _interactionService.DisplayMessage("bug", $"Waiting for debugger to attach to app host process");
                        }

                        // The wait for the debugger in the apphost is done inside the CreateBuilder(...) method
                        // before the backchannel is created, therefore waiting on the backchannel is a 
                        // good signal that the debugger was attached (or timed out).
                        var backchannel = await backchannelCompletitionSource.Task.WaitAsync(cancellationToken);
                        return backchannel;
                    });

                // We wait for the first update of the console model via RPC from the AppHost.
                var dashboardUrls = await _interactionService.ShowStatusAsync(
                    ":chart_increasing:  Starting Aspire dashboard...",
                    () => backchannel.GetDashboardUrlsAsync(cancellationToken));

                _interactionService.DisplayDashboardUrls(dashboardUrls);

                var table = new Table().Border(TableBorder.Rounded);

                // Add columns
                table.AddColumn("Resource");
                table.AddColumn("Type");
                table.AddColumn("State");
                table.AddColumn("Health");
                table.AddColumn("Endpoint(s)");

                // We add a default row here to say that
                // there are no resources in the app host.
                // This will be replaced once the first
                // resource is streamed back from the
                // app host which should be almost immediate
                // if no resources are present.
                
                // Create placeholders based on number of columns defined.
                var placeholders = new Markup[table.Columns.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    placeholders[i] = new Markup("--");
                }
                table.Rows.Add(placeholders);

                var message = new Markup("Press [bold]Ctrl+C[/] to stop the app host and exit.");

                var renderables = new List<IRenderable> {
                    table,
                    message
                };
                var rows = new Rows(renderables);

                await _ansiConsole.Live(rows).StartAsync(async context =>
                {
                    // If we are running an apphost that has no
                    // resources in it then we want to display
                    // the message that there are no resources.
                    // That is why we immediately do a refresh.
                    context.Refresh();

                    var knownResources = new SortedDictionary<string, RpcResourceState>();

                    var resourceStates = backchannel.GetResourceStatesAsync(cancellationToken);

                    try
                    {
                        await foreach (var resourceState in resourceStates)
                        {
                            knownResources[resourceState.Resource] = resourceState;

                            table.Rows.Clear();

                            foreach (var knownResource in knownResources)
                            {
                                var nameRenderable = new Text(knownResource.Key, new Style().Foreground(Color.White));

                                var typeRenderable = new Text(knownResource.Value.Type, new Style().Foreground(Color.White));

                                var stateRenderable = knownResource.Value.State switch
                                {
                                    "Running" => new Text(knownResource.Value.State, new Style().Foreground(Color.Green)),
                                    "Starting" => new Text(knownResource.Value.State, new Style().Foreground(Color.LightGreen)),
                                    "FailedToStart" => new Text(knownResource.Value.State, new Style().Foreground(Color.Red)),
                                    "Waiting" => new Text(knownResource.Value.State, new Style().Foreground(Color.White)),
                                    "Unhealthy" => new Text(knownResource.Value.State, new Style().Foreground(Color.Yellow)),
                                    "Exited" => new Text(knownResource.Value.State, new Style().Foreground(Color.Grey)),
                                    "Finished" => new Text(knownResource.Value.State, new Style().Foreground(Color.Grey)),
                                    "NotStarted" => new Text(knownResource.Value.State, new Style().Foreground(Color.Grey)),
                                    _ => new Text(knownResource.Value.State ?? "Unknown", new Style().Foreground(Color.Grey))
                                };

                                var healthRenderable = knownResource.Value.Health switch
                                {
                                    "Healthy" => new Text(knownResource.Value.Health, new Style().Foreground(Color.Green)),
                                    "Degraded" => new Text(knownResource.Value.Health, new Style().Foreground(Color.Yellow)),
                                    "Unhealthy" => new Text(knownResource.Value.Health, new Style().Foreground(Color.Red)),
                                    null => new Text("Unknown", new Style().Foreground(Color.Grey)),
                                    _ => new Text(knownResource.Value.Health, new Style().Foreground(Color.Grey))
                                };

                                IRenderable endpointsRenderable = new Text("None");
                                if (knownResource.Value.Endpoints?.Length > 0)
                                {
                                    endpointsRenderable = new Rows(
                                        knownResource.Value.Endpoints.Select(e => new Text(e, new Style().Link(e)))
                                    );
                                }

                                table.AddRow(nameRenderable, typeRenderable, stateRenderable, healthRenderable, endpointsRenderable);
                            }

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

                var result =  await pendingRun;
                if (result != 0)
                {
                    _interactionService.DisplayLines(runOutputCollector.GetLines());
                    _interactionService.DisplayError($"The project could not be run. For more information run with --debug switch.");
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
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex) when (ex.Message == "Project file does not exist.")
        {
            _interactionService.DisplayError("The --project option specified a project that does not exist.");
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (ex.Message.Contains("Multiple project files"))
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
        catch (CertificateServiceException ex)
        {
            _interactionService.DisplayError($"An error occurred while trusting the certificates: {ex.Message}");
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            _interactionService.DisplayError($"An error occurred while connecting to the app host. The app host possibly crashed before it was available: {ex.Message}");
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError($"An unexpected error occurred: {ex.Message}");
            _interactionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }
}
