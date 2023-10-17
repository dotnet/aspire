// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal partial class DnsServiceEndPointResolver
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "Resolving endpoints for service '{ServiceName}' using host lookup for name '{RecordName}'.", EventName = "AddressQuery")]
        public static partial void AddressQuery(ILogger logger, string serviceName, string recordName);

        public static void DiscoveredEndPoints(ILogger logger, List<ServiceEndPoint> endPoints, string serviceName, TimeSpan ttl)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                DiscoveredEndPointsCoreTrace(logger, endPoints.Count, serviceName, ttl, string.Join(", ", endPoints.Select(static ep => ep.GetEndPointString())));
            }
            else if (logger.IsEnabled(LogLevel.Debug))
            {
                DiscoveredEndPointsCoreDebug(logger, endPoints.Count, serviceName, ttl);
            }
        }

        [LoggerMessage(2, LogLevel.Debug, "Discovered {Count} endpoints for service '{ServiceName}'. Will refresh in {Ttl}.", EventName = "DiscoveredEndPoints")]
        public static partial void DiscoveredEndPointsCoreDebug(ILogger logger, int count, string serviceName, TimeSpan ttl);

        [LoggerMessage(3, LogLevel.Debug, "Discovered {Count} endpoints for service '{ServiceName}'. Will refresh in {Ttl}. EndPoints: {EndPoints}", EventName = "DiscoveredEndPointsDetailed")]
        public static partial void DiscoveredEndPointsCoreTrace(ILogger logger, int count, string serviceName, TimeSpan ttl, string endPoints);

        [LoggerMessage(4, LogLevel.Warning, "Endpoints resolution failed for service '{ServiceName}'.", EventName = "ResolutionFailed")]
        public static partial void ResolutionFailed(ILogger logger, Exception exception, string serviceName);

        [LoggerMessage(5, LogLevel.Debug, "Service name '{ServiceName}' is not a valid URI or DNS name.", EventName = "ServiceNameIsNotUriOrDnsName")]
        public static partial void ServiceNameIsNotUriOrDnsName(ILogger<DnsServiceEndPointResolver> logger, string serviceName);
    }
}
