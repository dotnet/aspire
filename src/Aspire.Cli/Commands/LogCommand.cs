// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Commands;

internal sealed class LogCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IServiceProvider _serviceProvider;

    public LogCommand(
        IInteractionService interactionService,
        IProjectLocator projectLocator,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        IServiceProvider serviceProvider)
        : base("log", LogCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(telemetry);

        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _telemetry = telemetry;
        _serviceProvider = serviceProvider;

        var resourceArgument = new Argument<string>("resource-name");
        resourceArgument.Description = LogCommandStrings.ResourceArgumentDescription;
        Arguments.Add(resourceArgument);

        var linesOption = new Option<int>("--lines", "-n")
        {
            Description = LogCommandStrings.LinesArgumentDescription
        };
        linesOption.DefaultValueFactory = (result) => 100;
        Options.Add(linesOption);

        var tailOption = new Option<bool>("--tail", "-f");
        tailOption.Description = LogCommandStrings.TailArgumentDescription;
        Options.Add(tailOption);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = LogCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        var resourceName = parseResult.GetValue<string>("resource-name");
        if (string.IsNullOrEmpty(resourceName))
        {
            _interactionService.DisplayError(LogCommandStrings.ResourceNameRequired);
            return ExitCodeConstants.InvalidCommand;
        }

        var lines = parseResult.GetValue<int>("--lines");
        var tail = parseResult.GetValue<bool>("--tail");
        
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

        if (effectiveAppHostProjectFile is null)
        {
            return ExitCodeConstants.FailedToFindProject;
        }

        // Connect to running AppHost
        var backchannel = _serviceProvider.GetRequiredService<IAppHostBackchannel>();
        
        try
        {
            // Get the predictable socket path based on AppHost project file
            var socketPath = BackchannelHelper.GetSocketPath(effectiveAppHostProjectFile.FullName);
            if (!BackchannelHelper.IsAppHostRunning(effectiveAppHostProjectFile.FullName))
            {
                _interactionService.DisplayError(LogCommandStrings.NoRunningAppHost);
                return ExitCodeConstants.FailedToConnectToAppHost;
            }

            await backchannel.ConnectAsync(socketPath, cancellationToken);

            if (tail)
            {
                _interactionService.DisplayMessage("information", string.Format(CultureInfo.CurrentCulture, LogCommandStrings.TailingLogs, resourceName));
                
                // For tailing, we continuously read logs
                await foreach (var logEntry in backchannel.GetResourceLogEntriesAsync(resourceName, null, cancellationToken))
                {
                    Console.WriteLine(FormatLogEntry(logEntry));
                }
            }
            else
            {
                _interactionService.DisplayMessage("information", string.Format(CultureInfo.CurrentCulture, LogCommandStrings.ShowingRecentLogs, resourceName, lines));

                // Show recent logs
                var logEntries = new List<BackchannelLogEntry>();
                await foreach (var logEntry in backchannel.GetResourceLogEntriesAsync(resourceName, lines, cancellationToken))
                {
                    logEntries.Add(logEntry);
                }

                foreach (var entry in logEntries)
                {
                    Console.WriteLine(FormatLogEntry(entry));
                }
            }
            
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, LogCommandStrings.FailedToGetLogs, resourceName, ex.Message));
            return ExitCodeConstants.FailedOperation;
        }
    }

    private static string FormatLogEntry(BackchannelLogEntry entry)
    {
        var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return $"[{timestamp}] {entry.Message}";
    }
}