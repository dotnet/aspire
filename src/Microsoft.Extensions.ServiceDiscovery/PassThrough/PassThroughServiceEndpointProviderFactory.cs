// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

/// <summary>
/// Service endpoint provider factory which creates pass-through providers.
/// </summary>
internal sealed class PassThroughServiceEndpointProviderFactory(ILogger<PassThroughServiceEndpointProvider> logger) : IServiceEndpointProviderFactory
{
    /// <inheritdoc/>
    public bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? provider)
    {
        var serviceName = query.OriginalString;
        if (!TryCreateEndpoint(serviceName, out var endpoint))
        {
            // Propagate the value through regardless, leaving it to the caller to interpret it.
            endpoint = new DnsEndPoint(serviceName, 0);
        }

        provider = new PassThroughServiceEndpointProvider(logger, serviceName, endpoint);
        return true;
    }

    private static bool TryCreateEndpoint(string serviceName, [NotNullWhen(true)] out EndPoint? endpoint)
    {
        if ((serviceName.Contains("://", StringComparison.Ordinal) || !Uri.TryCreate($"fakescheme://{serviceName}", default, out var uri)) && !Uri.TryCreate(serviceName, default, out uri))
        {
            endpoint = null;
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
            endpoint = new IPEndPoint(ip, port);
        }
        else if (!string.IsNullOrEmpty(host))
        {
            endpoint = new DnsEndPoint(host, port);
        }
        else
        {
            endpoint = null;
            return false;
        }

        return true;
    }
}
