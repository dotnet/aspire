// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Utils;

internal class HostNameResolver
{
    // HACK: When the destination resource is a container, we need to replace the localhost with host.docker.internal
    // so the container can access the other container via the host's IP address.
    internal static string ReplaceLocalhostWithContainerHost(string value, IConfiguration configuration)
    {
        // https://stackoverflow.com/a/43541732/45091

        // This configuration value is a workaround for the fact that host.docker.internal is not available on Linux by default.
        var hostName = configuration["AppHost:ContainerHostname"] ?? "host.docker.internal";

        return value.Replace("localhost", hostName, StringComparison.OrdinalIgnoreCase)
                    .Replace("127.0.0.1", hostName)
                    .Replace("[::1]", hostName);
    }
}
