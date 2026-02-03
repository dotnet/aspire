// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class RestartCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly ILogger<RestartCommand> _logger;

    private static readonly Argument<string> s_resourceArgument = new("resource")
    {
        Description = ResourceCommandStrings.RestartResourceArgumentDescription
    };

    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = ResourceCommandStrings.ProjectOptionDescription
    };

    public RestartCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<RestartCommand> logger,
        AspireCliTelemetry telemetry)
        : base("restart", ResourceCommandStrings.RestartDescription, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);
        _logger = logger;

        Arguments.Add(s_resourceArgument);
        Options.Add(s_projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(s_resourceArgument)!;
        var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);

        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            ResourceCommandStrings.ScanningForRunningAppHosts,
            ResourceCommandStrings.SelectAppHost,
            ResourceCommandStrings.NoInScopeAppHostsShowingAll,
            ResourceCommandStrings.NoRunningAppHostsFound,
            cancellationToken);

        if (!result.Success)
        {
            _interactionService.DisplayError(result.ErrorMessage ?? ResourceCommandStrings.NoRunningAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }

        return await ResourceCommandHelper.ExecuteResourceCommandAsync(
            result.Connection!,
            _interactionService,
            _logger,
            resourceName,
            KnownResourceCommands.RestartCommand,
            "Restarting",
            "restarted",
            cancellationToken);
    }
}
