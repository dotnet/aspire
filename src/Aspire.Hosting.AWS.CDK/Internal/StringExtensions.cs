// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.CDK;

internal static class StringExtensions
{
    public static string TrimStart(this string target, string trimString)
    {
        if (!string.IsNullOrEmpty(trimString))
        {
            var result = target;
            while (result.StartsWith(trimString))
            {
                result = result[trimString.Length..];
            }
            return result;
        }
        return target;
    }
}
