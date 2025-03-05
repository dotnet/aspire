// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public static class TelemetryEventKeys
{
    private const string AspireDashboardEventPrefix = "aspire/dashboard/";

    public const string InitializeComponent = AspireDashboardEventPrefix + "component/initialize";
    public const string RenderComponent = AspireDashboardEventPrefix + "component/render";
}
