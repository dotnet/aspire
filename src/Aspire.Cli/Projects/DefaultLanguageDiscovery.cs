// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Default implementation of <see cref="ILanguageDiscovery"/>.
/// Maps language identifiers to their NuGet packages and detection patterns.
/// </summary>
/// <remarks>
/// This implementation provides a static list of supported languages.
/// Future implementations could discover languages from:
/// - Configuration files (~/.aspire/languages.json)
/// - NuGet search (Aspire.Hosting.Language.* packages)
/// - Remote service endpoints
/// </remarks>
internal sealed class DefaultLanguageDiscovery : ILanguageDiscovery
{
    private static readonly LanguageInfo[] s_languages =
    [
        new LanguageInfo(
            LanguageId: new LanguageId("typescript/nodejs"),
            DisplayName: "TypeScript (Node.js)",
            PackageName: "Aspire.Hosting.CodeGeneration.TypeScript",
            DetectionPatterns: ["apphost.ts"]),
        // Future: Add more runtimes
        // new LanguageInfo(
        //     LanguageId: new LanguageId("typescript/bun"),
        //     DisplayName: "TypeScript (Bun)",
        //     PackageName: "Aspire.Hosting.CodeGeneration.TypeScript.Bun",
        //     DetectionPatterns: ["apphost.ts", "bunfig.toml"]),
        // new LanguageInfo(
        //     LanguageId: new LanguageId("python"),
        //     DisplayName: "Python",
        //     PackageName: "Aspire.Hosting.CodeGeneration.Python",
        //     DetectionPatterns: ["apphost.py"]),
    ];

    /// <inheritdoc />
    public Task<IEnumerable<LanguageInfo>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<LanguageInfo>>(s_languages);
    }

    /// <inheritdoc />
    public Task<string?> GetPackageForLanguageAsync(LanguageId languageId, CancellationToken cancellationToken = default)
    {
        var language = s_languages.FirstOrDefault(l =>
            string.Equals(l.LanguageId.Value, languageId.Value, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(language?.PackageName);
    }

    /// <inheritdoc />
    public Task<LanguageId?> DetectLanguageAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
    {
        foreach (var language in s_languages)
        {
            foreach (var pattern in language.DetectionPatterns)
            {
                var filePath = Path.Combine(directory.FullName, pattern);
                if (File.Exists(filePath))
                {
                    return Task.FromResult<LanguageId?>(language.LanguageId);
                }
            }
        }

        return Task.FromResult<LanguageId?>(null);
    }
}
