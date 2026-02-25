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
/// Gets a secret value from an AppHost project.
/// </summary>
internal sealed class SecretGetCommand : BaseCommand
{
    private static readonly Argument<string> s_keyArgument = new("key")
    {
        Description = "The secret key to retrieve."
    };

    private readonly IInteractionService _interactionService;
    private readonly SecretStoreResolver _secretStoreResolver;

    public SecretGetCommand(
        IInteractionService interactionService,
        SecretStoreResolver secretStoreResolver,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("get", "Get a secret value.", features, updateNotifier, executionContext, interactionService, telemetry)
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

        var value = result.Store.Get(key);
        if (value is null)
        {
            _interactionService.DisplayError($"Secret '{key.EscapeMarkup()}' not found.");
            return ExitCodeConstants.ConfigNotFound;
        }

        // Write value to stdout (machine-readable)
        _interactionService.DisplayPlainText(value);
        return ExitCodeConstants.Success;
    }
}
