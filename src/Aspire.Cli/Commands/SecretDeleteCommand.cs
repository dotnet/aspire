// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Secrets;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Deletes a secret from an AppHost project.
/// </summary>
internal sealed class SecretDeleteCommand : BaseCommand
{
    private static readonly Argument<string> s_keyArgument = new("key")
    {
        Description = "The secret key to delete."
    };

    private readonly IInteractionService _interactionService;
    private readonly SecretStoreResolver _secretStoreResolver;

    public SecretDeleteCommand(
        IInteractionService interactionService,
        SecretStoreResolver secretStoreResolver,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("delete", "Delete a secret.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _secretStoreResolver = secretStoreResolver;

        Arguments.Add(s_keyArgument);
        Options.Add(SecretCommand.s_projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var key = parseResult.GetValue(s_keyArgument)!;
        var projectFile = parseResult.GetValue(SecretCommand.s_projectOption);

        var result = await _secretStoreResolver.ResolveAsync(projectFile, autoInit: false, cancellationToken);
        if (result is null)
        {
            _interactionService.DisplayError("Could not find an AppHost project.");
            return ExitCodeConstants.FailedToFindProject;
        }

        if (!result.Store.Remove(key))
        {
            _interactionService.DisplayError($"Secret '{key.EscapeMarkup()}' not found.");
            return ExitCodeConstants.ConfigNotFound;
        }

        result.Store.Save();
        _interactionService.DisplaySuccess($"Secret '{key.EscapeMarkup()}' deleted successfully.");
        return ExitCodeConstants.Success;
    }
}
