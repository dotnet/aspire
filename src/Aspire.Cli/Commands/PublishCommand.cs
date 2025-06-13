// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine.Parsing;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;

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
            "Select a publisher:",
            publishers,
            p => p,
            cancellationToken
        );
    }
}

internal sealed class PublishCommand : PublishCommandBase
{
    private readonly IPublishCommandPrompter _prompter;

    public PublishCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, IPublishCommandPrompter prompter, AspireCliActivityTelemetry telemetry)
        : base("publish", "Generates deployment artifacts for an Aspire app host project.", runner, interactionService, projectLocator, telemetry)
    {
        ArgumentNullException.ThrowIfNull(prompter);
        _prompter = prompter;
    }

    protected override string GetOutputPathDescription() => "The output path for the generated artifacts.";

    protected override string GetDefaultOutputPath(ArgumentResult result) => Path.Combine(Environment.CurrentDirectory);

    protected override string[] GetRunArguments(string fullyQualifiedOutputPath, string[] unmatchedTokens) =>
        ["--operation", "publish", "--publisher", "default", "--output-path", fullyQualifiedOutputPath, ..unmatchedTokens];

    protected override string GetSuccessMessage(string fullyQualifiedOutputPath) => $"Successfully published artifacts to: {fullyQualifiedOutputPath}";

    protected override string GetFailureMessage(int exitCode) => $"Publishing artifacts failed with exit code {exitCode}. For more information run with --debug switch.";

    protected override string GetCanceledMessage() => "The operation was canceled.";

    protected override string GetProgressMessage() => "Generating artifacts...";
}
