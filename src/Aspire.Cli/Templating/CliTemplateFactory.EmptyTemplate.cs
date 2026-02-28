// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // Resolve localhost TLD from --localhost-tld option (defaults to false)
        var useLocalhostTld = ResolveLocalhostTld(parseResult);

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
            }
            else
            {
                _logger.LogDebug("Using scaffolding service for language '{LanguageDisplayName}' in '{OutputPath}'.", language.DisplayName, outputPath);
                var context = new ScaffoldContext(language, new DirectoryInfo(outputPath), projectName);
                await _scaffoldingService.ScaffoldAsync(context, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _interactionService.DisplayError($"Failed to create project files: {ex.Message}");
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
        }

        _interactionService.DisplaySuccess($"Created {language.DisplayName.EscapeMarkup()} project at {outputPath.EscapeMarkup()}");
        _interactionService.DisplayMessage("information", "Run 'aspire run' to start your AppHost.");

        return new TemplateResult(ExitCodeConstants.Success, outputPath);
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
