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

internal interface IPublishCommandPrompter
{
    Task<string> PromptForPublisherAsync(IEnumerable<string> publishers, CancellationToken cancellationToken);
}

internal class PublishCommandPrompter(IInteractionService interactionService) : IPublishCommandPrompter
{
    public virtual async Task<string> PromptForPublisherAsync(IEnumerable<string> publishers, CancellationToken cancellationToken)
    {
        return await interactionService.PromptForSelectionAsync(
            PublishCommandStrings.SelectAPublisher,
            publishers,
            p => p,
            cancellationToken
        );
    }
}

internal sealed class PublishCommand : PublishCommandBase
{
    private readonly IPublishCommandPrompter _prompter;

    public PublishCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, IPublishCommandPrompter prompter, AspireCliTelemetry telemetry, IDotNetSdkInstaller sdkInstaller, IFeatures features, ICliUpdateNotifier updateNotifier, CliExecutionContext executionContext, ICIEnvironmentDetector ciDetector)
        : base("publish", PublishCommandStrings.Description, runner, interactionService, projectLocator, telemetry, sdkInstaller, features, updateNotifier, executionContext, ciDetector)
    {
        ArgumentNullException.ThrowIfNull(prompter);
        _prompter = prompter;
    }

    protected override string OperationCompletedPrefix => PublishCommandStrings.OperationCompletedPrefix;
    protected override string OperationFailedPrefix => PublishCommandStrings.OperationFailedPrefix;
    protected override string GetOutputPathDescription() => PublishCommandStrings.OutputPathArgumentDescription;

    protected override string[] GetRunArguments(string? fullyQualifiedOutputPath, string[] unmatchedTokens, ParseResult parseResult)
    {
        var baseArgs = new List<string> { "--operation", "publish", "--publisher", "default" };

        var targetPath = fullyQualifiedOutputPath is not null
            ? fullyQualifiedOutputPath
            : Path.Combine(Environment.CurrentDirectory, "aspire-output");

        baseArgs.AddRange(["--output-path", targetPath]);

        baseArgs.AddRange(unmatchedTokens);

        return [.. baseArgs];
    }

    protected override string GetCanceledMessage() => InteractionServiceStrings.OperationCancelled;

    protected override string GetProgressMessage() => PublishCommandStrings.GeneratingArtifacts;
}
