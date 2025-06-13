// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;

namespace Aspire.Cli.Commands;

internal sealed class DeployCommand : PublishCommandBase
{
    public DeployCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator)
        : base("deploy", "Deploy an Aspire app host project to its supported deployment targets.", runner, interactionService, projectLocator)
    {
    }

    protected override string GetOutputPathDescription() => "The output path for deployment artifacts.";

    protected override string GetDefaultOutputPath(ArgumentResult result) => Path.Combine(Environment.CurrentDirectory, "deploy");

    protected override string[] GetRunArguments(string fullyQualifiedOutputPath, string[] unmatchedTokens) =>
        ["--operation", "publish", "--publisher", "default", "--output-path", fullyQualifiedOutputPath, "--deploy", "true", ..unmatchedTokens];

    protected override string GetSuccessMessage(string fullyQualifiedOutputPath) => $"Successfully deployed. Artifacts available at: {fullyQualifiedOutputPath}";

    protected override string GetFailureMessage(int exitCode) => $"Deployment failed with exit code {exitCode}. For more information run with --debug switch.";

    protected override string GetCanceledMessage() => "The deployment was canceled.";

    protected override string GetProgressMessage() => "Generating artifacts...";
}
