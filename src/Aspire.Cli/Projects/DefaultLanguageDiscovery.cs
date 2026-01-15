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
    private static readonly LanguageInfo[] s_allLanguages =
    [
        new LanguageInfo(
            LanguageId: new LanguageId(KnownLanguageId.CSharp),
            DisplayName: KnownLanguageId.CSharpDisplayName,
            PackageName: "", // C# doesn't need a code generation package
            DetectionPatterns: ["*.csproj"],
            AppHostFileName: null), // C# uses .csproj
        new LanguageInfo(
            LanguageId: new LanguageId("typescript/nodejs"),
            DisplayName: "TypeScript (Node.js)",
            PackageName: "Aspire.Hosting.CodeGeneration.TypeScript",
            DetectionPatterns: ["apphost.ts"],
            AppHostFileName: "apphost.ts"),
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
        return Task.FromResult<IEnumerable<LanguageInfo>>(s_allLanguages);
    }

    /// <inheritdoc />
    public Task<string?> GetPackageForLanguageAsync(LanguageId languageId, CancellationToken cancellationToken = default)
    {
        var language = s_allLanguages.FirstOrDefault(l =>
            string.Equals(l.LanguageId.Value, languageId.Value, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(language?.PackageName);
    }

    /// <inheritdoc />
    public Task<LanguageId?> DetectLanguageAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
    {
        foreach (var language in s_allLanguages)
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

    /// <inheritdoc />
    public LanguageInfo? GetLanguageById(LanguageId languageId)
    {
        return s_allLanguages.FirstOrDefault(l =>
            string.Equals(l.LanguageId.Value, languageId.Value, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public LanguageInfo? GetLanguageByFile(FileInfo file)
    {
        return s_allLanguages.FirstOrDefault(l =>
            l.DetectionPatterns.Any(p => MatchesPattern(file.Name, p)));
    }

    private static bool MatchesPattern(string fileName, string pattern)
    {
        // Handle wildcard patterns like "*.csproj"
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            var extension = pattern[1..]; // ".csproj"
            return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }
        
        // Exact match
        return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
