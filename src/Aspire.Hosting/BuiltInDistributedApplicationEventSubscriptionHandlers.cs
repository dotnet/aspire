// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

internal static class BuiltInDistributedApplicationEventSubscriptionHandlers
{
    public static Task ExcludeDashboardFromManifestAsync(BeforeStartEvent beforeStartEvent, CancellationToken _)
    {
        // When developing locally, exclude the dashboard from the manifest. This only affects our playground projects in practice.
        if (beforeStartEvent.Model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is { } dashboardResource)
        {
            dashboardResource.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);
        }

        return Task.CompletedTask;
    }

    public static Task MutateHttp2TransportAsync(BeforeStartEvent beforeStartEvent, CancellationToken _)
    {
        foreach (var resource in beforeStartEvent.Model.Resources)
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

    public static Task UpdateContainerRegistryAsync(BeforeStartEvent @event, DistributedApplicationOptions options)
    {
        var resourcesWithContainerImages = @event.Model.Resources.SelectMany(
            r => r.Annotations.OfType<ContainerImageAnnotation>()
                              .Select(cia => new { Resource = r, Annotation = cia })
            );

        foreach (var resourceWithContainerImage in resourcesWithContainerImages)
        {
            resourceWithContainerImage.Annotation.Registry = options.ContainerRegistryOverride;
        }

        return Task.CompletedTask;
    }
}
