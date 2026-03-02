// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class StartCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.ResourceManagement;

    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly AppHostLauncher _appHostLauncher;
    private readonly ILogger<StartCommand> _logger;

    private static readonly Argument<string?> s_resourceArgument = new("resource")
    {
        Description = ResourceCommandStrings.StartResourceArgumentDescription,
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<bool> s_noBuildOption = new("--no-build")
    {
        Description = RunCommandStrings.NoBuildArgumentDescription
    };

    public StartCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<StartCommand> logger,
        AspireCliTelemetry telemetry,
        AppHostLauncher appHostLauncher)
        : base("start", ResourceCommandStrings.StartDescription,
               features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);
        _appHostLauncher = appHostLauncher;
        _logger = logger;

        Arguments.Add(s_resourceArgument);
        Options.Add(s_noBuildOption);
        AppHostLauncher.AddLaunchOptions(this);

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(s_resourceArgument);
        var passedAppHostProjectFile = parseResult.GetValue(AppHostLauncher.s_appHostOption);
        var format = parseResult.GetValue(AppHostLauncher.s_formatOption);
        var isolated = parseResult.GetValue(AppHostLauncher.s_isolatedOption);

        // If a resource name is provided, start that specific resource
        if (!string.IsNullOrEmpty(resourceName))
        {
            if (format == OutputFormat.Json)
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.OptionNotValidWithResource, "--format"));
                return ExitCodeConstants.InvalidCommand;
            }

            if (isolated)
            {
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.OptionNotValidWithResource, "--isolated"));
                return ExitCodeConstants.InvalidCommand;
            }

            return await StartResourceAsync(passedAppHostProjectFile, resourceName, cancellationToken);
        }

        // No resource specified — start the AppHost in detached mode
        var noBuild = parseResult.GetValue(s_noBuildOption);
        var isExtensionHost = ExtensionHelper.IsExtensionHost(_interactionService, out _, out _);
        var globalArgs = RootCommand.GetChildProcessArgs(parseResult);
        var additionalArgs = parseResult.UnmatchedTokens.ToList();

        if (noBuild)
        {
            additionalArgs.Add("--no-build");
        }

        return await _appHostLauncher.LaunchDetachedAsync(
            passedAppHostProjectFile,
            format,
            isolated,
            isExtensionHost,
            globalArgs,
            additionalArgs,
            cancellationToken);
    }

    private async Task<int> StartResourceAsync(FileInfo? passedAppHostProjectFile, string resourceName, CancellationToken cancellationToken)
    {
        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            SharedCommandStrings.ScanningForRunningAppHosts,
            string.Format(CultureInfo.CurrentCulture, SharedCommandStrings.SelectAppHost, ResourceCommandStrings.SelectAppHostAction),
            SharedCommandStrings.AppHostNotRunning,
            cancellationToken);

        if (!result.Success)
        {
            _interactionService.DisplayError(result.ErrorMessage);
            return ExitCodeConstants.FailedToFindProject;
        }

        return await ResourceCommandHelper.ExecuteResourceCommandAsync(
            result.Connection!,
            _interactionService,
            _logger,
            resourceName,
            KnownResourceCommands.StartCommand,
            "Starting",
            "start",
            "started",
            cancellationToken);
    }
}
