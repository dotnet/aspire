// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating.Git;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateNewIndexCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string?> s_pathArgument = new("path")
    {
        Description = "Directory to create aspire-template-index.json in (defaults to current directory)",
        Arity = ArgumentArity.ZeroOrOne
    };

    public TemplateNewIndexCommand(
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("new-index", "Scaffold a new aspire-template-index.json index file", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Arguments.Add(s_pathArgument);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var targetDir = parseResult.GetValue(s_pathArgument) ?? Directory.GetCurrentDirectory();
        targetDir = Path.GetFullPath(targetDir);

        var outputPath = Path.Combine(targetDir, "aspire-template-index.json");

        if (File.Exists(outputPath))
        {
            var overwrite = await InteractionService.ConfirmAsync(
                $"aspire-template-index.json already exists in {targetDir}. Overwrite?",
                defaultValue: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!overwrite)
            {
                InteractionService.DisplayMessage("information", "Cancelled.");
                return ExitCodeConstants.Success;
            }
        }

        var index = new GitTemplateIndex
        {
            Schema = "https://aka.ms/aspire/template-index-schema/v1",
            Templates =
            [
                new GitTemplateIndexEntry
                {
                    Name = "my-template",
                    DisplayName = "My Template",
                    Description = "A template created with aspire template new-index.",
                    Path = "templates/my-template"
                }
            ]
        };

        Directory.CreateDirectory(targetDir);

        var json = JsonSerializer.Serialize(index, GitTemplateJsonContext.RelaxedEscaping.GitTemplateIndex);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken).ConfigureAwait(false);

        InteractionService.DisplaySuccess($"Created {outputPath}");
        InteractionService.DisplayMessage("information", "Edit the file to add your templates, then run 'aspire template new' in each template directory.");
        return ExitCodeConstants.Success;
    }
}
