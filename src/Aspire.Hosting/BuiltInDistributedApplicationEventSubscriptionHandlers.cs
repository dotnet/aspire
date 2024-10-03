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
            AddInstance(container, nameGenerator.GetContainerName(container), 0);
            container.AddLifeCycleCommands();
        }

        foreach (var executable in beforeStartEvent.Model.GetExecutableResources())
        {
            AddInstance(executable, nameGenerator.GetExecutableName(executable), 0);
            executable.AddLifeCycleCommands();
        }

        foreach (var project in beforeStartEvent.Model.GetProjectResources())
        {
            var replicas = project.GetReplicaCount();
            for (var i = 0; i < replicas; i++)
            {
                AddInstance(project, nameGenerator.GetExecutableName(project), i);
            }
            project.AddLifeCycleCommands();
        }

        return Task.CompletedTask;

        static void AddInstance(IResource resource, (string Name, string Suffix) instanceName, int index)
        {
            if (!resource.TryGetLastAnnotation<ReplicaInstancesAnnotation>(out var replicaAnnotation))
            {
                replicaAnnotation = new ReplicaInstancesAnnotation();
                resource.Annotations.Add(replicaAnnotation);
            }

            replicaAnnotation.Instances[instanceName.Name] = new Instance(instanceName.Name, instanceName.Suffix, index);
        }
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

    public static Task ExcludeDashboardFromLifecycleCommands(BeforeStartEvent beforeStartEvent, CancellationToken _)
    {
        // The dashboard resource can be visible during development. We don't want people to be able to stop the dashboard from inside the dashboard.
        // Exclude the lifecycle commands from the dashboard resource so they're not accidently clicked during development.
        if (beforeStartEvent.Model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is { } dashboardResource)
        {
            dashboardResource.Annotations.Add(new ExcludeLifecycleCommandsAnnotation());
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
