// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Service for managing AppHost language selection and persistence.
/// </summary>
internal interface ILanguageService
{
    /// <summary>
    /// Gets the configured language from settings, or null if not set.
    /// </summary>
    Task<AppHostLanguage?> GetConfiguredLanguageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the language in the configuration.
    /// </summary>
    /// <param name="language">The language to save.</param>
    /// <param name="isGlobal">Whether to save to global settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetLanguageAsync(AppHostLanguage language, bool isGlobal = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts the user to select a language interactively.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected language.</returns>
    Task<AppHostLanguage> PromptForLanguageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the language to use, prompting if not already configured.
    /// </summary>
    /// <param name="explicitLanguage">An explicitly specified language (e.g., from command line).</param>
    /// <param name="saveSelection">Whether to save the selection to config if prompted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The language to use.</returns>
    Task<AppHostLanguage> GetOrPromptForLanguageAsync(string? explicitLanguage = null, bool saveSelection = true, CancellationToken cancellationToken = default);
}
