// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery;

public sealed partial class ServiceEndPointResolver
{
    private sealed partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Resolving endpoints for service '{ServiceName}'.", EventName = "ResolvingEndPoints")]
        public static partial void ResolvingEndPoints(ILogger logger, string serviceName);

        [LoggerMessage(2, LogLevel.Debug, "Endpoint resolution is pending for service '{ServiceName}'.", EventName = "ResolutionPending")]
        public static partial void ResolutionPending(ILogger logger, string serviceName);

        [LoggerMessage(3, LogLevel.Debug, "Resolved {Count} endpoints for service '{ServiceName}': {EndPoints}.", EventName = "ResolutionSucceeded")]
        public static partial void ResolutionSucceededCore(ILogger logger, int count, string serviceName, string endPoints);

        public static void ResolutionSucceeded(ILogger logger, string serviceName, ServiceEndPointCollection endPoints)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                ResolutionSucceededCore(logger, endPoints.Count, serviceName, string.Join(", ", endPoints.Select(GetEndPointString)));
            }

            static string GetEndPointString(ServiceEndPoint ep)
            {
                if (ep.Features.Get<IServiceEndPointResolver>() is { } resolver)
                {
                    return $"{ep.GetEndPointString()} ({resolver.DisplayName})";
                }

                return ep.GetEndPointString();
            }   
        }

        [LoggerMessage(4, LogLevel.Error, "Error resolving endpoints for service '{ServiceName}'.", EventName = "ResolutionFailed")]
        public static partial void ResolutionFailed(ILogger logger, Exception exception, string serviceName);
    }
}
