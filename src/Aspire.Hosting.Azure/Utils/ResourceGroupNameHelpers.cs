// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Hosting.Azure.Utils;

internal static class ResourceGroupNameHelpers
{
    public static int MaxResourceGroupNameLength = 90;

    /// <summary>
    /// Converts or excludes any characters which are not valid resource group name components.
    /// </summary>
    /// <param name="resourceGroupName">The text to normalize.</param>
    /// <returns>The normalized resource group name or an empty string if no characters were valid.</returns>
    public static string NormalizeResourceGroupName(string resourceGroupName)
    {
        resourceGroupName = RemoveDiacritics(resourceGroupName);

        var stringBuilder = new StringBuilder(capacity: resourceGroupName.Length);

        for (var i = 0; i < resourceGroupName.Length; i++)
        {
            var c = resourceGroupName[i];

            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')
            {
                continue;
            }

            stringBuilder.Append(c);
        }

        return stringBuilder.ToString();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (var i = 0; i < normalizedString.Length; i++)
        {
            var c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}
