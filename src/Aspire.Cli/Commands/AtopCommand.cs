// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.UI;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

internal sealed class AtopCommand : BaseCommand
{
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly ILogger<AtopCommand> _logger;

    public AtopCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        ILogger<AtopCommand> logger)
        : base("atop", MonitorCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _backchannelMonitor = backchannelMonitor;
        _logger = logger;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var tui = new AspireAtopTui(_backchannelMonitor, _logger);
        await tui.RunAsync(cancellationToken).ConfigureAwait(false);

        return ExitCodeConstants.Success;
    }
}
