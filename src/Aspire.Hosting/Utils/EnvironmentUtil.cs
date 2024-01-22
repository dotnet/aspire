// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Utils;

internal static class EnvironmentUtil
{
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static Uri[]? GetAddressUris(string variableName, Uri? defaultValue)
    {
        try
        {
            var urls = Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrWhiteSpace(urls))
            {
                return defaultValue switch
                {
                    not null => [defaultValue],
                    null => null
                };
            }
            else
            {
                return urls
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(url => new Uri(url))
                    .ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
        }
    }
}
