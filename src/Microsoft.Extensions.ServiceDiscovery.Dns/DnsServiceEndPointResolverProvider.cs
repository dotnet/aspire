// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsServiceEndPointResolverProvider(
    IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
    ILogger<DnsServiceEndPointResolver> logger,
    TimeProvider timeProvider) : IServiceEndPointProviderFactory
{
    /// <inheritdoc/>
    public bool TryCreateProvider(ServiceEndPointQuery query, [NotNullWhen(true)] out IServiceEndPointProvider? resolver)
    {
        resolver = new DnsServiceEndPointResolver(query.OriginalString, hostName: query.ServiceName, options, logger, timeProvider);
        return true;
    }
}
