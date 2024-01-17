// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

internal static class EnvironmentUtil
{
    public static Uri[] GetAddressUris(string variableName, string defaultValue)
    {
        try
        {
            var urls = Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrWhiteSpace(urls))
            {
                urls = defaultValue;
            }

            return urls
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(url => new Uri(url))
                .ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
        }
    }
}
