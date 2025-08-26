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

internal sealed class StartCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IServiceProvider _serviceProvider;

    public StartCommand(
        IInteractionService interactionService,
        IProjectLocator projectLocator,
        AspireCliTelemetry telemetry,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        IServiceProvider serviceProvider)
        : base("start", StartCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(telemetry);

        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _telemetry = telemetry;
        _serviceProvider = serviceProvider;

        var resourceArgument = new Argument<string>("resource-name");
        resourceArgument.Description = StartCommandStrings.ResourceArgumentDescription;
        Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = StartCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        var resourceName = parseResult.GetValue<string>("resource-name");
        if (string.IsNullOrEmpty(resourceName))
        {
            _interactionService.DisplayError(StartCommandStrings.ResourceNameRequired);
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
            // Get the predictable socket path based on AppHost project file
            var socketPath = BackchannelHelper.GetSocketPath(effectiveAppHostProjectFile.FullName);
            if (!BackchannelHelper.IsAppHostRunning(effectiveAppHostProjectFile.FullName))
            {
                _interactionService.DisplayError(StartCommandStrings.NoRunningAppHost);
                return ExitCodeConstants.FailedToConnectToAppHost;
            }

            await backchannel.ConnectAsync(socketPath, cancellationToken);
            
            _interactionService.DisplayMessage("information", string.Format(CultureInfo.CurrentCulture, StartCommandStrings.StartingResource, resourceName));
            
            await backchannel.StartResourceAsync(resourceName, cancellationToken);
            
            _interactionService.DisplayMessage("success", string.Format(CultureInfo.CurrentCulture, StartCommandStrings.ResourceStarted, resourceName));
            
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, StartCommandStrings.FailedToStartResource, resourceName, ex.Message));
            return ExitCodeConstants.FailedOperation;
        }
    }
}