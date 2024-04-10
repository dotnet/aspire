// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Aspire.Dashboard.Extensions;

internal static class StringExtensions
{
    public static string SanitizeHtmlId(this string input)
    {
        var sanitizedBuilder = new StringBuilder(capacity: input.Length);

        foreach (var c in input)
        {
            if (IsValidHtmlIdCharacter(c))
            {
                sanitizedBuilder.Append(c);
            }
            else
            {
                sanitizedBuilder.Append('_');
            }
        }

        return sanitizedBuilder.ToString();

        static bool IsValidHtmlIdCharacter(char c)
        {
            // Check if the character is a letter, digit, underscore, or hyphen
            return char.IsLetterOrDigit(c) || c == '_' || c == '-';
        }
    }

    /// <summary>
    /// Returns the two initial letters of the first and last words in the specified <paramref name="name"/>.
    /// If only one word is present, a single initial is returned. If <paramref name="name"/> is null, empty or
    /// white space only, <paramref name="defaultValue"/> is returned.
    /// </summary>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetInitials(this string name, string? defaultValue = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return defaultValue;
        }

        var initials = name.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s[0].ToString())
                           .ToList();

        if (initials.Count > 1)
        {
            // If the name contained two or more words, return the initials from the first and last
            return initials[0].ToUpperInvariant() + initials[^1].ToUpperInvariant();
        }

        return initials[0].ToUpperInvariant();
    }
}
