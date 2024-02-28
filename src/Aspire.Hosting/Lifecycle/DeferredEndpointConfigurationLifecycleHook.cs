// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Lifecycle;

internal class DeferredEndpointConfigurationLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        foreach (var resource in appModel.Resources)
        {
            var endpointAnnotations = resource.Annotations.OfType<EndpointAnnotation>();

            var deferredAnnotations = resource.Annotations.OfType<DeferredEndpointConfigurationCallbackAnnotation>()
                .ToDictionary(a => a.EndpointName, StringComparers.EndpointAnnotationName);

            if (endpointAnnotations.Any())
            {
                foreach (var endpoint in endpointAnnotations)
                {
                    if (deferredAnnotations.Remove(endpoint.Name, out var callback))
                    {
                        callback.Callback(endpoint);
                    }
                }
            }

            foreach (var annotation in deferredAnnotations)
            {
                var newEndpoint = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: annotation.Value.EndpointName);
                annotation.Value.Callback(newEndpoint);
                resource.Annotations.Add(newEndpoint);
            }
        }

        return Task.CompletedTask;
    }
}
