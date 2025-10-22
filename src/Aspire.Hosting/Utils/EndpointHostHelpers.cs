// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Provides helper methods for validating localhost addresses.
/// </summary>
public static class EndpointHostHelpers
{
    /// <summary>
    /// Determines whether the specified host is "localhost".
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host is "localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhost(string? host)
    {
        return host is not null && string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified host ends with ".localhost".
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host ends with ".localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostTld(string? host)
    {
        return host is not null && host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified host is "localhost" or uses the ".localhost" top-level domain.
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host is "localhost" (case-insensitive) or ends with ".localhost" (case-insensitive);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostOrLocalhostTld(string? host)
    {
        return IsLocalhost(host) || IsLocalhostTld(host);
    }
}
