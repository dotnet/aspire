// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Nats;

public class NatsContainerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    public string GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var endpoints))
        {
            throw new DistributedApplicationException("NATS resource does not have endpoint annotation.");
        }

        return string.Join(",", endpoints.Select(e => e.EndPointString));
    }
}
