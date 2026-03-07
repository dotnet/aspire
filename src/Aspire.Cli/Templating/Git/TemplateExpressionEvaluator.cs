// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Evaluates template expressions like <c>{{variableName}}</c> and <c>{{variableName | filter}}</c>.
/// </summary>
internal static partial class TemplateExpressionEvaluator
{
    /// <summary>
    /// Replaces all <c>{{...}}</c> expressions in the input string using the provided variable values.
    /// </summary>
    public static string Evaluate(string input, IReadOnlyDictionary<string, string> variables)
    {
        return ExpressionPattern().Replace(input, match =>
        {
            var expression = match.Groups[1].Value.Trim();
            var parts = expression.Split('|', 2, StringSplitOptions.TrimEntries);
            var variableName = parts[0];
            var filter = parts.Length > 1 ? parts[1] : null;

            if (!variables.TryGetValue(variableName, out var value))
            {
                // Leave unresolved expressions as-is.
                return match.Value;
            }

            return filter is not null ? ApplyFilter(value, filter) : value;
        });
    }

    private static string ApplyFilter(string value, string filter) =>
        filter.ToLowerInvariant() switch
        {
            "lowercase" => value.ToLowerInvariant(),
            "uppercase" => value.ToUpperInvariant(),
            "kebabcase" => ToKebabCase(value),
            "snakecase" => ToSnakeCase(value),
            "camelcase" => ToCamelCase(value),
            "pascalcase" => ToPascalCase(value),
            _ => value
        };

    private static string ToKebabCase(string value)
    {
        return string.Concat(SplitWords(value).Select((w, i) =>
            (i > 0 ? "-" : "") + w.ToLowerInvariant()));
    }

    private static string ToSnakeCase(string value)
    {
        return string.Concat(SplitWords(value).Select((w, i) =>
            (i > 0 ? "_" : "") + w.ToLowerInvariant()));
    }

    private static string ToCamelCase(string value)
    {
        return string.Concat(SplitWords(value).Select((w, i) =>
            i == 0 ? w.ToLowerInvariant() : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static string ToPascalCase(string value)
    {
        return string.Concat(SplitWords(value).Select(w =>
            char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static IEnumerable<string> SplitWords(string value)
    {
        // Split on transitions: lowercase→uppercase, non-letter→letter, or explicit separators
        return WordSplitPattern().Matches(value)
            .Select(m => m.Value)
            .Where(w => w.Length > 0);
    }

    [GeneratedRegex(@"\{\{(.+?)\}\}")]
    private static partial Regex ExpressionPattern();

    [GeneratedRegex(@"[A-Z]?[a-z]+|[A-Z]+(?=[A-Z][a-z]|\d|\b)|[A-Z]|\d+")]
    private static partial Regex WordSplitPattern();
}
