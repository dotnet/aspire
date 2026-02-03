// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class RestartCommand : ResourceCommandBase
{
    protected override string CommandName => KnownResourceCommands.RestartCommand;
    protected override string DisplayVerb => "Restarting";
    protected override string PastTenseVerb => "restarted";
    protected override string ResourceArgumentDescription => ResourceCommandStrings.RestartResourceArgumentDescription;

    public RestartCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<RestartCommand> logger,
        AspireCliTelemetry telemetry)
        : base("restart", ResourceCommandStrings.RestartDescription,
               interactionService, backchannelMonitor, features, updateNotifier,
               executionContext, logger, telemetry)
    {
    }
}
