// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;

internal static class BuiltInDistributedApplicationEventSubscriptionHandlers
{
    public static Task InitializeDcpAnnotations(BeforeStartEvent beforeStartEvent, CancellationToken _)
    {
        // DCP names need to be calculated before any user code runs so that using IResource with
        // ResourceNotificationService and ResourceLoggerService overloads uses the right resource instance names.
        var nameGenerator = beforeStartEvent.Services.GetRequiredService<DcpNameGenerator>();

        foreach (var container in beforeStartEvent.Model.GetContainerResources())
        {
            nameGenerator.EnsureDcpInstancesPopulated(container);
        }

        foreach (var executable in beforeStartEvent.Model.GetExecutableResources())
        {
            nameGenerator.EnsureDcpInstancesPopulated(executable);
        }

        foreach (var project in beforeStartEvent.Model.GetProjectResources())
        {
            nameGenerator.EnsureDcpInstancesPopulated(project);
        }

        return Task.CompletedTask;
    }

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
