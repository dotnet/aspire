// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Provides helper methods for working with endpoint hosts.
/// </summary>
public static class EndpointHostHelpers
{
    /// <summary>
    /// Determines whether the specified host is a localhost address.
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host is "localhost" (case-insensitive) or ends with ".localhost" (case-insensitive);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostAddress(string? host)
    {
        return host is not null &&
               (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase));
    }
}
