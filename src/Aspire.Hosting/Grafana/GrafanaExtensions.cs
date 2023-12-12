// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Grafana;

public static class GrafanaBuilderExtensions
{
    public static IResourceBuilder<GrafanaContainerResource> AddGrafanaContainer(
        this IDistributedApplicationBuilder builder, string name, string configFilePath, string dashboardFilePath , string dataVolumeName)
    {
        var grafanaContainer = new GrafanaContainerResource(name, configFilePath, dashboardFilePath, dataVolumeName);

        return builder.AddResource(grafanaContainer)
                      .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, containerPort: 3000))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "grafana/grafana", Tag = "latest" })
                      .WithVolumeMount(configFilePath, "/etc/grafana")
                      .WithVolumeMount(dashboardFilePath, "/var/lib/grafana/dashboards");
    }
}
