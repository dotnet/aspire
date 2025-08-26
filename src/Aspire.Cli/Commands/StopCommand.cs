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

internal sealed class StopCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IServiceProvider _serviceProvider;

    public StopCommand(
        IInteractionService interactionService,
        IProjectLocator projectLocator,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        IServiceProvider serviceProvider)
        : base("stop", StopCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(telemetry);

        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _telemetry = telemetry;
        _serviceProvider = serviceProvider;

        var resourceArgument = new Argument<string>("resource-name");
        resourceArgument.Description = StopCommandStrings.ResourceArgumentDescription;
        Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = StopCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        var resourceName = parseResult.GetValue<string>("resource-name");
        if (string.IsNullOrEmpty(resourceName))
        {
            _interactionService.DisplayError(StopCommandStrings.ResourceNameRequired);
            return ExitCodeConstants.InvalidCommand;
        }

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
            // Find the socket path - this would need to be stored somewhere accessible
            var socketPath = GetAppHostSocketPath(effectiveAppHostProjectFile);
            if (socketPath is null)
            {
                _interactionService.DisplayError(StopCommandStrings.NoRunningAppHost);
                return ExitCodeConstants.FailedToConnectToAppHost;
            }

            await backchannel.ConnectAsync(socketPath, cancellationToken);
            
            _interactionService.DisplayMessage("information", string.Format(CultureInfo.CurrentCulture, StopCommandStrings.StoppingResource, resourceName));
            
            await backchannel.StopResourceAsync(resourceName, cancellationToken);
            
            _interactionService.DisplayMessage("success", string.Format(CultureInfo.CurrentCulture, StopCommandStrings.ResourceStopped, resourceName));
            
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, StopCommandStrings.FailedToStopResource, resourceName, ex.Message));
            return ExitCodeConstants.FailedOperation;
        }
    }

    private static string? GetAppHostSocketPath(FileInfo appHostProjectFile)
    {
        // This is a simplified implementation - in practice, we'd need a way to discover running AppHosts
        var socketDir = Path.Combine(Path.GetTempPath(), "aspire");
        var projectName = Path.GetFileNameWithoutExtension(appHostProjectFile.Name);
        var socketPath = Path.Combine(socketDir, $"{projectName}.sock");
        
        return File.Exists(socketPath) ? socketPath : null;
    }
}