// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Publishing;

internal sealed class Http2TransportMutationHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        foreach (var resource in appModel.Resources)
        {
            var isHttp2Service = resource.Annotations.OfType<Http2ServiceAnnotation>().Any();
            var httpEndpoints = resource.Annotations.OfType<EndpointAnnotation>().Where(sb => sb.UriScheme == "http" || sb.UriScheme == "https");
            foreach (var httpEndpoint in httpEndpoints)
            {
                httpEndpoint.Transport = isHttp2Service ? "http2" : httpEndpoint.Transport;
            }
        }

        return Task.CompletedTask;
    }
}
