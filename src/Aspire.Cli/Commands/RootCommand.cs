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
        Recursive = true
    };

    public static readonly Option<bool> CliWaitForDebuggerOption = new("--cli-wait-for-debugger")
    {
        Description = RootCommandStrings.CliWaitForDebuggerArgumentDescription,
        Recursive = true,
        Hidden = true
    };

    private readonly IInteractionService _interactionService;

    public RootCommand(
        NewCommand newCommand,
        InitCommand initCommand,
        RunCommand runCommand,
        StopCommand stopCommand,
        PsCommand psCommand,
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
        SdkCommand sdkCommand,
        ExtensionInternalCommand extensionInternalCommand,
        IFeatures featureFlags,
        IInteractionService interactionService)
        : base(RootCommandStrings.Description)
    {
        ArgumentNullException.ThrowIfNull(newCommand);
        ArgumentNullException.ThrowIfNull(initCommand);
        ArgumentNullException.ThrowIfNull(runCommand);
        ArgumentNullException.ThrowIfNull(stopCommand);
        ArgumentNullException.ThrowIfNull(psCommand);
        ArgumentNullException.ThrowIfNull(addCommand);
        ArgumentNullException.ThrowIfNull(publishCommand);
        ArgumentNullException.ThrowIfNull(configCommand);
        ArgumentNullException.ThrowIfNull(cacheCommand);
        ArgumentNullException.ThrowIfNull(doctorCommand);
        ArgumentNullException.ThrowIfNull(deployCommand);
        ArgumentNullException.ThrowIfNull(doCommand);
        ArgumentNullException.ThrowIfNull(updateCommand);
        ArgumentNullException.ThrowIfNull(execCommand);
        ArgumentNullException.ThrowIfNull(mcpCommand);
        ArgumentNullException.ThrowIfNull(sdkCommand);
        ArgumentNullException.ThrowIfNull(extensionInternalCommand);
        ArgumentNullException.ThrowIfNull(featureFlags);
        ArgumentNullException.ThrowIfNull(interactionService);

        _interactionService = interactionService;

        // Set default value factory for wait-for-debugger options
        WaitForDebuggerOption.DefaultValueFactory = (result) => false;
        CliWaitForDebuggerOption.DefaultValueFactory = (result) => false;

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

        if (featureFlags.IsFeatureEnabled(KnownFeatures.ExecCommandEnabled, false))
        {
            Subcommands.Add(execCommand);
        }

        if (featureFlags.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false))
        {
            Subcommands.Add(sdkCommand);
        }

    }
}
