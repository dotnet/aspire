// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

/// <summary>
/// HTTP message handler which resolves endpoints using service discovery.
/// </summary>
public class ResolvingHttpDelegatingHandler : DelegatingHandler
{
    private readonly HttpServiceEndPointResolver _resolver;

    /// <summary>
    /// Initializes a new <see cref="ResolvingHttpDelegatingHandler"/> instance.
    /// </summary>
    /// <param name="resolver">The endpoint resolver.</param>
    public ResolvingHttpDelegatingHandler(HttpServiceEndPointResolver resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// Initializes a new <see cref="ResolvingHttpDelegatingHandler"/> instance.
    /// </summary>
    /// <param name="resolver">The endpoint resolver.</param>
    /// <param name="innerHandler">The inner handler.</param>
    public ResolvingHttpDelegatingHandler(HttpServiceEndPointResolver resolver, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _resolver = resolver;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var originalUri = request.RequestUri;
        IEndPointHealthFeature? epHealth = null;
        Exception? error = null;
        var responseDuration = ValueStopwatch.StartNew();
        if (originalUri?.Host is not null)
        {
            var result = await _resolver.GetEndpointAsync(request, cancellationToken).ConfigureAwait(false);
            request.RequestUri = GetUriWithEndPoint(originalUri, result);
            request.Headers.Host ??= result.Features.Get<IHostNameFeature>()?.HostName;
            epHealth = result.Features.Get<IEndPointHealthFeature>();
        }

        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            error = exception;
            throw;
        }
        finally
        {
            epHealth?.ReportHealth(responseDuration.GetElapsedTime(), error); // Report health so that the resolver pipeline can take health and performance into consideration, possibly triggering a circuit breaker?.
            request.RequestUri = originalUri;
        }
    }

    internal static Uri GetUriWithEndPoint(Uri uri, ServiceEndPoint serviceEndPoint)
    {
        var endpoint = serviceEndPoint.EndPoint;

        string host;
        int port;
        switch (endpoint)
        {
            case IPEndPoint ip:
                host = ip.Address.ToString();
                port = ip.Port;
                break;
            case DnsEndPoint dns:
                host = dns.Host;
                port = dns.Port;
                break;
            default:
                throw new InvalidOperationException($"Endpoints of type {endpoint.GetType()} are not supported");
        }

        var builder = new UriBuilder(uri)
        {
            Host = host,
        };

        // Default to the default port for the scheme.
        if (port > 0)
        {
            builder.Port = port;
        }

        return builder.Uri;
    }
}
