// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal partial class DnsResolver : IDnsResolver, IDisposable
{
    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Resolving {QueryType} {QueryName} on {Server} attempt {Attempt}", EventName = "Query")]
        public static partial void Query(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(2, LogLevel.Debug, "Result truncated for {QueryType} {QueryName} from {Server} attempt {Attempt}. Restarting over TCP", EventName = "ResultTruncated")]
        public static partial void ResultTruncated(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(3, LogLevel.Error, "Server {Server} replied with {ResponseCode} when querying {QueryType} {QueryName}", EventName = "ErrorResponseCode")]
        public static partial void ErrorResponseCode(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, QueryResponseCode responseCode);

        [LoggerMessage(4, LogLevel.Warning, "Query {QueryType} {QueryName} on {Server} attempt {Attempt} timed out.", EventName = "Timeout")]
        public static partial void Timeout(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(5, LogLevel.Warning, "Query {QueryType} {QueryName} on {Server} attempt {Attempt} returned no data", EventName = "NoData")]
        public static partial void NoData(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(6, LogLevel.Error, "Query {QueryType} {QueryName} on {Server} attempt {Attempt} failed.", EventName = "QueryError")]
        public static partial void QueryError(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt, Exception exception);
    }
}
