// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
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
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class LogCommand : BaseCommand
{
    private readonly IProjectLocator _projectLocator;
    private readonly IInteractionService _interactionService;
    private readonly IAnsiConsole _ansiConsole;
    private readonly ILogger<LogCommand> _logger;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IAppHostBackchannel _backchannel;

    public LogCommand(
        IProjectLocator projectLocator,
        IInteractionService interactionService,
        IAnsiConsole ansiConsole,
        ILogger<LogCommand> logger,
        AspireCliTelemetry telemetry,
        IAppHostBackchannel backchannel,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext)
        : base("log", LogCommandStrings.Description, features, updateNotifier, executionContext)
    {
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(backchannel);

        _projectLocator = projectLocator;
        _interactionService = interactionService;
        _ansiConsole = ansiConsole;
        _logger = logger;
        _telemetry = telemetry;
        _backchannel = backchannel;

        // Resource name argument (required)
        var resourceNameArgument = new Argument<string>("resource-name");
        resourceNameArgument.Description = LogCommandStrings.ResourceNameArgumentDescription;
        Arguments.Add(resourceNameArgument);

        // --lines|-n option
        var linesOption = new Option<int>("--lines", "-n");
        linesOption.Description = LogCommandStrings.LinesArgumentDescription;
        linesOption.DefaultValueFactory = (_) => 100;
        Options.Add(linesOption);

        // --tail|-f option  
        var tailOption = new Option<bool>("--tail", "-f");
        tailOption.Description = LogCommandStrings.TailArgumentDescription;
        tailOption.DefaultValueFactory = (_) => false;
        Options.Add(tailOption);

        // --project|-p option
        var projectOption = new Option<FileInfo?>("--project", "-p");
        projectOption.Description = LogCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue<string>("resource-name");
        var lines = parseResult.GetValue<int>("--lines");
        var tail = parseResult.GetValue<bool>("--tail");
        var projectFile = parseResult.GetValue<FileInfo?>("--project");

        // Validate arguments
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            _interactionService.DisplayError(LogCommandStrings.ResourceNameRequired);
            return ExitCodeConstants.InvalidArguments;
        }

        if (lines <= 0)
        {
            _interactionService.DisplayError(LogCommandStrings.InvalidLineCount);
            return ExitCodeConstants.InvalidArguments;
        }

        try
        {
            // Find the AppHost project
            var appHostProject = await _projectLocator.UseOrFindAppHostProjectFileAsync(projectFile, cancellationToken);
            if (appHostProject is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            // Generate deterministic socket path based on project path
            var socketPath = DotNetCliRunner.GetBackchannelSocketPath(appHostProject);

            if (!File.Exists(socketPath))
            {
                _interactionService.DisplayError(LogCommandStrings.NoRunningAppHost);
                return ExitCodeConstants.FailedToConnect;
            }

            // Connect to the backchannel
            _interactionService.ShowStatus(LogCommandStrings.ConnectingToAppHost, () => { });

            try
            {
                await _backchannel.ConnectAsync(socketPath, cancellationToken);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                // Clean up stale socket file
                try { File.Delete(socketPath); } catch { }
                _interactionService.DisplayError(LogCommandStrings.NoRunningAppHost);
                return ExitCodeConstants.FailedToConnect;
            }

            if (tail)
            {
                _interactionService.DisplayMessage("📄", string.Format(CultureInfo.CurrentCulture, LogCommandStrings.StreamingLogs, resourceName));
            }

            // Stream the logs
            await foreach (var logEntry in _backchannel.GetResourceLogEntriesAsync(resourceName, lines, tail, cancellationToken))
            {
                var formattedTime = logEntry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var message = $"[{formattedTime}] [{logEntry.LogLevel}] [{resourceName}] {logEntry.Message}";
                _ansiConsole.WriteLine(message);
            }

            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex)
        {
            return BaseCommand.HandleProjectLocatorException(ex, _interactionService);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User pressed Ctrl+C
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve logs");
            _interactionService.DisplayError($"Failed to retrieve logs: {ex.Message}");
            return ExitCodeConstants.UnknownError;
        }
    }
}