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
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Base class for commands that execute resource lifecycle operations (start, restart, etc.).
/// </summary>
internal abstract class ResourceCommandBase : BaseCommand
{
    protected readonly AppHostConnectionResolver ConnectionResolver;
    protected readonly ILogger Logger;

    private readonly Argument<string> _resourceArgument;

    protected static readonly OptionWithLegacy<FileInfo?> s_appHostOption = new("--apphost", "--project", SharedCommandStrings.AppHostOptionDescription);

    /// <summary>
    /// The resource command name to execute (e.g., KnownResourceCommands.StartCommand).
    /// </summary>
    protected abstract string CommandName { get; }

    /// <summary>
    /// The verb to display during progress (e.g., "Starting").
    /// </summary>
    protected abstract string ProgressVerb { get; }

    /// <summary>
    /// The base verb for error messages (e.g., "start").
    /// </summary>
    protected abstract string BaseVerb { get; }

    /// <summary>
    /// The past tense verb for success messages (e.g., "started").
    /// </summary>
    protected abstract string PastTenseVerb { get; }

    /// <summary>
    /// The description for the resource argument.
    /// </summary>
    protected abstract string ResourceArgumentDescription { get; }

    protected ResourceCommandBase(
        string name,
        string description,
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger logger,
        AspireCliTelemetry telemetry)
        : base(name, description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        ConnectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);
        Logger = logger;

        _resourceArgument = new Argument<string>("resource")
        {
            Description = ResourceArgumentDescription
        };

        Arguments.Add(_resourceArgument);
        Options.Add(s_appHostOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue(_resourceArgument)!;
        var passedAppHostProjectFile = parseResult.GetValue(s_appHostOption);

        var result = await ConnectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            SharedCommandStrings.ScanningForRunningAppHosts,
            string.Format(CultureInfo.CurrentCulture, SharedCommandStrings.SelectAppHost, ResourceCommandStrings.SelectAppHostAction),
            SharedCommandStrings.AppHostNotRunning,
            cancellationToken);

        if (!result.Success)
        {
            InteractionService.DisplayError(result.ErrorMessage);
            return ExitCodeConstants.FailedToFindProject;
        }

        return await ResourceCommandHelper.ExecuteResourceCommandAsync(
            result.Connection!,
            InteractionService,
            Logger,
            resourceName,
            CommandName,
            ProgressVerb,
            BaseVerb,
            PastTenseVerb,
            cancellationToken);
    }
}
