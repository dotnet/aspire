// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Grafana;

public class GrafanaContainerResource : ContainerResource
{
    public string ConfigFilePath { get; init; }
    public string DataVolumeName { get; init; }
    public string DashboardFilePath { get; init; }

    public GrafanaContainerResource(string name, string configFilePath, string dashboardFilePath, string dataVolumeName) : base(name)
    {
        ConfigFilePath = configFilePath;
        DashboardFilePath = dashboardFilePath;
        DataVolumeName = dataVolumeName;
    }
}
