// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Aspire.Hosting.Azure.Utils;

internal static partial class BicepIdentifierHelpers
{
    // See rules from Bicep's highlightjs implementation:
    // https://github.com/Azure/bicep/blob/a992bdf2d4d7c5c7dec684b7d0de4db9cb260f8a/src/highlightjs/src/bicep.ts#L12
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex GetBicepIdentifierExpression();

    internal static void ThrowIfInvalid(string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        var regex = GetBicepIdentifierExpression();

        if (!regex.IsMatch(name))
        {
            throw new ArgumentException(
                "Bicep identifiers must only contain alpha, numeric, and _ characters and must start with an alpha or _ character.",
                paramName);
        }
    }

    /// <summary>
    /// Normalizes the given identifier name to make it a valid Bicep identifier name.
    /// </summary>
    /// <param name="name">The identifier name to normalize.</param>
    /// <returns>The normalized identifier name.</returns>
    internal static string Normalize(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var regex = GetBicepIdentifierExpression();
        if (regex.IsMatch(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length);
        // digits are not allowed as the first character, so prepend an underscore if the name starts with a digit
        if (char.IsAsciiDigit(name[0]))
        {
            builder.Append('_');
        }
        foreach (var c in name)
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '_')
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('_');
            }
        }
        return builder.ToString();
    }
}
