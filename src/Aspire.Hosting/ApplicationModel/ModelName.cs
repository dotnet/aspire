// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal static class ModelName
{
    /// <summary>
    /// Validate that a model name is valid.
    /// - Must start with an ASCII letter.
    /// - Must contain only ASCII letters, digits, and hyphens.
    /// - Must not end with a hyphen.
    /// - Must not contain consecutive hyphens.
    /// - Must be between 1 and 64 characters long.
    /// </summary>
    internal static void ValidateName(string target, string name)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length < 1 || name.Length > 64)
        {
            throw new ArgumentException($"{target} name '{name}' is invalid. Name must be between 1 and 64 characters long.", nameof(name));
        }

        var lastCharacterHyphen = false;
        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '-')
            {
                if (lastCharacterHyphen)
                {
                    throw new ArgumentException($"{target} name '{name}' is invalid. Name cannot contain consecutive hyphens.", nameof(name));
                }
                lastCharacterHyphen = true;
            }
            else if (!char.IsAsciiLetterOrDigit(name[i]))
            {
                throw new ArgumentException($"{target} name '{name}' is invalid. Name must contain only ASCII letters, digits, and hyphens.", nameof(name));
            }
            else
            {
                lastCharacterHyphen = false;
            }
        }

        if (!char.IsAsciiLetter(name[0]))
        {
            throw new ArgumentException($"{target} name '{name}' is invalid. Name must start with an ASCII letter.", nameof(name));
        }

        if (name[^1] == '-')
        {
            throw new ArgumentException($"{target} name '{name}' is invalid. Name cannot end with a hyphen.", nameof(name));
        }
    }
}
