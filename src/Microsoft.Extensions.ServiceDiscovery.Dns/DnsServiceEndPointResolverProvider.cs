// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// Provides <see cref="IServiceEndPointResolver"/> instances which resolve endpoints from DNS.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="DnsServiceEndPointResolverProvider"/> instance.
/// </remarks>
/// <param name="options">The options.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The time provider.</param>
internal sealed partial class DnsServiceEndPointResolverProvider(
    IOptionsMonitor<DnsServiceEndPointResolverOptions> options,
    ILogger<DnsServiceEndPointResolver> logger,
    TimeProvider timeProvider) : IServiceEndPointResolverProvider
{
    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
    {
        if (!ServiceNameParts.TryParse(serviceName, out var parts))
        {
            DnsServiceEndPointResolverBase.Log.ServiceNameIsNotUriOrDnsName(logger, serviceName);
            resolver = default;
            return false;
        }

        resolver = new DnsServiceEndPointResolver(serviceName, hostName: parts.Host, options, logger, timeProvider);
        return true;
    }
}
