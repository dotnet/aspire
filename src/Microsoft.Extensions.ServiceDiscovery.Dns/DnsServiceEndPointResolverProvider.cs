// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsServiceEndPointResolverProvider(
    IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
    ILogger<DnsServiceEndPointResolver> logger,
    TimeProvider timeProvider,
    ServiceNameParser parser) : IServiceEndPointResolverProvider
{
    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointProvider? resolver)
    {
        if (!parser.TryParse(serviceName, out var parts))
        {
            DnsServiceEndPointResolverBase.Log.ServiceNameIsNotUriOrDnsName(logger, serviceName);
            resolver = default;
            return false;
        }

        resolver = new DnsServiceEndPointResolver(serviceName, hostName: parts.Host, options, logger, timeProvider);
        return true;
    }
}
