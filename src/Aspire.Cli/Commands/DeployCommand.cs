// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;
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
    public DeployCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier)
        : base("deploy", DeployCommandStrings.Description, runner, interactionService, projectLocator, telemetry, sdkInstaller, features, updateNotifier)
    {
    }

    protected override string OperationCompletedPrefix => DeployCommandStrings.OperationCompletedPrefix;
    protected override string OperationFailedPrefix => DeployCommandStrings.OperationFailedPrefix;

    protected override string GetOutputPathDescription() => DeployCommandStrings.OutputPathArgumentDescription;

    protected override string CreateDefaultOutputPath(ArgumentResult result)
    {
        try
        {
            // Get the project path to create a stable directory based on the source path
            var projectFile = result.GetValue<FileInfo?>("--project");
            var sourcePath = projectFile?.DirectoryName ?? Environment.CurrentDirectory;
            
            // Create a stable hash of the source path for the directory name using SHA256
            var sourceHash = SHA256.HashData(Encoding.UTF8.GetBytes(sourcePath));
            var hashString = Convert.ToHexString(sourceHash)[..8].ToLowerInvariant();
            
            // Use project-relative directory for security instead of system temp
            var outputDir = Path.Combine(sourcePath, ".aspire", "deploy", hashString);
            return outputDir;
        }
        catch
        {
            // Fallback to a simple directory if there are any issues
            return Path.Combine(Environment.CurrentDirectory, ".aspire", "deploy", "default");
        }
    }

    protected override string[] GetRunArguments(string fullyQualifiedOutputPath, string[] unmatchedTokens) =>
        ["--operation", "publish", "--publisher", "default", "--output-path", fullyQualifiedOutputPath, "--deploy", "true", ..unmatchedTokens];

    protected override string GetCanceledMessage() => DeployCommandStrings.DeploymentCanceled;

    protected override string GetProgressMessage() => PublishCommandStrings.GeneratingArtifacts;
}
