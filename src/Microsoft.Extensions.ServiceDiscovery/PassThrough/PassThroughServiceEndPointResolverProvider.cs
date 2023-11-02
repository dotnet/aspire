// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

/// <summary>
/// Service endpoint resolver provider which passes through the provided value.
/// </summary>
internal sealed class PassThroughServiceEndPointResolverProvider(ILogger<PassThroughServiceEndPointResolver> logger) : IServiceEndPointResolverProvider
{
    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
    {
        if (!ServiceNameParts.TryCreateEndPoint(serviceName, out var endPoint))
        {
            // Propagate the value through regardless, leaving it to the caller to interpret it.
            endPoint = new DnsEndPoint(serviceName, 0);
        }

        resolver = new PassThroughServiceEndPointResolver(logger, serviceName, endPoint);
        return true;
    }
}
