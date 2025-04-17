// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Spectre.Console;
using Spectre.Console.Rendering;
using StreamJsonRpc;

namespace Aspire.Cli.Commands;

internal sealed class RunCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource(nameof(RunCommand));
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;

    public RunCommand(IDotNetCliRunner runner, IInteractionService interactionService, ICertificateService certificateService, IProjectLocator projectLocator, IAnsiConsole ansiConsole)
        : base("run", "Run an Aspire app host in development mode.")
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(ansiConsole);

        _runner = runner;
        _interactionService = interactionService;
        _certificateService = certificateService;
        _projectLocator = projectLocator;
        _ansiConsole = ansiConsole;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
        projectOption.Validators.Add((result) => ProjectFileHelper.ValidateProjectOption(result, projectLocator));
        Options.Add(projectOption);

        var watchOption = new Option<bool>("--watch", "-w");
        watchOption.Description = "Start project resources in watch mode.";
        Options.Add(watchOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingSdkVersion)? appHostCompatibilityCheck = null;
        try
        {
            using var activity = _activitySource.StartActivity();

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = _projectLocator.UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var env = new Dictionary<string, string>();

            var debug = parseResult.GetValue<bool>("--debug");

            var waitForDebugger = parseResult.GetValue<bool>("--wait-for-debugger");

            var forceUseRichConsole = Environment.GetEnvironmentVariable(KnownConfigNames.ForceRichConsole) == "true";
            
            var useRichConsole = forceUseRichConsole || !debug && !waitForDebugger;

            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            try
            {
                await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
            }
            catch (Exception ex)
            {
                _interactionService.DisplayError($"An error occurred while trusting the certificates: {ex.Message}");
                return ExitCodeConstants.FailedToTrustCertificates;
            }

            var watch = parseResult.GetValue<bool>("--watch");

            if (!watch)
            {
                var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, _interactionService, effectiveAppHostProjectFile, cancellationToken);

                if (buildExitCode != 0)
                {
                    _interactionService.DisplayError($"The project could not be built. For more information run with --debug switch.");
                    return ExitCodeConstants.FailedToBuildArtifacts;
                }
            }
            
            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, _interactionService, effectiveAppHostProjectFile, cancellationToken);

            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException("IsCompatibleAppHost is null"))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

            var pendingRun = _runner.RunAsync(
                effectiveAppHostProjectFile,
                watch,
                !watch,
                Array.Empty<string>(),
                env,
                backchannelCompletitionSource,
                cancellationToken);

            if (useRichConsole)
            {
                // We wait for the back channel to be created to signal that
                // the AppHost is ready to accept requests.
                var backchannel = await _interactionService.ShowStatusAsync(
                    ":linked_paperclips:  Starting Aspire app host...",
                    () => backchannelCompletitionSource.Task);

                // We wait for the first update of the console model via RPC from the AppHost.
                var dashboardUrls = await _interactionService.ShowStatusAsync(
                    ":chart_increasing:  Starting Aspire dashboard...",
                    () => backchannel.GetDashboardUrlsAsync(cancellationToken));

                _interactionService.DisplayDashboardUrls(dashboardUrls);

                var table = new Table().Border(TableBorder.Rounded);

                await _ansiConsole.Live(table).StartAsync(async context =>
                {
                    var knownResources = new SortedDictionary<string, (string Resource, string Type, string State, string[] Endpoints)>();

                    table.AddColumn("Resource");
                    table.AddColumn("Type");
                    table.AddColumn("State");
                    table.AddColumn("Endpoint(s)");

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

                                IRenderable endpointsRenderable = new Text("None");
                                if (knownResource.Value.Endpoints?.Length > 0)
                                {
                                    endpointsRenderable = new Rows(
                                        knownResource.Value.Endpoints.Select(e => new Text(e, new Style().Link(e)))
                                    );
                                }

                                table.AddRow(nameRenderable, typeRenderable, stateRenderable, endpointsRenderable);
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

                return await pendingRun;
            }
            else
            {
                return await pendingRun;
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _interactionService.DisplayMessage("stop_sign", "The run command was cancelled by user.");
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex) when (ex.Message == "Project file does not exist.")
        {
            _interactionService.DisplayError("The --project option specified a project that does not exist.");
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (ex.Message.Contains("Nultiple project files"))
        {
            _interactionService.DisplayError("The --project option was not specified and multiple *.csproj files were detected.");
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
    }
}
