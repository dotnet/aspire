// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using Aspire.Dashboard.Telemetry;

namespace Aspire.Dashboard.Components.Pages;

public partial class Error : IComponentWithTelemetry, IDisposable
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
        TelemetryContext.Initialize(TelemetryService);
    }

    [Inject]
    public required DashboardTelemetryService TelemetryService { get; init;  }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new("Error");

    protected override void OnParametersSet()
    {
        UpdateTelemetryProperties();
    }

    public void UpdateTelemetryProperties()
    {
        TelemetryContext.UpdateTelemetryProperties([
            new ComponentTelemetryProperty(TelemetryPropertyKeys.ErrorRequestId, new AspireTelemetryProperty(RequestId ?? string.Empty)),
        ]);
    }

    public void Dispose()
    {
        TelemetryContext.Dispose();
    }
}
