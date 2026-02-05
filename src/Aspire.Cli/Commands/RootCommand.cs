// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

#if DEBUG
using System.Globalization;
using System.Diagnostics;
#endif

using Aspire.Cli.Commands.Sdk;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using BaseRootCommand = System.CommandLine.RootCommand;

namespace Aspire.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    public static readonly Option<bool> DebugOption = new("--debug", "-d")
    {
        Description = RootCommandStrings.DebugArgumentDescription,
        Recursive = true
    };

    public static readonly Option<bool> NonInteractiveOption = new("--non-interactive")
    {
        Description = "Run the command in non-interactive mode, disabling all interactive prompts and spinners",
        Recursive = true
    };

    public static readonly Option<bool> NoLogoOption = new("--nologo")
    {
        Description = RootCommandStrings.NoLogoArgumentDescription,
        Recursive = true
    };

    public static readonly Option<bool> WaitForDebuggerOption = new("--wait-for-debugger")
    {
        Description = RootCommandStrings.WaitForDebuggerArgumentDescription,
        Recursive = true,
        DefaultValueFactory = _ => false
    };

    public static readonly Option<bool> CliWaitForDebuggerOption = new("--cli-wait-for-debugger")
    {
        Description = RootCommandStrings.CliWaitForDebuggerArgumentDescription,
        Recursive = true,
        Hidden = true,
        DefaultValueFactory = _ => false
    };

    private readonly IInteractionService _interactionService;

    public RootCommand(
        NewCommand newCommand,
        InitCommand initCommand,
        RunCommand runCommand,
        StopCommand stopCommand,
        PsCommand psCommand,
        ResourcesCommand resourcesCommand,
        LogsCommand logsCommand,
        AddCommand addCommand,
        PublishCommand publishCommand,
        DeployCommand deployCommand,
        DoCommand doCommand,
        ConfigCommand configCommand,
        CacheCommand cacheCommand,
        DoctorCommand doctorCommand,
        ExecCommand execCommand,
        UpdateCommand updateCommand,
        McpCommand mcpCommand,
        AgentCommand agentCommand,
        TelemetryCommand telemetryCommand,
        SdkCommand sdkCommand,
        DapCommand dapCommand,
        ExtensionInternalCommand extensionInternalCommand,
        IFeatures featureFlags,
        IInteractionService interactionService)
        : base(RootCommandStrings.Description)
    {
        _interactionService = interactionService;

#if DEBUG
        CliWaitForDebuggerOption.Validators.Add((result) =>
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

                        Debugger.Break();
                    });
            }
        });
#endif

        Options.Add(DebugOption);
        Options.Add(NonInteractiveOption);
        Options.Add(NoLogoOption);
        Options.Add(WaitForDebuggerOption);
        Options.Add(CliWaitForDebuggerOption);

        Subcommands.Add(newCommand);
        Subcommands.Add(initCommand);
        Subcommands.Add(runCommand);
        Subcommands.Add(stopCommand);
        Subcommands.Add(psCommand);
        Subcommands.Add(resourcesCommand);
        Subcommands.Add(logsCommand);
        Subcommands.Add(addCommand);
        Subcommands.Add(publishCommand);
        Subcommands.Add(configCommand);
        Subcommands.Add(cacheCommand);
        Subcommands.Add(doctorCommand);
        Subcommands.Add(deployCommand);
        Subcommands.Add(doCommand);
        Subcommands.Add(updateCommand);
        Subcommands.Add(extensionInternalCommand);
        Subcommands.Add(mcpCommand);
        Subcommands.Add(agentCommand);
        Subcommands.Add(telemetryCommand);

        if (featureFlags.IsFeatureEnabled(KnownFeatures.ExecCommandEnabled, false))
        {
            Subcommands.Add(execCommand);
        }

        if (featureFlags.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false))
        {
            Subcommands.Add(sdkCommand);
        }

        if (featureFlags.IsFeatureEnabled(KnownFeatures.DapEnabled, false))
        {
            Subcommands.Add(dapCommand);
        }
        else
        {
            Subcommands.Add(dapCommand);
        }

    }
}
