// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;

namespace Aspire.Cli.Projects;

/// <summary>
/// Service for managing AppHost language selection and persistence.
/// </summary>
internal sealed class LanguageService : ILanguageService
{
    private const string LanguageConfigKey = "language";

    private readonly IConfigurationService _configurationService;
    private readonly IInteractionService _interactionService;

    public LanguageService(
        IConfigurationService configurationService,
        IInteractionService interactionService)
    {
        _configurationService = configurationService;
        _interactionService = interactionService;
    }

    /// <inheritdoc />
    public async Task<AppHostLanguage?> GetConfiguredLanguageAsync(CancellationToken cancellationToken = default)
    {
        var languageValue = await _configurationService.GetConfigurationAsync(LanguageConfigKey, cancellationToken);

        if (AppHostLanguageExtensions.TryParse(languageValue, out var language))
        {
            return language;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task SetLanguageAsync(AppHostLanguage language, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        await _configurationService.SetConfigurationAsync(
            LanguageConfigKey,
            language.ToConfigValue(),
            isGlobal,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AppHostLanguage> PromptForLanguageAsync(CancellationToken cancellationToken = default)
    {
        var languages = new Dictionary<AppHostLanguage, string>
        {
            { AppHostLanguage.CSharp, AppHostLanguage.CSharp.GetDisplayName() },
            { AppHostLanguage.TypeScript, AppHostLanguage.TypeScript.GetDisplayName() },
            { AppHostLanguage.Python, AppHostLanguage.Python.GetDisplayName() }
        };

        _interactionService.DisplayEmptyLine();
        _interactionService.DisplayMarkdown("""
            # Select AppHost Language

            Choose the programming language for your Aspire AppHost.
            This selection will be saved for future use.
            """);
        _interactionService.DisplayEmptyLine();

        var selected = await _interactionService.PromptForSelectionAsync(
            "Which language would you like to use?",
            languages,
            kvp => kvp.Value,
            cancellationToken);

        return selected.Key;
    }

    /// <inheritdoc />
    public async Task<AppHostLanguage> GetOrPromptForLanguageAsync(
        string? explicitLanguage = null,
        bool saveSelection = true,
        CancellationToken cancellationToken = default)
    {
        // If explicitly specified, use that
        if (!string.IsNullOrWhiteSpace(explicitLanguage))
        {
            if (AppHostLanguageExtensions.TryParse(explicitLanguage, out var language))
            {
                return language;
            }

            _interactionService.DisplayError($"Unknown language: '{explicitLanguage}'. Valid options are: csharp, typescript, python");
            throw new ArgumentException($"Unknown language: '{explicitLanguage}'", nameof(explicitLanguage));
        }

        // Check if configured
        var configuredLanguage = await GetConfiguredLanguageAsync(cancellationToken);
        if (configuredLanguage.HasValue)
        {
            return configuredLanguage.Value;
        }

        // Prompt for selection
        var selectedLanguage = await PromptForLanguageAsync(cancellationToken);

        // Save the selection
        if (saveSelection)
        {
            await SetLanguageAsync(selectedLanguage, isGlobal: false, cancellationToken);
            _interactionService.DisplayMessage("check_mark", $"Language preference saved to local settings: {selectedLanguage.GetDisplayName()}");
        }

        return selectedLanguage;
    }
}
