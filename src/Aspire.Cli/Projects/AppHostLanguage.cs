// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Represents the programming language for an AppHost.
/// </summary>
internal enum AppHostLanguage
{
    /// <summary>
    /// C# (.NET) AppHost.
    /// </summary>
    CSharp,

    /// <summary>
    /// TypeScript (Node.js) AppHost.
    /// </summary>
    TypeScript,

    /// <summary>
    /// Python AppHost.
    /// </summary>
    Python
}

/// <summary>
/// Extension methods for <see cref="AppHostLanguage"/>.
/// </summary>
internal static class AppHostLanguageExtensions
{
    /// <summary>
    /// Gets the display name for the language.
    /// </summary>
    public static string GetDisplayName(this AppHostLanguage language) => language switch
    {
        AppHostLanguage.CSharp => "C# (.NET)",
        AppHostLanguage.TypeScript => "TypeScript (Node.js)",
        AppHostLanguage.Python => "Python",
        _ => language.ToString()
    };

    /// <summary>
    /// Gets the file extension for the language's apphost file.
    /// </summary>
    public static string GetAppHostFileExtension(this AppHostLanguage language) => language switch
    {
        AppHostLanguage.CSharp => ".cs",
        AppHostLanguage.TypeScript => ".ts",
        AppHostLanguage.Python => ".py",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    /// <summary>
    /// Gets the apphost filename for the language.
    /// </summary>
    public static string GetAppHostFileName(this AppHostLanguage language) => language switch
    {
        AppHostLanguage.CSharp => "apphost.cs",
        AppHostLanguage.TypeScript => "apphost.ts",
        AppHostLanguage.Python => "apphost.py",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    /// <summary>
    /// Tries to parse a language string to an <see cref="AppHostLanguage"/>.
    /// </summary>
    public static bool TryParse(string? value, out AppHostLanguage language)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            language = default;
            return false;
        }

        // Normalize the input
        var normalized = value.Trim().ToLowerInvariant();

        language = normalized switch
        {
            "csharp" or "c#" or "cs" or "dotnet" or ".net" => AppHostLanguage.CSharp,
            "typescript" or "ts" or "node" or "nodejs" => AppHostLanguage.TypeScript,
            "python" or "py" => AppHostLanguage.Python,
            _ => default
        };

        return normalized switch
        {
            "csharp" or "c#" or "cs" or "dotnet" or ".net" => true,
            "typescript" or "ts" or "node" or "nodejs" => true,
            "python" or "py" => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the configuration value string for the language.
    /// </summary>
    public static string ToConfigValue(this AppHostLanguage language) => language switch
    {
        AppHostLanguage.CSharp => "csharp",
        AppHostLanguage.TypeScript => "typescript",
        AppHostLanguage.Python => "python",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };
}
