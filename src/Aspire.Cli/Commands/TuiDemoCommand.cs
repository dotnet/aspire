// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tui;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Demo command for testing the new TUI components.
/// </summary>
internal sealed class TuiDemoCommand : BaseCommand
{
    private readonly IAnsiConsole _console;
    private readonly IAuxiliaryBackchannelMonitor _monitor;

    public TuiDemoCommand(
        IAnsiConsole console,
        IAuxiliaryBackchannelMonitor monitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService)
        : base("tui-demo", "Demo the new TUI components", features, updateNotifier, executionContext, interactionService)
    {
        _console = console;
        _monitor = monitor;
        this.Hidden = true; // Hide from help since it's for testing
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        await TuiDemo.RunAsync(_console, _monitor, cancellationToken);
        return ExitCodeConstants.Success;
    }
}
