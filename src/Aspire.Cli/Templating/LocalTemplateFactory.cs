// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Spectre.Console;

namespace Aspire.Cli.Templating;

internal sealed class LocalTemplateFactory(
    CliExecutionContext executionContext,
    IInteractionService interactionService,
    INewCommandPrompter prompter,
    ICliHostEnvironment hostEnvironment)
{
    public async Task<TemplateResult> CreateAsync(string templatePath, TemplateInputs inputs, ParseResult parseResult, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(templatePath, executionContext.WorkingDirectory.FullName);
            if (!Directory.Exists(fullPath))
            {
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, NewCommandStrings.TemplateDirectoryNotFound, fullPath));
                return new TemplateResult(CliExitCodes.InvalidCommand);
            }

            // Register concrete built-ins instead of loading engine extensions dynamically, which
            // Native AOT cannot support. A virtual hive keeps local templates out of dotnet new's state.
            // https://github.com/dotnet/templating/blob/main/src/Microsoft.TemplateEngine.Edge/DefaultTemplateEngineHost.cs
            var components = Microsoft.TemplateEngine.Edge.Components.AllComponents
                .Concat(Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Components.AllComponents).ToList();
            using var host = new DefaultTemplateEngineHost("aspire", "1.0.0", builtIns: components);
            using var settings = new EngineEnvironmentSettings(host, virtualizeSettings: true);
            using var manager = new TemplatePackageManager(settings);
            var installed = await manager.GetBuiltInManagedProvider().InstallAsync([new InstallRequest(fullPath)], cancellationToken);
            var failures = installed.Where(result => !result.Success).ToArray();
            if (failures.Length > 0)
            {
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, NewCommandStrings.LocalTemplateLoadFailed,
                    string.Join(", ", failures.Select(result => result.Error))));
                return new TemplateResult(CliExitCodes.FailedToInstallTemplates);
            }

            var creator = new TemplateCreator(settings);
            var templates = (await manager.GetTemplatesAsync(cancellationToken))
                .OrderBy(template => template.Name, StringComparer.OrdinalIgnoreCase)
                .Select(template => new CallbackTemplate(
                    GetTemplateName(template),
                    template.Name,
                    (context, name) => OutputPathHelper.GetUniqueDefaultOutputPath(name, context.WorkingDirectory.FullName),
                    _ => { },
                    (_, templateInputs, result, token) => ApplyAsync(creator, template, templateInputs, result, token)))
                .ToArray();
            if (templates.Length == 0)
            {
                interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, NewCommandStrings.NoLocalTemplatesFound, fullPath));
                return new TemplateResult(CliExitCodes.InvalidCommand);
            }

            if (templates.Length > 1 && !hostEnvironment.SupportsInteractiveInput)
            {
                interactionService.DisplayError(NewCommandStrings.LocalTemplateSelectionRequiresInteraction);
                return new TemplateResult(CliExitCodes.InvalidCommand);
            }

            var selectedTemplate = templates.Length == 1
                ? templates[0]
                : await prompter.PromptForTemplateAsync(templates, cancellationToken);
            return await selectedTemplate.ApplyTemplateAsync(inputs, parseResult, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, NewCommandStrings.LocalTemplateLoadFailed, ex.Message));
            return new TemplateResult(CliExitCodes.FailedToCreateNewProject);
        }
    }

    private async Task<TemplateResult> ApplyAsync(TemplateCreator creator, ITemplateInfo template, TemplateInputs inputs, ParseResult parseResult, CancellationToken cancellationToken)
    {
        var name = inputs.Name is { } explicitName && ProjectNameValidator.IsProjectNameValid(explicitName)
            ? explicitName
            : await prompter.PromptForProjectNameAsync(
                string.IsNullOrWhiteSpace(template.DefaultName) ? GetTemplateName(template) : template.DefaultName, parseResult, cancellationToken);
        var isExtensionHost = ExtensionHelper.IsExtensionHost(interactionService, out _, out _);
        var createSubdirectory = await OutputPathHelper.PromptExtensionCreateProjectNameSubdirectoryAsync(
            interactionService, isExtensionHost, inputs.Output is not null, name, cancellationToken);
        var outputPath = await OutputPathHelper.ResolveOutputPathAsync(
            inputs.Output,
            executionContext.WorkingDirectory.FullName,
            () => prompter.PromptForOutputPath(
                OutputPathHelper.GetUniqueDefaultOutputPath(name, executionContext.WorkingDirectory.FullName),
                parseResult,
                OutputPathHelper.CreateOutputPathValidator(executionContext.WorkingDirectory.FullName),
                OutputPathHelper.CreateProjectNameSubdirectoryOutputPathResolver(createSubdirectory, name),
                cancellationToken),
            interactionService);
        if (outputPath is null)
        {
            return new TemplateResult(CliExitCodes.FailedToCreateNewProject);
        }

        var parameters = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var parameter in template.ParameterDefinitions)
        {
            if (parameter.IsName || parameter.Type != "parameter" ||
                parameter.Precedence.PrecedenceDefinition is PrecedenceDefinition.Implicit or PrecedenceDefinition.Disabled)
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (parameter.Name.Equals("AspireVersion", StringComparison.OrdinalIgnoreCase))
            {
                parameters[parameter.Name] = executionContext.IdentitySdkVersion;
                continue;
            }

            if (!hostEnvironment.SupportsInteractiveInput)
            {
                // Leave unset values to the engine, including conditional requirements and defaults.
                continue;
            }

            parameters[parameter.Name] = await PromptForParameterAsync(parameter, cancellationToken);
        }

        // Validate through the engine before it creates the output directory. TemplateCreator
        // returns post-actions as metadata; this flow deliberately does not execute them.
        var preview = await creator.InstantiateAsync(template, name, null, outputPath, parameters, dryRun: true, cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (preview.Status != CreationResultStatus.Success)
        {
            return CreationFailed(preview);
        }

        var result = await interactionService.ShowStatusAsync(
            TemplatingStrings.CreatingNewProject,
            () => creator.InstantiateAsync(template, name, null, outputPath, parameters, cancellationToken: cancellationToken),
            emoji: KnownEmojis.Rocket);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.Status != CreationResultStatus.Success)
        {
            return CreationFailed(result);
        }

        interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, TemplatingStrings.ProjectCreatedSuccessfully, outputPath));
        return new TemplateResult(CliExitCodes.Success, outputPath);
    }

    private async Task<string> PromptForParameterAsync(ITemplateParameter parameter, CancellationToken cancellationToken)
    {
        var prompt = (!string.IsNullOrWhiteSpace(parameter.DisplayName)
            ? parameter.DisplayName
            : !string.IsNullOrWhiteSpace(parameter.Description) ? parameter.Description : parameter.Name).EscapeMarkup();
        if (parameter.DataType.Equals("bool", StringComparison.OrdinalIgnoreCase))
        {
            var value = await interactionService.PromptConfirmAsync(
                prompt,
                binding: PromptBinding.CreateDefault(bool.TryParse(parameter.DefaultValue, out var defaultValue) && defaultValue),
                cancellationToken: cancellationToken);
            return value ? "true" : "false";
        }

        if (parameter.Choices is { Count: > 0 } choices)
        {
            // The engine represents multi-choice values as "red|blue", not comma-separated input.
            var defaults = parameter.DefaultValue?.Split('|') ?? [];
            var keys = choices.Keys.OrderByDescending(key => defaults.Contains(key, StringComparer.OrdinalIgnoreCase)).ToArray();
            string FormatChoice(string key) => (string.IsNullOrWhiteSpace(choices[key].DisplayName) ? key : choices[key].DisplayName).EscapeMarkup();
            if (parameter.AllowMultipleValues)
            {
                var selected = await interactionService.PromptForSelectionsAsync(
                    prompt, keys, FormatChoice,
                    preSelected: keys.Where(key => defaults.Contains(key, StringComparer.OrdinalIgnoreCase)),
                    optional: !parameter.Precedence.IsRequired,
                    cancellationToken: cancellationToken);
                return string.Join('|', selected);
            }

            return await interactionService.PromptForSelectionAsync(prompt, keys, FormatChoice, cancellationToken: cancellationToken);
        }

        return await interactionService.PromptForStringAsync(
            prompt,
            required: parameter.Precedence.IsRequired,
            binding: PromptBinding.CreateDefault<string?>(parameter.DefaultValue ?? string.Empty),
            cancellationToken: cancellationToken);
    }

    private TemplateResult CreationFailed(ITemplateCreationResult result)
    {
        interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, NewCommandStrings.LocalTemplateCreationFailed, result.Status, result.ErrorMessage));
        return new TemplateResult(CliExitCodes.FailedToCreateNewProject);
    }

    private static string GetTemplateName(ITemplateInfo template) =>
        template.ShortNameList.Count > 0 ? template.ShortNameList[0] : template.Identity;
}
