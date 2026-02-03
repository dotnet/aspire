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

internal sealed class StartCommand : ResourceCommandBase
{
    protected override string CommandName => KnownResourceCommands.StartCommand;
    protected override string DisplayVerb => "Starting";
    protected override string PastTenseVerb => "started";
    protected override string ResourceArgumentDescription => ResourceCommandStrings.StartResourceArgumentDescription;

    public StartCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<StartCommand> logger,
        AspireCliTelemetry telemetry)
        : base("start", ResourceCommandStrings.StartDescription,
               interactionService, backchannelMonitor, features, updateNotifier,
               executionContext, logger, telemetry)
    {
    }
}
