// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interactivity;
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
    private readonly IInteractivityService _interactivityService;

    public RunCommand(IDotNetCliRunner runner, IInteractivityService interactivityService)
        : base("run", "Run an Aspire app host in development mode.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        ArgumentNullException.ThrowIfNull(interactivityService, nameof(interactivityService));

        _runner = runner;
        _interactivityService = interactivityService;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = "The path to the Aspire app host project file.";
        projectOption.Validators.Add(ProjectFileHelper.ValidateProjectOption);
        Options.Add(projectOption);

        var watchOption = new Option<bool>("--watch", "-w");
        watchOption.Description = "Start project resources in watch mode.";
        Options.Add(watchOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var effectiveAppHostProjectFile = ProjectFileHelper.UseOrFindAppHostProjectFile(passedAppHostProjectFile);
        
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
            await CertificatesHelper.EnsureCertificatesTrustedAsync(_interactivityService, _runner, cancellationToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down:  An error occurred while trusting the certificates: {ex.Message}[/]");
            return ExitCodeConstants.FailedToTrustCertificates;
        }

        var appHostCompatabilityCheck = await AppHostHelper.CheckAppHostCompatabilityAsync(_runner, effectiveAppHostProjectFile, cancellationToken);

        if (!appHostCompatabilityCheck.IsCompatableAppHost)
        {
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }

        var watch = parseResult.GetValue<bool>("--watch");

        var buildExitCode = await AppHostHelper.BuildAppHostAsync(_runner, effectiveAppHostProjectFile, cancellationToken);

        if (buildExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down: The project could not be built. For more information run with --debug switch.[/]");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        var backchannelCompletitionSource = new TaskCompletionSource<IAppHostBackchannel>();

        var pendingRun = _runner.RunAsync(
            effectiveAppHostProjectFile,
            watch,
            true,
            Array.Empty<string>(),
            env,
            backchannelCompletitionSource,
            cancellationToken);

        if (useRichConsole)
        {
            // We wait for the back channel to be created to signal that
            // the AppHost is ready to accept requests.
            var backchannel = await AnsiConsole.Status()
                                                .Spinner(Spinner.Known.Dots3)
                                                .SpinnerStyle(Style.Parse("purple"))
                                                .StartAsync(":linked_paperclips:  Starting Aspire app host...", async context => {
                                                    return await backchannelCompletitionSource.Task;
                                                });

            // We wait for the first update of the console model via RPC from the AppHost.
            var dashboardUrls = await AnsiConsole.Status()
                                                .Spinner(Spinner.Known.Dots3)
                                                .SpinnerStyle(Style.Parse("purple"))
                                                .StartAsync(":chart_increasing:  Starting Aspire dashboard...", async context => {
                                                    return await backchannel.GetDashboardUrlsAsync(cancellationToken);
                                                });

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green bold]Dashboard[/]:");
            if (dashboardUrls.CodespacesUrlWithLoginToken is not  null)
            {
                AnsiConsole.MarkupLine($":chart_increasing:  Direct: [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
                AnsiConsole.MarkupLine($":chart_increasing:  Codespaces: [link={dashboardUrls.CodespacesUrlWithLoginToken}]{dashboardUrls.CodespacesUrlWithLoginToken}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($":chart_increasing:  [link={dashboardUrls.BaseUrlWithLoginToken}]{dashboardUrls.BaseUrlWithLoginToken}[/]");
            }
            AnsiConsole.WriteLine();

            var table = new Table().Border(TableBorder.Rounded);

            await AnsiConsole.Live(table).StartAsync(async context => {

                var knownResources = new SortedDictionary<string, (string Resource, string Type, string State, string[] Endpoints)>();

                table.AddColumn("Resource");
                table.AddColumn("Type");
                table.AddColumn("State");
                table.AddColumn("Endpoint(s)");

                var resourceStates = backchannel.GetResourceStatesAsync(cancellationToken);

                try
                {
                    await foreach(var resourceState in resourceStates)
                    {
                        knownResources[resourceState.Resource] = resourceState;

                        table.Rows.Clear();

                        foreach (var knownResource in knownResources)
                        {
                            var nameRenderable = new Text(knownResource.Key, new Style().Foreground(Color.White));

                            var typeRenderable = new Text(knownResource.Value.Type, new Style().Foreground(Color.White));

                            var stateRenderable = knownResource.Value.State switch {
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
}