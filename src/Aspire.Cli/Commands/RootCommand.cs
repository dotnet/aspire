// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

#if DEBUG
using System.Diagnostics;
#endif

using Aspire.Cli.Interaction;

using BaseRootCommand = System.CommandLine.RootCommand;

namespace Aspire.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    private readonly IInteractionService _interactionService;

    public RootCommand(NewCommand newCommand, RunCommand runCommand, AddCommand addCommand, PublishCommand publishCommand, ConfigCommand configCommand, IInteractionService interactionService)
        : base("The Aspire CLI can be used to create, run, and publish Aspire-based applications.")
    {
        ArgumentNullException.ThrowIfNull(newCommand);
        ArgumentNullException.ThrowIfNull(runCommand);
        ArgumentNullException.ThrowIfNull(addCommand);
        ArgumentNullException.ThrowIfNull(publishCommand);
        ArgumentNullException.ThrowIfNull(configCommand);
        ArgumentNullException.ThrowIfNull(interactionService);
        
        _interactionService = interactionService;

        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Description = "Enable debug logging to the console.";
        debugOption.Recursive = true;
        Options.Add(debugOption);
        
        var waitForDebuggerOption = new Option<bool>("--wait-for-debugger");
        waitForDebuggerOption.Description = "Wait for a debugger to attach before executing the command.";
        waitForDebuggerOption.Recursive = true;
        waitForDebuggerOption.DefaultValueFactory = (result) => false;

        var cliWaitForDebuggerOption = new Option<bool>("--cli-wait-for-debugger");
        cliWaitForDebuggerOption.Description = "Wait for a debugger to attach before executing the command.";
        cliWaitForDebuggerOption.Recursive = true;
        cliWaitForDebuggerOption.Hidden = true;
        cliWaitForDebuggerOption.DefaultValueFactory = (result) => false;

        #if DEBUG
        cliWaitForDebuggerOption.Validators.Add((result) => {

            var waitForDebugger = result.GetValueOrDefault<bool>();

            if (waitForDebugger)
            {
                _interactionService.ShowStatus(
                    $":bug:  Waiting for debugger to attach to CLI process ID: {Environment.ProcessId}",
                    () => {
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
    }
}
