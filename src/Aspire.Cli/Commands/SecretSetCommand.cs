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
/// Sets a secret value for an AppHost project.
/// </summary>
internal sealed class SecretSetCommand : BaseCommand
{
    private static readonly Argument<string> s_keyArgument = new("key")
    {
        Description = "The secret key (e.g., Azure:Location or Parameters:postgres-password)."
    };

    private static readonly Argument<string> s_valueArgument = new("value")
    {
        Description = "The secret value to set."
    };

    private readonly IInteractionService _interactionService;
    private readonly SecretStoreResolver _secretStoreResolver;

    public SecretSetCommand(
        IInteractionService interactionService,
        SecretStoreResolver secretStoreResolver,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("set", "Set a secret value.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _secretStoreResolver = secretStoreResolver;

        Arguments.Add(s_keyArgument);
        Arguments.Add(s_valueArgument);
        Options.Add(SecretCommand.s_projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var key = parseResult.GetValue(s_keyArgument)!;
        var value = parseResult.GetValue(s_valueArgument)!;
        var projectFile = parseResult.GetValue(SecretCommand.s_projectOption);

        var result = await _secretStoreResolver.ResolveAsync(projectFile, autoInit: true, cancellationToken);
        if (result is null)
        {
            _interactionService.DisplayError("Could not find an AppHost project.");
            return ExitCodeConstants.FailedToFindProject;
        }

        result.Store.Set(key, value);
        result.Store.Save();

        _interactionService.DisplaySuccess($"Secret '{key.EscapeMarkup()}' set successfully.");
        return ExitCodeConstants.Success;
    }
}
