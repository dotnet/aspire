// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Azure.Utils;

internal static partial class BicepParameterNameValidator
{
    // See rules from Bicep's highlightjs implementation:
    // https://github.com/Azure/bicep/blob/a992bdf2d4d7c5c7dec684b7d0de4db9cb260f8a/src/highlightjs/src/bicep.ts#L12
    [GeneratedRegex("^[a-z_][a-z0-9_]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GetBicepParameterExpression();

    internal static void ThrowIfInvalid(string bicepParameterName)
    {
        var regex = GetBicepParameterExpression();

        if (!regex.IsMatch(bicepParameterName))
        {
            throw new ArgumentException(
                "Bicep parameter names must only contain alpha, numeric, and _ characters and must start with an alpha or _ characters.",
                nameof(bicepParameterName)
                );
        }
    }
}
