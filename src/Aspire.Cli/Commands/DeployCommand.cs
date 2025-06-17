// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;
using System.Globalization;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;

namespace Aspire.Cli.Commands;

internal sealed class DeployCommand : PublishCommandBase
{
    public DeployCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry)
        : base("deploy", DeployCommandStrings.Description, runner, interactionService, projectLocator, telemetry)
    {
    }

    protected override string GetOutputPathDescription() => DeployCommandStrings.OutputPathArgumentDescription;

    protected override string GetDefaultOutputPath(ArgumentResult result) => Path.Combine(Environment.CurrentDirectory, "deploy");

    protected override string[] GetRunArguments(string fullyQualifiedOutputPath, string[] unmatchedTokens) =>
        ["--operation", "publish", "--publisher", "default", "--output-path", fullyQualifiedOutputPath, "--deploy", "true", ..unmatchedTokens];

    protected override string GetSuccessMessage(string fullyQualifiedOutputPath) => string.Format(CultureInfo.CurrentCulture, DeployCommandStrings.SuccessfullyDeployed, fullyQualifiedOutputPath);

    protected override string GetFailureMessage(int exitCode) => string.Format(CultureInfo.CurrentCulture, DeployCommandStrings.DeploymentFailed, exitCode);

    protected override string GetCanceledMessage() => DeployCommandStrings.DeploymentCanceled;

    protected override string GetProgressMessage() => PublishCommandStrings.GeneratingArtifacts;
}
