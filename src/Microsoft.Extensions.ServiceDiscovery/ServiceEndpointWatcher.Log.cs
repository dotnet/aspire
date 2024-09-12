// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery;

partial class ServiceEndpointWatcher
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Resolving endpoints for service '{ServiceName}'.", EventName = "ResolvingEndpoints")]
        public static partial void ResolvingEndpoints(ILogger logger, string serviceName);

        [LoggerMessage(2, LogLevel.Debug, "Endpoint resolution is pending for service '{ServiceName}'.", EventName = "ResolutionPending")]
        public static partial void ResolutionPending(ILogger logger, string serviceName);

        [LoggerMessage(3, LogLevel.Debug, "Resolved {Count} endpoints for service '{ServiceName}': {Endpoints}.", EventName = "ResolutionSucceeded")]
        public static partial void ResolutionSucceededCore(ILogger logger, int count, string serviceName, string endpoints);

        public static void ResolutionSucceeded(ILogger logger, string serviceName, ServiceEndpointSource endpointSource)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ResolutionSucceededCore(logger, endpointSource.Endpoints.Count, serviceName, string.Join(", ", endpointSource.Endpoints.Select(GetEndpointString)));
            }

            static string GetEndpointString(ServiceEndpoint ep)
            {
                if (ep.Features.Get<IServiceEndpointProvider>() is { } provider)
                {
                    return $"{ep} ({provider})";
                }

                return ep.ToString()!;
            }
        }

        [LoggerMessage(4, LogLevel.Error, "Error resolving endpoints for service '{ServiceName}'.", EventName = "ResolutionFailed")]
        public static partial void ResolutionFailed(ILogger logger, Exception exception, string serviceName);
    }
}
