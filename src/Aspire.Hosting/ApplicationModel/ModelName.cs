// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Note this file is included in the Aspire.Hosting.Analyzers project which targets netstandard2.0
/// </summary>
internal static class ModelName
{
    internal static bool IsValidName(string target, string name) => TryValidateName(target, name, out _);

    internal static void ValidateName(string target, string name)
    {
#pragma warning disable CA1510 // Use ArgumentNullException throw helper
        // This file is included in projects targeting netstandard2.0
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }
#pragma warning restore CA1510

        if (!TryValidateName(target, name, out var validationMessage))
        {
            throw new ArgumentException(validationMessage, nameof(name));
        }
    }

    /// <summary>
    /// Validate that a model name is valid.
    /// - Must start with an ASCII letter.
    /// - Must contain only ASCII letters, digits, and hyphens.
    /// - Must not end with a hyphen.
    /// - Must not contain consecutive hyphens.
    /// - Must be between 1 and 64 characters long.
    /// </summary>
    internal static bool TryValidateName(string target, string name, out string? validationMessage)
    {
        validationMessage = null;

        if (name.Length < 1 || name.Length > 64)
        {
            validationMessage = $"{target} name '{name}' is invalid. Name must be between 1 and 64 characters long.";
            return false;
        }

        var lastCharacterHyphen = false;
        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '-')
            {
                if (lastCharacterHyphen)
                {
                    validationMessage = $"{target} name '{name}' is invalid. Name cannot contain consecutive hyphens.";
                    return false;
                }
                lastCharacterHyphen = true;
            }
            else if (!IsAsciiLetterOrDigit(name[i]))
            {
                validationMessage = $"{target} name '{name}' is invalid. Name must contain only ASCII letters, digits, and hyphens.";
                return false;
            }
            else
            {
                lastCharacterHyphen = false;
            }
        }

        if (!IsAsciiLetter(name[0]))
        {
            validationMessage = $"{target} name '{name}' is invalid. Name must start with an ASCII letter.";
            return false;
        }

        if (name[name.Length - 1] == '-')
        {
            validationMessage = $"{target} name '{name}' is invalid. Name cannot end with a hyphen.";
            return false;
        }

        return true;
    }

    private static bool IsAsciiLetter(char c)
    {
#if NET8_0_OR_GREATER
        return char.IsAsciiLetter(c);
#else
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
#endif
    }

    private static bool IsAsciiLetterOrDigit(char c)
    {
#if NET8_0_OR_GREATER
        return char.IsAsciiLetterOrDigit(c);
#else
        return IsAsciiLetter(c) || char.IsDigit(c);
#endif
    }
}
