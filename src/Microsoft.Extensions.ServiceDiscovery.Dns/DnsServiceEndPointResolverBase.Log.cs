// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal partial class DnsServiceEndPointResolverBase
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "Resolving endpoints for service '{ServiceName}' using DNS SRV lookup for name '{RecordName}'.", EventName = "SrvQuery")]
        public static partial void SrvQuery(ILogger logger, string serviceName, string recordName);

        [LoggerMessage(2, LogLevel.Trace, "Resolving endpoints for service '{ServiceName}' using host lookup for name '{RecordName}'.", EventName = "AddressQuery")]
        public static partial void AddressQuery(ILogger logger, string serviceName, string recordName);

        [LoggerMessage(3, LogLevel.Debug, "Skipping endpoint resolution for service '{ServiceName}': '{Reason}'.", EventName = "SkippedResolution")]
        public static partial void SkippedResolution(ILogger logger, string serviceName, string reason);

        [LoggerMessage(4, LogLevel.Debug, "Service name '{ServiceName}' is not a valid URI or DNS name.", EventName = "ServiceNameIsNotUriOrDnsName")]
        public static partial void ServiceNameIsNotUriOrDnsName(ILogger logger, string serviceName);

        [LoggerMessage(5, LogLevel.Debug, "DNS SRV query cannot be constructed for service name '{ServiceName}' because no DNS namespace was configured or detected.", EventName = "NoDnsSuffixFound")]
        public static partial void NoDnsSuffixFound(ILogger logger, string serviceName);
    }
}
