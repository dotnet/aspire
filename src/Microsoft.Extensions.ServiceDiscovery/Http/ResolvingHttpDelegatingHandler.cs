// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

/// <summary>
/// HTTP message handler which resolves endpoints using service discovery.
/// </summary>
public class ResolvingHttpDelegatingHandler : DelegatingHandler
{
    private readonly HttpServiceEndPointResolver _resolver;
    private readonly ServiceDiscoveryOptions _options;

    /// <summary>
    /// Initializes a new <see cref="ResolvingHttpDelegatingHandler"/> instance.
    /// </summary>
    /// <param name="resolver">The endpoint resolver.</param>
    /// <param name="options">The service discovery options.</param>
    public ResolvingHttpDelegatingHandler(HttpServiceEndPointResolver resolver, IOptions<ServiceDiscoveryOptions> options)
    {
        _resolver = resolver;
        _options = options.Value;
    }

    /// <summary>
    /// Initializes a new <see cref="ResolvingHttpDelegatingHandler"/> instance.
    /// </summary>
    /// <param name="resolver">The endpoint resolver.</param>
    /// <param name="options">The service discovery options.</param>
    /// <param name="innerHandler">The inner handler.</param>
    public ResolvingHttpDelegatingHandler(HttpServiceEndPointResolver resolver, IOptions<ServiceDiscoveryOptions> options, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _resolver = resolver;
        _options = options.Value;
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
            request.RequestUri = GetUriWithEndPoint(originalUri, result, _options);
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

    internal static Uri GetUriWithEndPoint(Uri uri, ServiceEndPoint serviceEndPoint, ServiceDiscoveryOptions options)
    {
        var endpoint = serviceEndPoint.EndPoint;
        UriBuilder result;
        if (endpoint is UriEndPoint { Uri: { } ep })
        {
            result = new UriBuilder(uri)
            {
                Scheme = ep.Scheme,
                Host = ep.Host,
            };

            if (ep.Port > 0)
            {
                result.Port = ep.Port;
            }

            if (ep.AbsolutePath.Length > 1)
            {
                result.Path = $"{ep.AbsolutePath.TrimEnd('/')}/{uri.AbsolutePath.TrimStart('/')}";
            }
        }
        else
        {
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

            result = new UriBuilder(uri)
            {
                Host = host,
            };

            // Default to the default port for the scheme.
            if (port > 0)
            {
                result.Port = port;
            }

            if (uri.Scheme.IndexOf('+') > 0)
            {
                var scheme = uri.Scheme.Split('+')[0];
                if (options.AllowedSchemes.Equals(ServiceDiscoveryOptions.AllSchemes) || options.AllowedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
                {
                    result.Scheme = scheme;
                }
                else
                {
                    throw new InvalidOperationException($"The scheme '{scheme}' is not allowed.");
                }
            }
        }

        return result.Uri;
    }
}
