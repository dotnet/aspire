// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Utils;

internal static class ResourceGroupNameHelpers
{
    /// <summary>
    /// Excludes any characters which are not valid resource group name components.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized resource group name, or an empty string if no characters where valid.</returns>
    public static string NormalizeResourceGroupName(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var stringBuilder = new StringBuilder(capacity: text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            // Only be a letter, digit, '-', '.', '(', ')' or '_'
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '.' && c != '(' && c != ')' && c != '_')
            {
                continue;
            }

            stringBuilder.Append(c);
        }

        // Can't end with '.'
        return stringBuilder.ToString().TrimEnd('.');
    }
}
