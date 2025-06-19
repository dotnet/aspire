// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

#if DEBUG
using System.Globalization;
using System.Diagnostics;
#endif

using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using BaseRootCommand = System.CommandLine.RootCommand;

namespace Aspire.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    private readonly IInteractionService _interactionService;

    public RootCommand(
        NewCommand newCommand,
        RunCommand runCommand,
        AddCommand addCommand,
        PublishCommand publishCommand,
        DeployCommand deployCommand,
        ConfigCommand configCommand,
        ExecCommand execCommand,
        IInteractionService interactionService)
        : base(RootCommandStrings.Description)
    {
        ArgumentNullException.ThrowIfNull(newCommand);
        ArgumentNullException.ThrowIfNull(runCommand);
        ArgumentNullException.ThrowIfNull(addCommand);
        ArgumentNullException.ThrowIfNull(publishCommand);
        ArgumentNullException.ThrowIfNull(configCommand);
        ArgumentNullException.ThrowIfNull(deployCommand);
        ArgumentNullException.ThrowIfNull(execCommand);
        ArgumentNullException.ThrowIfNull(interactionService);

        _interactionService = interactionService;

        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Description = RootCommandStrings.DebugArgumentDescription;
        debugOption.Recursive = true;
        Options.Add(debugOption);

        var waitForDebuggerOption = new Option<bool>("--wait-for-debugger");
        waitForDebuggerOption.Description = RootCommandStrings.WaitForDebuggerArgumentDescription;
        waitForDebuggerOption.Recursive = true;
        waitForDebuggerOption.DefaultValueFactory = (result) => false;

        var cliWaitForDebuggerOption = new Option<bool>("--cli-wait-for-debugger");
        cliWaitForDebuggerOption.Description = RootCommandStrings.CliWaitForDebuggerArgumentDescription;
        cliWaitForDebuggerOption.Recursive = true;
        cliWaitForDebuggerOption.Hidden = true;
        cliWaitForDebuggerOption.DefaultValueFactory = (result) => false;

#if DEBUG
        cliWaitForDebuggerOption.Validators.Add((result) =>
        {

            var waitForDebugger = result.GetValueOrDefault<bool>();

            if (waitForDebugger)
            {
                _interactionService.ShowStatus(
                    $":bug:  {string.Format(CultureInfo.CurrentCulture, RootCommandStrings.WaitingForDebugger, Environment.ProcessId)}",
                    () =>
                    {
                        while (!Debugger.IsAttached)
                        {
                            Thread.Sleep(1000);
                        }
                    });
            }
        });
#endif

        Options.Add(waitForDebuggerOption);
        Options.Add(cliWaitForDebuggerOption);

        Subcommands.Add(newCommand);
        Subcommands.Add(runCommand);
        Subcommands.Add(addCommand);
        Subcommands.Add(publishCommand);
        Subcommands.Add(configCommand);
        Subcommands.Add(deployCommand);
        Subcommands.Add(execCommand);
    }
}
