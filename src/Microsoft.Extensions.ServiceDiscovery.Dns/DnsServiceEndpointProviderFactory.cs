// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsServiceEndpointProviderFactory(
    IOptionsMonitor<DnsServiceEndpointProviderOptions> options,
    ILogger<DnsServiceEndpointProvider> logger,
    TimeProvider timeProvider) : IServiceEndpointProviderFactory
{
    /// <inheritdoc/>
    public bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? provider)
    {
        provider = new DnsServiceEndpointProvider(query, hostName: query.ServiceName, options, logger, timeProvider);
        return true;
    }
}
