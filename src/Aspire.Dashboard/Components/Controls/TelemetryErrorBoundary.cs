// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Aspire.Dashboard.Components.Controls;

public class TelemetryErrorBoundary : ErrorBoundary
{
    [Inject]
    public required DashboardTelemetryService TelemetryService { get; init; }

    protected override Task OnErrorAsync(Exception ex)
    {
        TelemetryService.PostFault(
            TelemetryEventKeys.Error,
            $"{ex.GetType().FullName}: {ex.Message}",
            FaultSeverity.Critical,
            new Dictionary<string, AspireTelemetryProperty>
            {
                [TelemetryPropertyKeys.ExceptionType] = new AspireTelemetryProperty(ex.GetType().FullName!),
                [TelemetryPropertyKeys.ExceptionMessage] = new AspireTelemetryProperty(ex.Message),
                [TelemetryPropertyKeys.ExceptionStackTrace] = new AspireTelemetryProperty(ex.StackTrace ?? string.Empty)
            }
        );

        return Task.CompletedTask;
    }
}
