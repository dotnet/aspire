// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal enum SendQueryError
{
    /// <summary>
    /// DNS query was successful and returned response message with answers.
    /// </summary>
    NoError,

    /// <summary>
    /// Server failed to respond to the query withing specified timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// Server returned a response with an error code.
    /// </summary>
    ServerError,

    /// <summary>
    /// Server returned a malformed response.
    /// </summary>
    MalformedResponse,

    /// <summary>
    /// Server returned a response indicating no data are available.
    /// </summary>
    NoData,

    /// <summary>
    /// Network-level error occurred during the query.
    /// </summary>
    NetworkError,

    /// <summary>
    /// Internal error on part of the implementation.
    /// </summary>
    InternalError,
}
