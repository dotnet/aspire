// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

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
    public static bool IsLocalhost([NotNullWhen(true)] string? host)
    {
        return host is not null && string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified URI uses a host that is "localhost".
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <returns>
    /// <c>true</c> if the host is "localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhost([NotNullWhen(true)] Uri? uri)
    {
        return uri?.Host is not null && IsLocalhost(uri.Host);
    }

    /// <summary>
    /// Determines whether the specified host ends with ".localhost".
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host ends with ".localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostTld([NotNullWhen(true)] string? host)
    {
        return host is not null && host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified host ends with ".dev.localhost".
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host ends with ".dev.localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsDevLocalhostTld([NotNullWhen(true)] string? host)
    {
        return host is not null && host.EndsWith(".dev.localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified URI uses a host that is "localhost".
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <returns>
    /// <c>true</c> if the host ends with ".localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostTld([NotNullWhen(true)] Uri? uri)
    {
        return uri?.Host is not null && IsLocalhostTld(uri.Host);
    }

    /// <summary>
    /// Determines whether the specified URI uses a host that ends with ".dev.localhost".
    /// </summary>
    /// <param name="uri">The URI to check.</param>
    /// <returns>
    /// <c>true</c> if the host ends with ".dev.localhost" (case-insensitive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsDevLocalhostTld([NotNullWhen(true)] Uri? uri)
    {
        return uri?.Host is not null && IsDevLocalhostTld(uri.Host);
    }

    /// <summary>
    /// Determines whether the specified host is "localhost" or uses the ".localhost" top-level domain.
    /// </summary>
    /// <param name="host">The host to check.</param>
    /// <returns>
    /// <c>true</c> if the host is "localhost" (case-insensitive) or ends with ".localhost" (case-insensitive);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostOrLocalhostTld([NotNullWhen(true)] string? host)
    {
        return IsLocalhost(host) || IsLocalhostTld(host);
    }

    /// <summary>
    /// Determines whether the specified URI uses a host that is "localhost" or ends with ".localhost".
    /// </summary>
    /// <param name="uri"></param>
    /// <returns>
    /// <c>true</c> if the host is "localhost" (case-insensitive) or ends with ".localhost" (case-insensitive);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLocalhostOrLocalhostTld([NotNullWhen(true)] Uri? uri)
    {
        return uri?.Host is not null && IsLocalhostOrLocalhostTld(uri.Host);
    }
}
