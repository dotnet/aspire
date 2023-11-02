// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery;

public partial class ServiceEndPointResolverFactory
{
    private sealed partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Creating endpoint resolver for service '{ServiceName}' with {Count} resolvers: {Resolvers}.", EventName = "CreatingResolver")]
        public static partial void ServiceEndPointResolverListCore(ILogger logger, string serviceName, int count, string resolvers);
        public static void CreatingResolver(ILogger logger, string serviceName, List<IServiceEndPointResolver> resolvers)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ServiceEndPointResolverListCore(logger, serviceName, resolvers.Count, string.Join(", ", resolvers.Select(static r => r.DisplayName)));
            }
        }
    }
}
