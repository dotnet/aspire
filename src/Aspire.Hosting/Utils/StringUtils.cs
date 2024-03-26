// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Utils;

internal static class StringUtils
{
    public static bool TryGetUriFromDelimitedString(string input, string delimiter, [NotNullWhen(true)] out Uri? uri)
    {
        if (!string.IsNullOrEmpty(input)
            && input.Split(delimiter) is { Length: > 0 } splitInput
            && Uri.TryCreate(splitInput[0], UriKind.Absolute, out uri))
        {
            return true;
        }
        else
        {
            uri = null;
            return false;
        }
    }
}
