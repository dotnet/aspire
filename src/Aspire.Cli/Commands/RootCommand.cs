// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using Microsoft.Extensions.Logging;

#if DEBUG
using System.Globalization;
using System.Diagnostics;
#endif

using Aspire.Cli.Bundles;
using Aspire.Cli.Commands.Sdk;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using BaseRootCommand = System.CommandLine.RootCommand;

namespace Aspire.Cli.Commands;

internal sealed class RootCommand : BaseRootCommand
{
    public static readonly Option<bool> DebugOption = new(CommonOptionNames.Debug, CommonOptionNames.DebugShort)
    {
        Description = RootCommandStrings.DebugArgumentDescription,
        Recursive = true,
        Hidden = true // Hidden for backward compatibility, use --debug-level instead
    };

    public static readonly Option<LogLevel?> DebugLevelOption = new("--debug-level", "-v")
    {
        Description = RootCommandStrings.DebugLevelArgumentDescription,
        Recursive = true
    };

    public static readonly Option<bool> NonInteractiveOption = new(CommonOptionNames.NonInteractive)
    {
        Description = "Run the command in non-interactive mode, disabling all interactive prompts and spinners",
        Recursive = true
    };

    public static readonly Option<bool> NoLogoOption = new(CommonOptionNames.NoLogo)
    {
        Description = RootCommandStrings.NoLogoArgumentDescription,
        Recursive = true
    };

    public static readonly Option<bool> BannerOption = new(CommonOptionNames.Banner)
    {
        Description = RootCommandStrings.BannerArgumentDescription,
        Recursive = true
    };

    public static readonly Option<bool> WaitForDebuggerOption = new(CommonOptionNames.WaitForDebugger)
    {
        Description = RootCommandStrings.WaitForDebuggerArgumentDescription,
        Recursive = true,
        DefaultValueFactory = _ => false
    };

    public static readonly Option<bool> CliWaitForDebuggerOption = new(CommonOptionNames.CliWaitForDebugger)
    {
        Description = RootCommandStrings.CliWaitForDebuggerArgumentDescription,
        Recursive = true,
        Hidden = true,
        DefaultValueFactory = _ => false
    };

    /// <summary>
    /// Global options that should be passed through to child CLI processes when spawning.
    /// Add new global options here to ensure they are forwarded during detached mode execution.
    /// </summary>
    private static readonly (Option Option, Func<ParseResult, string[]?> GetArgs)[] s_childProcessOptions =
    [
        (DebugOption, pr => pr.GetValue(DebugOption) ? ["--debug"] : null),
        (DebugLevelOption, pr =>
        {
            var level = pr.GetValue(DebugLevelOption);
            return level.HasValue ? ["--debug-level", level.Value.ToString()] : null;
        }),
        (WaitForDebuggerOption, pr => pr.GetValue(WaitForDebuggerOption) ? ["--wait-for-debugger"] : null),
    ];

    /// <summary>
    /// Gets the command-line arguments for global options that should be passed to a child CLI process.
    /// </summary>
    /// <param name="parseResult">The parse result from the current command invocation.</param>
    /// <returns>Arguments to pass to the child process.</returns>
    public static IEnumerable<string> GetChildProcessArgs(ParseResult parseResult)
    {
        foreach (var (_, getArgs) in s_childProcessOptions)
        {
            var args = getArgs(parseResult);
            if (args is not null)
            {
                foreach (var arg in args)
                {
                    yield return arg;
                }
            }
        }
    }

    private readonly IInteractionService _interactionService;

    public RootCommand(
        NewCommand newCommand,
        InitCommand initCommand,
        RunCommand runCommand,
        StopCommand stopCommand,
        StartCommand startCommand,
        RestartCommand restartCommand,
        WaitCommand waitCommand,
        ResourceCommand commandCommand,
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
        DocsCommand docsCommand,
        SdkCommand sdkCommand,
        SetupCommand setupCommand,
        ExtensionInternalCommand extensionInternalCommand,
        IBundleService bundleService,
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
        Options.Add(DebugLevelOption);
        Options.Add(NonInteractiveOption);
        Options.Add(NoLogoOption);
        Options.Add(BannerOption);
        Options.Add(WaitForDebuggerOption);
        Options.Add(CliWaitForDebuggerOption);

        // Handle standalone 'aspire' or 'aspire --banner' (no subcommand)
        this.SetAction((context, cancellationToken) =>
        {
            var bannerRequested = context.GetValue(BannerOption);
            if (bannerRequested)
            {
                // If --banner was passed, we've already shown it in Main, just exit successfully
                return Task.FromResult(ExitCodeConstants.Success);
            }

            // No subcommand provided - show grouped help but return InvalidCommand to signal usage error
            GroupedHelpWriter.WriteHelp(this, Console.Out);
            return Task.FromResult(ExitCodeConstants.InvalidCommand);
        });

        Subcommands.Add(newCommand);
        Subcommands.Add(initCommand);
        Subcommands.Add(runCommand);
        Subcommands.Add(stopCommand);
        Subcommands.Add(startCommand);
        Subcommands.Add(restartCommand);
        Subcommands.Add(waitCommand);
        Subcommands.Add(commandCommand);
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
        Subcommands.Add(docsCommand);

        if (bundleService.IsBundle)
        {
            Subcommands.Add(setupCommand);
        }

        if (featureFlags.IsFeatureEnabled(KnownFeatures.ExecCommandEnabled, false))
        {
            Subcommands.Add(execCommand);
        }

        if (featureFlags.IsFeatureEnabled(KnownFeatures.PolyglotSupportEnabled, false))
        {
            Subcommands.Add(sdkCommand);
        }

        // Replace the default --help action with grouped help output.
        foreach (var option in Options)
        {
            if (option is HelpOption helpOption)
            {
                helpOption.Action = new GroupedHelpAction(this);
                break;
            }
        }

    }
}
