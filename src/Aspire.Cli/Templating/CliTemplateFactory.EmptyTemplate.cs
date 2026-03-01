// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Scaffolding;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Templating;

internal sealed partial class CliTemplateFactory
{
    private async Task<TemplateResult> ApplyEmptyAppHostTemplateAsync(CallbackTemplate _, TemplateInputs inputs, System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken)
    {
        var languageId = inputs.Language;
        if (string.IsNullOrWhiteSpace(languageId))
        {
            _interactionService.DisplayError("Language selection is required.");
            return new TemplateResult(ExitCodeConstants.InvalidCommand);
        }

        var language = _languageDiscovery.GetLanguageById(languageId);
        if (language is null)
        {
            _interactionService.DisplayError($"Unknown language: '{languageId}'");
            return new TemplateResult(ExitCodeConstants.InvalidCommand);
        }

        var projectName = inputs.Name;
        if (string.IsNullOrWhiteSpace(projectName))
        {
            var defaultName = _executionContext.WorkingDirectory.Name;
            projectName = await _prompter.PromptForProjectNameAsync(defaultName, cancellationToken);
        }

        var outputPath = inputs.Output;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var defaultOutputPath = $"./{projectName}";
            outputPath = await _prompter.PromptForOutputPath(defaultOutputPath, cancellationToken);
        }
        if (!Path.IsPathRooted(outputPath))
        {
            outputPath = Path.Combine(_executionContext.WorkingDirectory.FullName, outputPath);
        }

        _logger.LogDebug("Applying empty AppHost template. LanguageId: {LanguageId}, Language: {LanguageDisplayName}, ProjectName: {ProjectName}, OutputPath: {OutputPath}.", languageId, language.DisplayName, projectName, outputPath);

        var useLocalhostTld = await ResolveUseLocalhostTldAsync(parseResult, cancellationToken);

        try
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            if (language.LanguageId.Value.Equals(Projects.KnownLanguageId.CSharp, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Using embedded C# empty AppHost template for '{OutputPath}'.", outputPath);
                await WriteCSharpEmptyAppHostAsync(inputs.Version, outputPath, projectName, useLocalhostTld, cancellationToken);
                await _templateNuGetConfigService.PromptToCreateOrUpdateNuGetConfigAsync(inputs.Channel, outputPath, cancellationToken);
            }
            else
            {
                _logger.LogDebug("Using scaffolding service for language '{LanguageDisplayName}' in '{OutputPath}'.", language.DisplayName, outputPath);
                var context = new ScaffoldContext(language, new DirectoryInfo(outputPath), projectName);
                await _scaffoldingService.ScaffoldAsync(context, cancellationToken);

                if (useLocalhostTld)
                {
                    await ApplyLocalhostTldToScaffoldedRunProfileAsync(outputPath, projectName, cancellationToken);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _interactionService.DisplayError($"Failed to create project files: {ex.Message}");
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
        }

        _interactionService.DisplaySuccess($"Created {language.DisplayName.EscapeMarkup()} project at {outputPath.EscapeMarkup()}");
        _interactionService.DisplayMessage(KnownEmojis.Information, "Run 'aspire run' to start your AppHost.");

        return new TemplateResult(ExitCodeConstants.Success, outputPath);
    }

    private async Task<bool> ResolveUseLocalhostTldAsync(System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken)
    {
        var localhostTldOptionSpecified = parseResult.Tokens.Any(token =>
            string.Equals(token.Value, "--localhost-tld", StringComparisons.CliInputOrOutput));
        var useLocalhostTld = parseResult.GetValue(s_localhostTldOption);
        if (!localhostTldOptionSpecified)
        {
            if (!_hostEnvironment.SupportsInteractiveInput)
            {
                return false;
            }

            useLocalhostTld = await _interactionService.PromptForSelectionAsync(
                TemplatingStrings.UseLocalhostTld_Prompt,
                [TemplatingStrings.No, TemplatingStrings.Yes],
                choice => choice,
                cancellationToken) switch
            {
                var choice when string.Equals(choice, TemplatingStrings.Yes, StringComparisons.CliInputOrOutput) => true,
                var choice when string.Equals(choice, TemplatingStrings.No, StringComparisons.CliInputOrOutput) => false,
                _ => throw new InvalidOperationException(TemplatingStrings.UseLocalhostTld_UnexpectedChoice)
            };
        }

        if (useLocalhostTld ?? false)
        {
            _interactionService.DisplayMessage(KnownEmojis.CheckMark, TemplatingStrings.UseLocalhostTld_UsingLocalhostTld);
        }

        return useLocalhostTld ?? false;
    }

    private async Task ApplyLocalhostTldToScaffoldedRunProfileAsync(string outputPath, string projectName, CancellationToken cancellationToken)
    {
        var appHostRunProfilePath = Path.Combine(outputPath, "apphost.run.json");
        if (!File.Exists(appHostRunProfilePath))
        {
            _logger.LogDebug("Skipping localhost TLD update because '{RunProfilePath}' was not found.", appHostRunProfilePath);
            return;
        }

        var hostName = $"{projectName.ToLowerInvariant()}.dev.localhost";
        var content = await File.ReadAllTextAsync(appHostRunProfilePath, cancellationToken);
        var updatedContent = content.Replace("://localhost", $"://{hostName}", StringComparison.Ordinal);

        if (!string.Equals(content, updatedContent, StringComparison.Ordinal))
        {
            await File.WriteAllTextAsync(appHostRunProfilePath, updatedContent, cancellationToken);
        }
    }

    private async Task WriteCSharpEmptyAppHostAsync(string? templateVersion, string outputPath, string projectName, bool useLocalhostTld, CancellationToken cancellationToken)
    {
        var aspireVersion = string.IsNullOrWhiteSpace(templateVersion)
            ? VersionHelper.GetDefaultTemplateVersion()
            : templateVersion;
        var projectNameLower = projectName.ToLowerInvariant();
        var ports = GenerateRandomPorts();
        var hostName = useLocalhostTld ? $"{projectNameLower}.dev.localhost" : "localhost";
        string ApplyAllTokens(string content) => ApplyTokens(content, projectName, projectNameLower, aspireVersion, ports, hostName);

        _logger.LogDebug("Writing C# empty AppHost template files to '{OutputPath}' with Aspire version '{AspireVersion}'.", outputPath, aspireVersion);
        await CopyTemplateTreeToDiskAsync("empty-apphost", outputPath, ApplyAllTokens, cancellationToken);
    }
}
