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
        var serviceName = query.ToString()!;
        if (!TryCreateEndPoint(serviceName, out var endPoint))
        {
            // Propagate the value through regardless, leaving it to the caller to interpret it.
            endPoint = new DnsEndPoint(serviceName, 0);
        }

        provider = new PassThroughServiceEndpointProvider(logger, serviceName, endPoint);
        return true;
    }

    private static bool TryCreateEndPoint(string serviceName, [NotNullWhen(true)] out EndPoint? endPoint)
    {
        if ((serviceName.Contains("://", StringComparison.Ordinal) || !Uri.TryCreate($"fakescheme://{serviceName}", default, out var uri)) && !Uri.TryCreate(serviceName, default, out uri))
        {
            endPoint = null;
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
            endPoint = new IPEndPoint(ip, port);
        }
        else if (!string.IsNullOrEmpty(host))
        {
            endPoint = new DnsEndPoint(host, port);
        }
        else
        {
            endPoint = null;
            return false;
        }

        return true;
    }
}
