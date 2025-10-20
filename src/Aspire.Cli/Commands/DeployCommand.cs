// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// Computes the SHA256 hash of the AppHost path for organizing deployment artifacts.
    /// </summary>
    /// <param name="appHostFile">The AppHost project file.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    private static string ComputeAppHostSha(FileInfo appHostFile)
    {
        // Use the same logic as DistributedApplicationBuilder:
        // Hash the lowercase, full path of the AppHost file
        var appHostPath = Path.GetFullPath(appHostFile.FullName).ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(appHostPath);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Creates a deployment log writer for automatic logging of deployment activities.
    /// </summary>
    /// <param name="appHostFile">The AppHost project file.</param>
    /// <returns>A deployment log writer.</returns>
    protected override DeploymentLogWriter? CreateDeploymentLogWriter(FileInfo appHostFile)
    {
        try
        {
            var appHostSha = ComputeAppHostSha(appHostFile);
            return new DeploymentLogWriter(appHostSha);
        }
        catch
        {
            // If we can't create the log writer, return null (best-effort logging)
            return null;
        }
    }

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
}
