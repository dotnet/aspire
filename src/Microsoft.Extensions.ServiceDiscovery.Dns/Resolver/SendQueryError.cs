// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal enum SendQueryError
{
    NoError,
    Timeout,
    ServerError,
    ParseError,
    NoData,
}
