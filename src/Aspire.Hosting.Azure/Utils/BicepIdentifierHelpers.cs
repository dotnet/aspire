// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace Aspire.Hosting.Azure.Utils;

internal static partial class BicepIdentifierHelpers
{
    // See rules from Bicep's highlightjs implementation:
    // https://github.com/Azure/bicep/blob/a992bdf2d4d7c5c7dec684b7d0de4db9cb260f8a/src/highlightjs/src/bicep.ts#L12
    [GeneratedRegex("^[a-z_][a-z0-9_]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GetBicepIdentifierExpression();

    internal static void ThrowIfInvalid(string bicepParameterName)
    {
        var regex = GetBicepIdentifierExpression();

        if (!regex.IsMatch(bicepParameterName))
        {
            throw new ArgumentException(
                "Bicep parameter names must only contain alpha, numeric, and _ characters and must start with an alpha or _ characters.",
                nameof(bicepParameterName)
                );
        }
    }

    private static readonly SearchValues<char> s_validChars = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_");

    /// <summary>
    /// Normalizes the given variable name to make it a valid Bicep identifier name.
    /// </summary>
    /// <param name="name">The variable name to normalize.</param>
    /// <returns>The normalized variable name.</returns>
    internal static string Normalize(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));

        var builder = new StringBuilder(name.Length);
        var nameSpan = name.AsSpan();
        // If the name starts with a digit, we need to prefix it with an underscore to make it a valid Bicep variable name.
        if (char.IsAsciiDigit(nameSpan[0]))
        {
            builder.Append('_');
        }

        // Replace all invalid characters with underscores.
        while (!nameSpan.IsEmpty)
        {
            var nextInvalidChar = nameSpan.IndexOfAnyExcept(s_validChars);
            if (nextInvalidChar == -1)
            {
                builder.Append(nameSpan);
                break;
            }

            builder.Append(nameSpan[..nextInvalidChar]);
            builder.Append('_');
            nameSpan = nameSpan[(nextInvalidChar + 1)..];
        }

        return builder.ToString();
    }
}
