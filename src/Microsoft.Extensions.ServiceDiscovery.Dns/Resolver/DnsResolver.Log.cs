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

        [LoggerMessage(5, LogLevel.Warning, "Query {QueryType} {QueryName} on {Server} attempt {Attempt}: no data matching given query type.", EventName = "NoData")]
        public static partial void NoData(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(6, LogLevel.Warning, "Query {QueryType} {QueryName} on {Server} attempt {Attempt}: server indicates given name does not exist.", EventName = "NameError")]
        public static partial void NameError(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(7, LogLevel.Warning, "Query {QueryType} {QueryName} on {Server} attempt {Attempt} failed to return a valid DNS response.", EventName = "MalformedResponse")]
        public static partial void MalformedResponse(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt);

        [LoggerMessage(8, LogLevel.Warning, "Query {QueryType} {QueryName} on {Server} attempt {Attempt} failed due to a network error.", EventName = "NetworkError")]
        public static partial void NetworkError(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt, Exception exception);

        [LoggerMessage(9, LogLevel.Error, "Query {QueryType} {QueryName} on {Server} attempt {Attempt} failed.", EventName = "QueryError")]
        public static partial void QueryError(ILogger logger, QueryType queryType, string queryName, IPEndPoint server, int attempt, Exception exception);
    }
}
