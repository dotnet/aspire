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

internal sealed class DeployCommand : PublishCommandBase
{
    private readonly Option<bool> _clearCacheOption;
    private readonly Option<string?> _stepOption;

    public DeployCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment)
        : base("deploy", DeployCommandStrings.Description, runner, interactionService, projectLocator, telemetry, sdkInstaller, features, updateNotifier, executionContext, hostEnvironment)
    {
        _clearCacheOption = new Option<bool>("--clear-cache")
        {
            Description = "Clear the deployment cache associated with the current environment and do not save deployment state"
        };
        Options.Add(_clearCacheOption);

        _stepOption = new Option<string?>("--step")
        {
            Description = "Run a specific deployment step and its dependencies"
        };
        Options.Add(_stepOption);
    }

    protected override string OperationCompletedPrefix => DeployCommandStrings.OperationCompletedPrefix;
    protected override string OperationFailedPrefix => DeployCommandStrings.OperationFailedPrefix;
    protected override string GetOutputPathDescription() => DeployCommandStrings.OutputPathArgumentDescription;

    protected override string[] GetRunArguments(string? fullyQualifiedOutputPath, string[] unmatchedTokens, ParseResult parseResult)
    {
        var baseArgs = new List<string> { "--operation", "publish", "--publisher", "default" };

        if (fullyQualifiedOutputPath != null)
        {
            baseArgs.AddRange(["--output-path", fullyQualifiedOutputPath]);
        }

        baseArgs.AddRange(["--deploy", "true"]);

        var clearCache = parseResult.GetValue(_clearCacheOption);
        if (clearCache)
        {
            baseArgs.AddRange(["--clear-cache", "true"]);
        }

        // Add --log-level and --envionment flags if specified
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

        var step = parseResult.GetValue(_stepOption);
        if (step != null)
        {
            baseArgs.AddRange(["--step", step]);
        }

        baseArgs.AddRange(unmatchedTokens);

        return [.. baseArgs];
    }

    protected override string GetCanceledMessage() => DeployCommandStrings.DeploymentCanceled;

    protected override string GetProgressMessage() => PublishCommandStrings.GeneratingArtifacts;
}
