// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Hosting.Azure.Utils;

internal static partial class BicepParameterNameValidator
{
    [GeneratedRegex("^[a-z_]+[a-z0-9_]*$", RegexOptions.IgnoreCase)]
    private static partial Regex GetBicepParameterExpression();

    internal static void ThrowIfInvalid(string bicepParameterName)
    {
        var regex = GetBicepParameterExpression();

        if (!regex.IsMatch(bicepParameterName))
        {
            throw new ArgumentException(
                "Bicep parameter names must only contain alpha, numeric, and _ characters with a leading alpha character.",
                nameof(bicepParameterName)
                );
        }
    }
}
