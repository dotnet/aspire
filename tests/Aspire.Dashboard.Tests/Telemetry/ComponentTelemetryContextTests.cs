// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;

namespace Aspire.Dashboard.Tests.Telemetry;

public class ComponentTelemetryContextTests
{
    [Fact]
    public async Task ComponentTelemetryContext_TelemetryEnabled_EndToEnd()
    {
        var telemetryContext = new ComponentTelemetryContext(nameof(ComponentTelemetryContextTests));
        var telemetrySender = new TestDashboardTelemetrySender { IsTelemetryEnabled = true };
        var telemetryService = new DashboardTelemetryService(NullLogger<DashboardTelemetryService>.Instance, telemetrySender);

        await telemetryContext.InitializeAsync(telemetryService, new TestJSRuntime());
        for (var i = 0; i < telemetryService.GetDefaultProperties().Count; i++)
        {
            Assert.True(telemetrySender.ContextChannel.Reader.TryRead(out var postPropertyOperation));
            Assert.Equal(TelemetryEndpoints.TelemetryPostProperty, postPropertyOperation.Name);
        }

        Assert.True(telemetrySender.ContextChannel.Reader.TryRead(out var initializeOperation));
        Assert.Equal("/telemetry/userTask - $aspire/dashboard/component/initialize", initializeOperation.Name);

        Assert.Single(initializeOperation.Properties);
        Assert.Equal(2, telemetryContext.Properties.Count);

        OperationContext? parametersUpdateOperation;

        telemetryContext.UpdateTelemetryProperties([new ComponentTelemetryProperty("Test", new AspireTelemetryProperty("Value"))]);
        Assert.Equal(3, telemetryContext.Properties.Count);
        Assert.True(telemetrySender.ContextChannel.Reader.TryRead(out parametersUpdateOperation));
        Assert.Equal("/telemetry/operation - $aspire/dashboard/component/paramsSet", parametersUpdateOperation.Name);
        Assert.Single(parametersUpdateOperation.Properties);

        // If value didn't change, we shouldn't post again
        telemetryContext.UpdateTelemetryProperties([new ComponentTelemetryProperty("Test", new AspireTelemetryProperty("Value"))]);
        Assert.Equal(3, telemetryContext.Properties.Count);
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out parametersUpdateOperation));

        telemetryContext.UpdateTelemetryProperties([new ComponentTelemetryProperty("Test", new AspireTelemetryProperty("NewValue"))]);
        Assert.Equal(3, telemetryContext.Properties.Count);
        Assert.True(telemetrySender.ContextChannel.Reader.TryRead(out parametersUpdateOperation));

        telemetryContext.Dispose();
        Assert.True(telemetrySender.ContextChannel.Reader.TryRead(out var disposeOperation));
        Assert.Equal("/telemetry/operation - $aspire/dashboard/component/dispose", disposeOperation.Name);
    }

    [Fact]
    public async Task ComponentTelemetryContext_TelemetryDisabled_EndToEnd()
    {
        var telemetryContext = new ComponentTelemetryContext(nameof(ComponentTelemetryContextTests));
        var telemetrySender = new TestDashboardTelemetrySender { IsTelemetryEnabled = false };
        var telemetryService = new DashboardTelemetryService(NullLogger<DashboardTelemetryService>.Instance, telemetrySender);

        await telemetryContext.InitializeAsync(telemetryService, new TestJSRuntime());
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out _));

        telemetryContext.UpdateTelemetryProperties([new ComponentTelemetryProperty("Test", new AspireTelemetryProperty("Value"))]);
        Assert.Equal(3, telemetryContext.Properties.Count);
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out _));

        telemetryContext.Dispose();
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out _));
    }

    private sealed class TestJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return typeof(TValue) == typeof(string) ? new ValueTask<TValue>((TValue)(object)string.Empty) : new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return typeof(TValue) == typeof(string) ? new ValueTask<TValue>((TValue)(object)string.Empty) : new ValueTask<TValue>(default(TValue)!);
        }
    }
}
