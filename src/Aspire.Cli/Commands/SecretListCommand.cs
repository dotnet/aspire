// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json.Nodes;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Secrets;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Shared.UserSecrets;

namespace Aspire.Cli.Commands;

/// <summary>
/// Lists all secrets for an AppHost project.
/// </summary>
internal sealed class SecretListCommand : BaseCommand
{
    private static readonly Option<OutputFormat?> s_formatOption = new("--format")
    {
        Description = "Output format."
    };

    private readonly IInteractionService _interactionService;
    private readonly SecretStoreResolver _secretStoreResolver;

    public SecretListCommand(
        IInteractionService interactionService,
        SecretStoreResolver secretStoreResolver,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry)
        : base("list", "List all secrets.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _secretStoreResolver = secretStoreResolver;

        Options.Add(SecretCommand.s_projectOption);
        Options.Add(s_formatOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var projectFile = parseResult.GetValue(SecretCommand.s_projectOption);
        var format = parseResult.GetValue(s_formatOption);

        var result = await _secretStoreResolver.ResolveAsync(projectFile, autoInit: false, cancellationToken);
        if (result is null)
        {
            _interactionService.DisplayError("Could not find an AppHost project.");
            return ExitCodeConstants.FailedToFindProject;
        }

        var secrets = result.Store.ToList();

        if (format == OutputFormat.Json)
        {
            var obj = new JsonObject();
            foreach (var (key, value) in secrets.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
            {
                obj[key] = value;
            }

            var json = obj.ToJsonString(SecretsStore.s_jsonOptions);
            // Structured output always goes to stdout.
            _interactionService.DisplayRawText(json, ConsoleOutput.Standard);
        }
        else
        {
            if (secrets.Count == 0)
            {
                _interactionService.DisplayMessage("information", "No secrets configured.");
            }
            else
            {
                foreach (var (key, value) in secrets.OrderBy(s => s.Key, StringComparer.OrdinalIgnoreCase))
                {
                    _interactionService.DisplayPlainText($"{key} = {value}");
                }
            }
        }

        return ExitCodeConstants.Success;
    }
}
