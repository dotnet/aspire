// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class PrometheusBuilderExtensions
{
    /// <summary>
    /// AddPrometheusContainer adds a Prometheus container to the application.
    /// </summary>
    /// <param name="builder">
    /// <see cref="IDistributedApplicationBuilder"/>
    /// </param>
    /// <param name="name">
    /// the name of the container
    /// </param>
    /// <param name="configFilePath">
    /// the path to the Prometheus configuration file. The directory should contain prometheus.yml
    /// </param>
    /// <param name="dataVolumeName">
    /// the name of the data volume to use for Prometheus data
    /// </param>
    /// <returns>
    /// <see cref="IResourceBuilder{PrometheusContainerResource}"/>
    /// </returns>
    public static IResourceBuilder<PrometheusContainerResource> AddPrometheusContainer(
        this IDistributedApplicationBuilder builder, string name, string configFilePath, string dataVolumeName)
    {
        // Define the resource
        var prometheusContainer = new PrometheusContainerResource(name, configFilePath, dataVolumeName);

        // Add the resource to the application
        return builder.AddResource(prometheusContainer)
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, containerPort: 9090))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "prom/prometheus", Tag = "latest" })
                      .WithVolumeMount(configFilePath, "/etc/prometheus");
    }
}
