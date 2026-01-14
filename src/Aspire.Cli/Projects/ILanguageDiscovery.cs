// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Information about a supported language.
/// </summary>
/// <param name="LanguageId">The language identifier (e.g., "typescript").</param>
/// <param name="DisplayName">The display name for the language (e.g., "TypeScript (Node.js)").</param>
/// <param name="PackageName">The NuGet package name for language support (e.g., "Aspire.Hosting.CodeGeneration.TypeScript").</param>
/// <param name="DetectionPatterns">File patterns used to detect this language (e.g., ["apphost.ts"]).</param>
internal sealed record LanguageInfo(
    string LanguageId,
    string DisplayName,
    string PackageName,
    string[] DetectionPatterns);

/// <summary>
/// Interface for discovering available languages.
/// Implementations provide language metadata and detection capabilities.
/// </summary>
/// <remarks>
/// This interface is designed to be async to support future implementations
/// that may discover languages from external sources (NuGet, config files, etc.).
/// </remarks>
internal interface ILanguageDiscovery
{
    /// <summary>
    /// Gets all available languages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All available language information.</returns>
    Task<IEnumerable<LanguageInfo>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the NuGet package name for a language.
    /// </summary>
    /// <param name="languageId">The language identifier (e.g., "typescript").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The package name, or null if the language is not found.</returns>
    Task<string?> GetPackageForLanguageAsync(string languageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the language used in a directory by checking for known file patterns.
    /// This is a fallback detection mechanism when .aspire/settings.json doesn't exist.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The detected language ID, or null if no language was detected.</returns>
    Task<string?> DetectLanguageAsync(DirectoryInfo directory, CancellationToken cancellationToken = default);
}
