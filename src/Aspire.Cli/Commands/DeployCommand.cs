// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
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

    public DeployCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, ICliHostEnvironment hostEnvironment)
        : base("deploy", DeployCommandStrings.Description, runner, interactionService, projectLocator, telemetry, sdkInstaller, features, updateNotifier, executionContext, hostEnvironment)
    {
        _clearCacheOption = new Option<bool>("--clear-cache")
        {
            Description = "Clear the deployment cache associated with the current environment and do not save deployment state"
        };
        Options.Add(_clearCacheOption);
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

        baseArgs.AddRange(unmatchedTokens);

        return [.. baseArgs];
    }

    protected override string GetCanceledMessage() => DeployCommandStrings.DeploymentCanceled;

    protected override string GetProgressMessage() => PublishCommandStrings.GeneratingArtifacts;

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Extract and display the target environment
        var environment = GetEnvironmentFromParseResult(parseResult);
        InteractionService.DisplayMessage("rocket", string.Format(CultureInfo.CurrentCulture, DeployCommandStrings.DeployingToEnvironment, environment));
        InteractionService.DisplayEmptyLine();

        // Continue with the base execution
        return await base.ExecuteAsync(parseResult, cancellationToken);
    }

    private static string GetEnvironmentFromParseResult(ParseResult parseResult)
    {
        // Check for --environment in unmatched tokens
        var unmatchedTokens = parseResult.UnmatchedTokens.ToArray();
        
        for (int i = 0; i < unmatchedTokens.Length; i++)
        {
            var token = unmatchedTokens[i];
            
            // Check for --environment=Value format
            if (token.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase))
            {
                return token.Substring("--environment=".Length).ToLowerInvariant();
            }
            
            // Check for --environment Value format (space-separated)
            if (token.Equals("--environment", StringComparison.OrdinalIgnoreCase) && i + 1 < unmatchedTokens.Length)
            {
                return unmatchedTokens[i + 1].ToLowerInvariant();
            }
        }

        // Default to Production if not specified
        return "production";
    }
}
