// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Http;

/// <summary>
/// <see cref="HttpClientHandler"/> which resolves endpoints using service discovery.
/// </summary>
internal sealed class ResolvingHttpClientHandler(HttpServiceEndpointResolver resolver, IOptions<ServiceDiscoveryOptions> options) : HttpClientHandler
{
    private readonly HttpServiceEndpointResolver _resolver = resolver;
    private readonly ServiceDiscoveryOptions _options = options.Value;

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var originalUri = request.RequestUri;

        try
        {
            if (originalUri?.Host is not null)
            {
                var result = await _resolver.GetEndpointAsync(request, cancellationToken).ConfigureAwait(false);
                request.RequestUri = ResolvingHttpDelegatingHandler.GetUriWithEndpoint(originalUri, result, _options);
                request.Headers.Host ??= result.Features.Get<IHostNameFeature>()?.HostName;
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            request.RequestUri = originalUri;
        }
    }
}
