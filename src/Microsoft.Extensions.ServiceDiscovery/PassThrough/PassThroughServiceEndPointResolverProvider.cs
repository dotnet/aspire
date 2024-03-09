// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

/// <summary>
/// Service endpoint resolver provider which passes through the provided value.
/// </summary>
internal sealed class PassThroughServiceEndPointResolverProvider(ILogger<PassThroughServiceEndPointResolver> logger) : IServiceEndPointResolverProvider
{
    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointProvider? resolver)
    {
        if (!TryCreateEndPoint(serviceName, out var endPoint))
        {
            // Propagate the value through regardless, leaving it to the caller to interpret it.
            endPoint = new DnsEndPoint(serviceName, 0);
        }

        resolver = new PassThroughServiceEndPointResolver(logger, serviceName, endPoint);
        return true;
    }

    private static bool TryCreateEndPoint(string serviceName, [NotNullWhen(true)] out EndPoint? serviceEndPoint)
    {
        if ((serviceName.Contains("://", StringComparison.Ordinal) || !Uri.TryCreate($"fakescheme://{serviceName}", default, out var uri)) && !Uri.TryCreate(serviceName, default, out uri))
        {
            serviceEndPoint = null;
            return false;
        }

        var uriHost = uri.Host;
        var segmentSeparatorIndex = uriHost.IndexOf('.');
        string host;
        if (uriHost.StartsWith('_') && segmentSeparatorIndex > 1 && uriHost[^1] != '.')
        {
            // Skip the endpoint name, including its prefix ('_') and suffix ('.').
            host = uriHost[(segmentSeparatorIndex + 1)..];
        }
        else
        {
            host = uriHost;
        }

        var port = uri.Port > 0 ? uri.Port : 0;
        if (IPAddress.TryParse(host, out var ip))
        {
            serviceEndPoint = new IPEndPoint(ip, port);
        }
        else if (!string.IsNullOrEmpty(host))
        {
            serviceEndPoint = new DnsEndPoint(host, port);
        }
        else
        {
            serviceEndPoint = null;
            return false;
        }

        return true;
    }
}
