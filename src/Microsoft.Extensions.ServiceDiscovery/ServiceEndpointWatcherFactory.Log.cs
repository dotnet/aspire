// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery;

partial class ServiceEndpointWatcherFactory
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Creating endpoint resolver for service '{ServiceName}' with {Count} providers: {Providers}.", EventName = "CreatingResolver")]
        public static partial void ServiceEndpointProviderListCore(ILogger logger, string serviceName, int count, string providers);

        public static void CreatingResolver(ILogger logger, string serviceName, List<IServiceEndpointProvider> providers)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ServiceEndpointProviderListCore(logger, serviceName, providers.Count, string.Join(", ", providers.Select(static r => r.ToString())));
            }
        }
    }
}
