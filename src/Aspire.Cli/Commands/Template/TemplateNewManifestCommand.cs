// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating.Git;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands.Template;

internal sealed partial class TemplateNewManifestCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string?> s_pathArgument = new("path")
    {
        Description = "Directory to create aspire-template.json in (defaults to current directory)",
        Arity = ArgumentArity.ZeroOrOne
    };

    public TemplateNewManifestCommand(
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("new", "Scaffold a new aspire-template.json manifest", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Arguments.Add(s_pathArgument);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var targetDir = parseResult.GetValue(s_pathArgument) ?? Directory.GetCurrentDirectory();
        targetDir = Path.GetFullPath(targetDir);

        var outputPath = Path.Combine(targetDir, "aspire-template.json");

        if (File.Exists(outputPath))
        {
            var overwrite = await InteractionService.ConfirmAsync(
                $"aspire-template.json already exists in {targetDir}. Overwrite?",
                defaultValue: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!overwrite)
            {
                InteractionService.DisplayMessage("information", "Cancelled.");
                return ExitCodeConstants.Success;
            }
        }

        var name = await InteractionService.PromptForStringAsync(
            "Template name (kebab-case identifier)",
            defaultValue: Path.GetFileName(targetDir)?.ToLowerInvariant(),
            validator: value => KebabCasePattern().IsMatch(value)
                ? ValidationResult.Success()
                : ValidationResult.Error("Must be lowercase kebab-case (e.g. my-template)"),
            required: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var canonicalName = ToPascalCase(name);

        var manifest = new GitTemplateManifest
        {
            Schema = "https://aka.ms/aspire/template-schema/v1",
            Name = name,
            Variables = new Dictionary<string, GitTemplateVariable>
            {
                ["projectName"] = new()
                {
                    Type = "string",
                    Required = true,
                    DefaultValue = canonicalName,
                    Validation = new GitTemplateVariableValidation
                    {
                        Pattern = "^[A-Za-z][A-Za-z0-9_.]*$",
                    }
                }
            },
            Substitutions = new GitTemplateSubstitutions
            {
                Filenames = new Dictionary<string, string>
                {
                    [canonicalName] = "{{projectName}}"
                },
                Content = new Dictionary<string, string>
                {
                    [canonicalName] = "{{projectName}}",
                    [canonicalName.ToLowerInvariant()] = "{{projectName | lowercase}}"
                }
            }
        };

        Directory.CreateDirectory(targetDir);

        var json = JsonSerializer.Serialize(manifest, GitTemplateJsonContext.RelaxedEscaping.GitTemplateManifest);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken).ConfigureAwait(false);

        InteractionService.DisplaySuccess($"Created {outputPath}");
        return ExitCodeConstants.Success;
    }

    private static string ToPascalCase(string kebab)
    {
        return string.Concat(kebab.Split('-').Select(part =>
            part.Length == 0 ? "" : char.ToUpperInvariant(part[0]) + part[1..]));
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$")]
    private static partial Regex KebabCasePattern();
}
