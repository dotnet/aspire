// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class PrometheusBuilderExtensions
{
    public static IResourceBuilder<PrometheusContainerResource> AddPrometheusContainer(
        this IDistributedApplicationBuilder builder, string name, string configFilePath, string dataVolumeName)
    {
        var prometheusContainer = new PrometheusContainerResource(name, configFilePath, dataVolumeName);

        return builder.AddResource(prometheusContainer)
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, containerPort: 9090))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "prom/prometheus", Tag = "latest" })
                      .WithVolumeMount(configFilePath, "/etc/prometheus");
    }
}
