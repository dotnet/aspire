// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal class ExecCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly IDotNetCliRunner _runner;
    private readonly ICertificateService _certificateService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAnsiConsole _ansiConsole;
    private readonly IDotNetSdkInstaller _sdkInstaller;
    private readonly ICliHostEnvironment _hostEnvironment;
    private readonly IFeatures _features;

    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = ExecCommandStrings.ProjectArgumentDescription
    };
    private static readonly Option<string> s_resourceOption = new("--resource", "-r")
    {
        Description = ExecCommandStrings.TargetResourceArgumentDescription
    };
    private static readonly Option<string> s_startResourceOption = new("--start-resource", "-s")
    {
        Description = ExecCommandStrings.StartTargetResourceArgumentDescription
    };
    private static readonly Option<string> s_workdirOption = new("--workdir", "-w")
    {
        Description = ExecCommandStrings.WorkdirArgumentDescription
    };
    private static readonly Option<string> s_commandOption = new("--")
    {
        Description = ExecCommandStrings.CommandArgumentDescription
    };

    public ExecCommand(
        IConfiguration configuration,
        IDotNetCliRunner runner,
        IInteractionService interactionService,
        ICertificateService certificateService,
        IProjectLocator projectLocator,
        IAnsiConsole ansiConsole,
        AspireCliTelemetry telemetry,
        IDotNetSdkInstaller sdkInstaller,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment)
        : base("exec", ExecCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(ansiConsole);
        ArgumentNullException.ThrowIfNull(sdkInstaller);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(features);

        _configuration = configuration;
        _runner = runner;
        _certificateService = certificateService;
        _projectLocator = projectLocator;
        _ansiConsole = ansiConsole;
        _sdkInstaller = sdkInstaller;
        _hostEnvironment = hostEnvironment;
        _features = features;

        Options.Add(s_projectOption);
        Options.Add(s_resourceOption);
        Options.Add(s_startResourceOption);
        Options.Add(s_workdirOption);
        // only for --help output
        Options.Add(s_commandOption);

        TreatUnmatchedTokensAsErrors = false;
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Check if running in extension mode with prompts enabled
        if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is "true")
        {
            // If no resource and no command specified, use interactive mode
            var checkResource = parseResult.GetValue(s_resourceOption) ?? parseResult.GetValue(s_startResourceOption);
            if (checkResource is null && parseResult.UnmatchedTokens.Count == 0)
            {
                return await InteractiveExecuteAsync(cancellationToken);
            }
        }

        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, InteractionService, _features, Telemetry, _hostEnvironment, cancellationToken))
        {
            return ExitCodeConstants.SdkNotInstalled;
        }

        // validate required arguments firstly to fail fast if not found
        var targetResourceMode = "--resource";
        var targetResource = parseResult.GetValue(s_resourceOption);
        if (string.IsNullOrEmpty(targetResource))
        {
            targetResourceMode = "--start-resource";
            targetResource = parseResult.GetValue(s_startResourceOption);
        }

        if (targetResource is null)
        {
            InteractionService.DisplayError(ExecCommandStrings.TargetResourceNotSpecified);
            return ExitCodeConstants.InvalidCommand;
        }

        // unmatched tokens are those which will be tried to parse as command.
        // if none - we should fail fast
        if (parseResult.UnmatchedTokens.Count == 0)
        {
            InteractionService.DisplayError(ExecCommandStrings.NoCommandSpecified);
            return ExitCodeConstants.InvalidCommand;
        }

        var (arbitraryFlags, commandTokens) = ParseCmdArgs(parseResult);

        if (commandTokens is null || commandTokens.Count == 0)
        {
            InteractionService.DisplayError(ExecCommandStrings.FailedToParseCommand);
            return ExitCodeConstants.InvalidCommand;
        }

        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        IAppHostCliBackchannel? backchannel = null;
        Task<int>? pendingRun = null;
        int? commandExitCode = null;

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;
        try
        {
            using var activity = Telemetry.StartDiagnosticActivity(this.Name);

            var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);
            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, createSettingsFile: true, cancellationToken);

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            if (string.Equals(effectiveAppHostProjectFile.Extension, ".cs", StringComparison.OrdinalIgnoreCase))
            {
                InteractionService.DisplayError(ErrorStrings.CommandNotSupportedWithSingleFileAppHost);
                return ExitCodeConstants.SingleFileAppHostNotSupported;
            }

            var env = new Dictionary<string, string>();

            var waitForDebugger = parseResult.GetValue(RootCommand.WaitForDebuggerOption);
            if (waitForDebugger)
            {
                env[KnownConfigNames.WaitForDebugger] = "true";
            }

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, InteractionService, effectiveAppHostProjectFile, Telemetry, ExecutionContext.WorkingDirectory, cancellationToken);
            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
            };

            string[] args = [
                "--operation", "run",
                targetResourceMode, targetResource!,

                // a bit hacky, but in order to pass a full command with possible quotes and etc properly without losing the signature
                // we can wrap it in a string and pass it as a single argument
                "--command", $"\"{string.Join(" ", commandTokens)}\"",

                ..arbitraryFlags,
            ];

            try
            {
                var backchannelCompletionSource = new TaskCompletionSource<IAppHostCliBackchannel>();
                pendingRun = _runner.RunAsync(
                    projectFile: effectiveAppHostProjectFile,
                    watch: false,
                    noBuild: false,
                    args: args,
                    env: env,
                    backchannelCompletionSource: backchannelCompletionSource,
                    options: runOptions,
                    cancellationToken: cancellationToken);

                // We wait for the back channel to be created to signal that
                // the AppHost is ready to accept requests.
                backchannel = await InteractionService.ShowStatusAsync(
                    $":linked_paperclips:  {RunCommandStrings.StartingAppHost}",
                    async () =>
                    {
                        // If we use the --wait-for-debugger option we print out the process ID
                        // of the apphost so that the user can attach to it.
                        if (waitForDebugger)
                        {
                            InteractionService.DisplayMessage(emoji: "bug", InteractionServiceStrings.WaitingForDebuggerToAttachToAppHost);
                        }

                        // The wait for the debugger in the apphost is done inside the CreateBuilder(...) method
                        // before the backchannel is created, therefore waiting on the backchannel is a
                        // good signal that the debugger was attached (or timed out).
                        var backchannel = await backchannelCompletionSource.Task.WaitAsync(cancellationToken);
                        return backchannel;
                    });

                commandExitCode = await InteractionService.ShowStatusAsync<int?>(
                    $":running_shoe: {ExecCommandStrings.Running}",
                    async () =>
                    {
                        // execute tool and stream the output
                        int? exitCode = null;
                        var outputStream = backchannel.ExecAsync(cancellationToken);
                        await foreach (var output in outputStream)
                        {
                            InteractionService.WriteConsoleLog(output.Text, output.LineNumber, output.Type, output.IsErrorMessage);
                            if (output.ExitCode is not null)
                            {
                                exitCode = output.ExitCode;
                            }
                        }

                        return exitCode;
                    });
            }
            finally
            {
                if (backchannel is not null)
                {
                    _ = await InteractionService.ShowStatusAsync<int>(
                    $":linked_paperclips: {ExecCommandStrings.StoppingAppHost}",
                    async () =>
                    {
                        await backchannel.RequestStopAsync(cancellationToken);
                        return ExitCodeConstants.Success;
                    });
                }
            }

            if (commandExitCode is not null)
            {
                // if there is a deterministic output of the command with exit code - we should display that
                return commandExitCode.Value;
            }

            if (pendingRun is not null)
            {
                var result = await pendingRun;
                if (result != 0)
                {
                    InteractionService.DisplayLines(runOutputCollector.GetLines());
                    InteractionService.DisplayError(RunCommandStrings.ProjectCouldNotBeRun);
                    return result;
                }
                else
                {
                    return ExitCodeConstants.Success;
                }
            }
            else
            {
                InteractionService.DisplayLines(runOutputCollector.GetLines());
                InteractionService.DisplayError(RunCommandStrings.ProjectCouldNotBeRun);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, InteractionService, Telemetry);
        }
        catch (AppHostIncompatibleException ex)
        {
            Telemetry.RecordError(ex.Message, ex);
            return InteractionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
        }
        catch (CertificateServiceException ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    /// <summary>
    /// Executes the exec command in interactive mode by prompting the user for the resource and command.
    /// This method is called when the command is run in extension mode without arguments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An exit code indicating the result of the operation.</returns>
    private async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
    {
        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, InteractionService, _features, Telemetry, _hostEnvironment, cancellationToken))
        {
            return ExitCodeConstants.SdkNotInstalled;
        }

        // Prompt for resource name
        var resourceName = await InteractionService.PromptForStringAsync(
            ExecCommandStrings.PromptForResource,
            required: true,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(resourceName))
        {
            InteractionService.DisplayError(ExecCommandStrings.TargetResourceNotSpecified);
            return ExitCodeConstants.InvalidCommand;
        }

        // Prompt for command
        var commandText = await InteractionService.PromptForStringAsync(
            ExecCommandStrings.PromptForCommand,
            required: true,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(commandText))
        {
            InteractionService.DisplayError(ExecCommandStrings.NoCommandSpecified);
            return ExitCodeConstants.InvalidCommand;
        }

        // Execute with the provided resource and command
        return await ExecuteWithResourceAndCommandAsync(
            resourceName,
            commandText,
            projectPath: null,
            workdir: null,
            useStartResource: false,
            cancellationToken);
    }

    /// <summary>
    /// Executes a command in the specified resource context.
    /// </summary>
    /// <param name="targetResource">The name of the resource to execute the command in.</param>
    /// <param name="command">The command string to execute.</param>
    /// <param name="projectPath">Optional path to the AppHost project file.</param>
    /// <param name="workdir">Optional working directory for the command execution.</param>
    /// <param name="useStartResource">Whether to wait for the resource to start before executing the command.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An exit code indicating the result of the operation.</returns>
    private async Task<int> ExecuteWithResourceAndCommandAsync(
        string targetResource,
        string command,
        FileInfo? projectPath,
        string? workdir,
        bool useStartResource,
        CancellationToken cancellationToken)
    {
        var targetResourceMode = useStartResource ? "--start-resource" : "--resource";
        var commandTokens = ParseCommandString(command);

        if (commandTokens.Count == 0)
        {
            InteractionService.DisplayError(ExecCommandStrings.FailedToParseCommand);
            return ExitCodeConstants.InvalidCommand;
        }

        var buildOutputCollector = new OutputCollector();
        var runOutputCollector = new OutputCollector();

        IAppHostCliBackchannel? backchannel = null;
        Task<int>? pendingRun = null;
        int? commandExitCode = null;

        (bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)? appHostCompatibilityCheck = null;
        try
        {
            using var activity = Telemetry.StartDiagnosticActivity(this.Name);

            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(projectPath, createSettingsFile: true, cancellationToken);

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            if (string.Equals(effectiveAppHostProjectFile.Extension, ".cs", StringComparison.OrdinalIgnoreCase))
            {
                InteractionService.DisplayError(ErrorStrings.CommandNotSupportedWithSingleFileAppHost);
                return ExitCodeConstants.SingleFileAppHostNotSupported;
            }

            var env = new Dictionary<string, string>();

            appHostCompatibilityCheck = await AppHostHelper.CheckAppHostCompatibilityAsync(_runner, InteractionService, effectiveAppHostProjectFile, Telemetry, ExecutionContext.WorkingDirectory, cancellationToken);
            if (!appHostCompatibilityCheck?.IsCompatibleAppHost ?? throw new InvalidOperationException(RunCommandStrings.IsCompatibleAppHostIsNull))
            {
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }

            var runOptions = new DotNetCliRunnerInvocationOptions
            {
                StandardOutputCallback = runOutputCollector.AppendOutput,
                StandardErrorCallback = runOutputCollector.AppendError,
            };

            var args = new List<string>
            {
                "--operation", "run",
                targetResourceMode, targetResource,
                "--command", $"\"{string.Join(" ", commandTokens)}\""
            };

            if (!string.IsNullOrEmpty(workdir))
            {
                args.Add("--workdir");
                args.Add(workdir);
            }

            try
            {
                var backchannelCompletionSource = new TaskCompletionSource<IAppHostCliBackchannel>();
                pendingRun = _runner.RunAsync(
                    projectFile: effectiveAppHostProjectFile,
                    watch: false,
                    noBuild: false,
                    args: [.. args],
                    env: env,
                    backchannelCompletionSource: backchannelCompletionSource,
                    options: runOptions,
                    cancellationToken: cancellationToken);

                backchannel = await InteractionService.ShowStatusAsync(
                    $":linked_paperclips:  {RunCommandStrings.StartingAppHost}",
                    async () =>
                    {
                        var backchannel = await backchannelCompletionSource.Task.WaitAsync(cancellationToken);
                        return backchannel;
                    });

                commandExitCode = await InteractionService.ShowStatusAsync<int?>(
                    $":running_shoe: {ExecCommandStrings.Running}",
                    async () =>
                    {
                        int? exitCode = null;
                        var outputStream = backchannel.ExecAsync(cancellationToken);
                        await foreach (var output in outputStream)
                        {
                            InteractionService.WriteConsoleLog(output.Text, output.LineNumber, output.Type, output.IsErrorMessage);
                            if (output.ExitCode is not null)
                            {
                                exitCode = output.ExitCode;
                            }
                        }

                        return exitCode;
                    });
            }
            finally
            {
                if (backchannel is not null)
                {
                    _ = await InteractionService.ShowStatusAsync<int>(
                    $":linked_paperclips: {ExecCommandStrings.StoppingAppHost}",
                    async () =>
                    {
                        await backchannel.RequestStopAsync(cancellationToken);
                        return ExitCodeConstants.Success;
                    });
                }
            }

            if (commandExitCode is not null)
            {
                return commandExitCode.Value;
            }

            if (pendingRun is not null)
            {
                var result = await pendingRun;
                if (result != 0)
                {
                    InteractionService.DisplayLines(runOutputCollector.GetLines());
                    InteractionService.DisplayError(RunCommandStrings.ProjectCouldNotBeRun);
                    return result;
                }
                else
                {
                    return ExitCodeConstants.Success;
                }
            }
            else
            {
                InteractionService.DisplayLines(runOutputCollector.GetLines());
                InteractionService.DisplayError(RunCommandStrings.ProjectCouldNotBeRun);
                return ExitCodeConstants.FailedToDotnetRunAppHost;
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            InteractionService.DisplayCancellationMessage();
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex)
        {
            return HandleProjectLocatorException(ex, InteractionService, Telemetry);
        }
        catch (AppHostIncompatibleException ex)
        {
            Telemetry.RecordError(ex.Message, ex);
            return InteractionService.DisplayIncompatibleVersionError(
                ex,
                appHostCompatibilityCheck?.AspireHostingVersion ?? throw new InvalidOperationException(ErrorStrings.AspireHostingVersionNull)
                );
        }
        catch (CertificateServiceException ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, TemplatingStrings.CertificateTrustError, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            return ExitCodeConstants.FailedToTrustCertificates;
        }
        catch (FailedToConnectBackchannelConnection ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.ErrorConnectingToAppHost, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
        catch (Exception ex)
        {
            var errorMessage = string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.UnexpectedErrorOccurred, ex.Message);
            Telemetry.RecordError(errorMessage, ex);
            InteractionService.DisplayError(errorMessage);
            InteractionService.DisplayLines(runOutputCollector.GetLines());
            return ExitCodeConstants.FailedToDotnetRunAppHost;
        }
    }

    /// <summary>
    /// Parses a command string into individual tokens, respecting quotes and escape characters.
    /// </summary>
    /// <param name="command">The command string to parse.</param>
    /// <returns>A list of command tokens.</returns>
    /// <remarks>
    /// This method handles:
    /// <list type="bullet">
    /// <item>Double quotes for grouping tokens with spaces</item>
    /// <item>Backslash escape sequences</item>
    /// <item>Whitespace as token delimiters</item>
    /// </list>
    /// Note: Unterminated quotes will consume all remaining characters.
    /// </remarks>
    private static List<string> ParseCommandString(string command)
    {
        var tokens = new List<string>();
        var currentToken = new System.Text.StringBuilder();
        bool inQuotes = false;
        bool escape = false;

        for (int i = 0; i < command.Length; i++)
        {
            char c = command[i];

            if (escape)
            {
                currentToken.Append(c);
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                continue;
            }

            currentToken.Append(c);
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }

    private (ICollection<string> arbitary, ICollection<string> command) ParseCmdArgs(ParseResult parseResult)
    {
        var allTokens = parseResult.UnmatchedTokens.ToList();
        int delimiterIndex = allTokens.IndexOf("--");
        List<string> arbitraryFlags = new();
        List<string> commandTokens = new();

        // Find the index of the first token that is not an option (doesn't start with '-') and is not the value for a known option
        // We'll use the options defined in this command to skip known option values
        var knownOptions = new HashSet<string>(Options.SelectMany(o => o.Aliases));
        int i = 0;
        while (i < allTokens.Count)
        {
            if (delimiterIndex >= 0 && i == delimiterIndex)
            {
                // Everything after -- is command
                commandTokens.AddRange(allTokens.Skip(i + 1));
                break;
            }

            var token = allTokens[i];
            if (knownOptions.Contains(token))
            {
                // Skip the option and its value (if it has one)
                var option = Options.FirstOrDefault(o => o.Aliases.Contains(token));
                if (option is not null)
                {
                    // If the option is not a bool, it expects a value
                    var isFlag = option.Arity.MaximumNumberOfValues == 0;
                    if (!isFlag && i + 1 < allTokens.Count)
                    {
                        i += 2;
                        continue;
                    }
                }
                i++;
                continue;
            }
            else if (token.StartsWith("-"))
            {
                // Arbitrary flag
                arbitraryFlags.Add(token);
                i++;
                continue;
            }
            else
            {
                // First non-option, non-flag token is the start of the command (if not using --)
                commandTokens.AddRange(allTokens.Skip(i));
                break;
            }
        }

        return (arbitraryFlags, commandTokens);
    }
}
