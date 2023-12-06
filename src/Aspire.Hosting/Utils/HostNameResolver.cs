// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Helpers to resolve host names when running in a containers.
/// </summary>
public class HostNameResolver
{
    /// <summary>
    /// Resolves the "localhost" with the container host name.
    /// </summary>
    /// <param name="value">The value that contains the localhost</param>
    /// <param name="configuration">The configuration object.</param>
    /// <returns>A new value with localhost replaced with the container host name</returns>
    public static string ReplaceLocalhostWithContainerHost(string value, IConfiguration configuration)
    {
        // https://stackoverflow.com/a/43541732/45091

        // This configuration value is a workaround for the fact that host.docker.internal is not available on Linux by default.
        var hostName = configuration["AppHost:ContainerHostname"] ?? "host.docker.internal";

        return value.Replace("localhost", hostName, StringComparison.OrdinalIgnoreCase)
                    .Replace("127.0.0.1", hostName)
                    .Replace("[::1]", hostName);
    }
}
