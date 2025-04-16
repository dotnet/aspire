// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Pages;

public partial class NotFound : IComponentWithTelemetry, IDisposable
{
    [Inject]
    public required DashboardTelemetryService TelemetryService { get; init;  }

    // IComponentWithTelemetry impl
    public ComponentTelemetryContext TelemetryContext { get; } = new("NotFound");

    protected override async Task OnInitializedAsync()
    {
        await TelemetryService.InitializeAsync();
    }

    public void Dispose()
    {
        TelemetryContext.Dispose();
    }
}
