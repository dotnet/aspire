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

namespace Aspire.Cli.Commands;

internal sealed class DoCommand : PipelineCommandBase
{
    private readonly Argument<string> _stepArgument;

    public DoCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment)
        : base("do", DoCommandStrings.Description, runner, interactionService, projectLocator, telemetry, sdkInstaller, features, updateNotifier, executionContext, hostEnvironment)
    {
        _stepArgument = new Argument<string>("step")
        {
            Description = DoCommandStrings.StepArgumentDescription
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
        var logLevel = parseResult.GetValue(_logLevelOption);
        if (!string.IsNullOrEmpty(logLevel))
        {
            baseArgs.AddRange(["--log-level", logLevel!]);
        }

        var environment = parseResult.GetValue(_environmentOption);
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
}
