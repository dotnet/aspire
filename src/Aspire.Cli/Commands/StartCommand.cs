// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class StartCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.AppCommands;

    private readonly AppHostLauncher _appHostLauncher;
    private readonly IInteractionService _interactionService;

    private static readonly Option<bool> s_noBuildOption = new("--no-build")
    {
        Description = RunCommandStrings.NoBuildArgumentDescription
    };

    public StartCommand(
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        AppHostLauncher appHostLauncher)
        : base("start", ResourceCommandStrings.StartDescription,
               features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _appHostLauncher = appHostLauncher;

        Options.Add(s_noBuildOption);
        AppHostLauncher.AddLaunchOptions(this);

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var passedAppHostProjectFile = parseResult.GetValue(AppHostLauncher.s_appHostOption);
        var format = parseResult.GetValue(AppHostLauncher.s_formatOption);
        var isolated = parseResult.GetValue(AppHostLauncher.s_isolatedOption);

        // Detect bare-word arguments that look like resource names and guide users
        // to the new 'aspire resource <name> start' syntax.
        var unmatchedTokens = parseResult.UnmatchedTokens;
        if (unmatchedTokens.Count > 0 && !unmatchedTokens[0].StartsWith('-'))
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.StartUnmatchedResourceHint, unmatchedTokens[0]));
            return ExitCodeConstants.InvalidCommand;
        }

        var noBuild = parseResult.GetValue(s_noBuildOption);
        var isExtensionHost = ExtensionHelper.IsExtensionHost(_interactionService, out _, out _);
        var globalArgs = RootCommand.GetChildProcessArgs(parseResult);
        var additionalArgs = unmatchedTokens.ToList();

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
}
