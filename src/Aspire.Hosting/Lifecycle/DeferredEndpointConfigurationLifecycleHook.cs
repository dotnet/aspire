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

            if (endpointAnnotations.Any())
            {
                var deferredAnnotations = resource.Annotations.OfType<DeferredEndpointConfigurationCallbackAnnotation>()
                    .ToDictionary(a => a.EndpointName, StringComparers.EndpointAnnotationName);

                foreach (var endpoint in endpointAnnotations)
                {
                    if (deferredAnnotations.TryGetValue(endpoint.Name, out var deferredAnnotation))
                    {
                        deferredAnnotation.Callback(endpoint);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
