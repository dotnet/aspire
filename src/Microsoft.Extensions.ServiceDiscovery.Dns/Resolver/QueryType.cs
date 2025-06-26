// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

/// <summary>
/// DNS Query Types.
/// </summary>
internal enum QueryType
{
    /// <summary>
    /// A host address.
    /// </summary>
    A = 1,

    /// <summary>
    /// An authoritative name server.
    /// </summary>
    NS = 2,

    /// <summary>
    /// The canonical name for an alias.
    /// </summary>
    CNAME = 5,

    /// <summary>
    /// Marks the start of a zone of authority.
    /// </summary>
    SOA = 6,

    /// <summary>
    /// Mail exchange.
    /// </summary>
    MX = 15,

    /// <summary>
    /// Text strings.
    /// </summary>
    TXT = 16,

    /// <summary>
    /// IPv6 host address. (RFC 3596)
    /// </summary>
    AAAA = 28,

    /// <summary>
    /// Location information. (RFC 2782)
    /// </summary>
    SRV = 33,

    /// <summary>
    /// Wildcard match.
    /// </summary>
    All = 255
}
