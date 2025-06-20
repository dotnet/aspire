// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal sealed class ExternalServiceParameterResource : ParameterResource, IResourceWithServiceDiscovery
{
    public ExternalServiceParameterResource(string name, Func<ParameterDefault?, string> callback) : base(name, callback, secret: false)
    {
        // Add endpoint annotation for service discovery
        var endpointAnnotation = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, "http", name: "default");
        endpointAnnotation.AllocatedEndpoint = new ExternalServiceAllocatedEndpoint(endpointAnnotation, UrlExpression);
        Annotations.Add(endpointAnnotation);
    }

    public ReferenceExpression UrlExpression =>
        ReferenceExpression.Create($"{this}");
}