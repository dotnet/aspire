// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class DoCommand : PipelineCommandBase
{
    private readonly Argument<string> _stepArgument;
    private readonly IConfiguration _configuration;

    public DoCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment, IAppHostProjectFactory projectFactory, ILogger<DoCommand> logger, IAnsiConsole ansiConsole, IConfiguration configuration)
        : base("do", DoCommandStrings.Description, runner, interactionService, projectLocator, telemetry, sdkInstaller, features, updateNotifier, executionContext, hostEnvironment, projectFactory, logger, ansiConsole)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
        _stepArgument = new Argument<string>("step")
        {
            Description = DoCommandStrings.StepArgumentDescription,
            Arity = ArgumentArity.ZeroOrOne
        };
        Arguments.Add(_stepArgument);
    }

    protected override string OperationCompletedPrefix => DoCommandStrings.OperationCompletedPrefix;
    protected override string OperationFailedPrefix => DoCommandStrings.OperationFailedPrefix;
    protected override string GetOutputPathDescription() => DoCommandStrings.OutputPathArgumentDescription;

    protected override string[] GetRunArguments(string? fullyQualifiedOutputPath, string[] unmatchedTokens, ParseResult parseResult)
    {
        var baseArgs = new List<string> { "--operation", "publish" };

        var step = parseResult.GetValue(_stepArgument);
        if (!string.IsNullOrEmpty(step))
        {
            baseArgs.AddRange(["--step", step]);
        }

        if (fullyQualifiedOutputPath != null)
        {
            baseArgs.AddRange(["--output-path", fullyQualifiedOutputPath]);
        }

        // Add --log-level and --environment flags if specified
        var logLevel = parseResult.GetValue(s_logLevelOption);
        if (!string.IsNullOrEmpty(logLevel))
        {
            baseArgs.AddRange(["--log-level", logLevel!]);
        }

        var includeExceptionDetails = parseResult.GetValue(s_includeExceptionDetailsOption);
        if (includeExceptionDetails)
        {
            baseArgs.AddRange(["--include-exception-details", "true"]);
        }

        var environment = parseResult.GetValue(s_environmentOption);
        if (!string.IsNullOrEmpty(environment))
        {
            baseArgs.AddRange(["--environment", environment!]);
        }

        baseArgs.AddRange(unmatchedTokens);

        return [.. baseArgs];
    }

    protected override string GetCanceledMessage() => DoCommandStrings.OperationCanceled;

    protected override string GetProgressMessage(ParseResult parseResult)
    {
        var step = parseResult.GetValue(_stepArgument);
        return $"Executing step {step}";
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Check if running in extension mode with prompts enabled
        if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is "true")
        {
            var step = parseResult.GetValue(_stepArgument);
            if (string.IsNullOrEmpty(step))
            {
                return await InteractiveExecuteAsync(parseResult, cancellationToken);
            }
        }

        // Use the base implementation for non-interactive execution
        return await base.ExecuteAsync(parseResult, cancellationToken);
    }

    private async Task<int> InteractiveExecuteAsync(ParseResult originalParseResult, CancellationToken cancellationToken)
    {
        // Prompt for step name
        var step = await InteractionService.PromptForStringAsync(
            DoCommandStrings.PromptForStep,
            required: true,
            cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(step))
        {
            InteractionService.DisplayError(DoCommandStrings.StepRequired);
            return ExitCodeConstants.InvalidCommand;
        }

        // Prompt for environment - get current value from parseResult or default to "Production"
        var currentEnvironment = originalParseResult.GetValue(s_environmentOption);
        var environments = new[] { "Production", "Development", "Staging", DoCommandStrings.CustomEnvironmentOption };
        var selectedEnvironment = await InteractionService.PromptForSelectionAsync(
            DoCommandStrings.PromptForEnvironment,
            environments,
            env => env,
            cancellationToken);

        string? environment = selectedEnvironment;
        if (selectedEnvironment == DoCommandStrings.CustomEnvironmentOption)
        {
            environment = await InteractionService.PromptForStringAsync(
                DoCommandStrings.PromptForCustomEnvironment,
                defaultValue: currentEnvironment ?? "Production",
                required: true,
                cancellationToken: cancellationToken);
        }

        // Prompt for optional output path
        var currentOutputPath = originalParseResult.GetValue(s_projectOption)?.DirectoryName;
        var includeOutputPath = await InteractionService.ConfirmAsync(
            DoCommandStrings.PromptForOutputPathConfirm,
            defaultValue: false,
            cancellationToken);

        string? outputPath = null;
        if (includeOutputPath)
        {
            outputPath = await InteractionService.PromptForStringAsync(
                DoCommandStrings.PromptForOutputPath,
                defaultValue: currentOutputPath,
                required: false,
                cancellationToken: cancellationToken);
        }

        // Prompt for optional log level
        var currentLogLevel = originalParseResult.GetValue(s_logLevelOption);
        var includeLogLevel = await InteractionService.ConfirmAsync(
            DoCommandStrings.PromptForLogLevelConfirm,
            defaultValue: false,
            cancellationToken);

        string? logLevel = null;
        if (includeLogLevel)
        {
            var logLevels = new[] { "trace", "debug", "information", "warning", "error", "critical" };
            var defaultLogLevel = currentLogLevel ?? "information";
            logLevel = await InteractionService.PromptForSelectionAsync(
                DoCommandStrings.PromptForLogLevel,
                logLevels,
                level => level,
                cancellationToken);
        }

        // Build command arguments for re-parsing
        var args = new List<string> { step };

        if (!string.IsNullOrEmpty(environment))
        {
            args.AddRange(["--environment", environment]);
        }

        if (!string.IsNullOrEmpty(outputPath))
        {
            args.AddRange(["--output-path", outputPath]);
        }

        if (!string.IsNullOrEmpty(logLevel))
        {
            args.AddRange(["--log-level", logLevel]);
        }

        // Preserve other options from original parse result
        var projectOption = originalParseResult.GetValue(s_projectOption);
        if (projectOption != null)
        {
            args.AddRange(["--project", projectOption.FullName]);
        }

        var includeExceptionDetails = originalParseResult.GetValue(s_includeExceptionDetailsOption);
        if (includeExceptionDetails)
        {
            args.Add("--include-exception-details");
        }

        // Parse the arguments and execute
        var newParseResult = this.Parse(string.Join(" ", args));
        return await base.ExecuteAsync(newParseResult, cancellationToken);
    }
}
