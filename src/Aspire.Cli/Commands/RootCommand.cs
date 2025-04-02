// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

#if DEBUG
using System.Diagnostics;
using Spectre.Console;
#endif

using BaseRootCommand = System.CommandLine.RootCommand;

namespace Aspire.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    public RootCommand(NewCommand newCommand, RunCommand runCommand, AddCommand addCommand, PublishCommand publishCommand) : base("Aspire CLI")
    {
        var debugOption = new Option<bool>("--debug", "-d");
        debugOption.Recursive = true;
        Options.Add(debugOption);
        
        var waitForDebuggerOption = new Option<bool>("--wait-for-debugger", "-w");
        waitForDebuggerOption.Recursive = true;
        waitForDebuggerOption.DefaultValueFactory = (result) => false;

        #if DEBUG
        waitForDebuggerOption.Validators.Add((result) => {

            var waitForDebugger = result.GetValueOrDefault<bool>();

            if (waitForDebugger)
            {
                AnsiConsole.Status().Start(
                    $":bug:  Waiting for debugger to attach to process ID: {Environment.ProcessId}",
                    context => {
                        while (!Debugger.IsAttached)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                );
            }
        });
        #endif

        Options.Add(waitForDebuggerOption);

        Subcommands.Add(newCommand);
        Subcommands.Add(runCommand);
        Subcommands.Add(addCommand);
        Subcommands.Add(publishCommand);
    }
}