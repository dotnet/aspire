// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Pages;
using Aspire.Dashboard.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Dashboard.Tests.Telemetry;

public class ComponentTelemetryContextTests
{
    [Fact]
    public async Task ComponentTelemetryContext_TelemetryEnabled_EndToEnd()
    {
        // Arrange
        var telemetryContext = new ComponentTelemetryContext(nameof(ComponentTelemetryContextTests));
        var telemetrySender = new TestDashboardTelemetrySender { IsTelemetryEnabled = true };
        var telemetryService = new DashboardTelemetryService(NullLogger<DashboardTelemetryService>.Instance, telemetrySender);
        var telemetryContextProvider = new ComponentTelemetryContextProvider(telemetryService);
        telemetryContextProvider.SetBrowserUserAgent("mozilla");
        await telemetryService.InitializeAsync();

        // Act & assert initialize
        telemetryContextProvider.Initialize(telemetryContext);
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

        // Act & assert update properties
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

        // Act & assert dispose
        telemetryContext.Dispose();
        Assert.True(telemetrySender.ContextChannel.Reader.TryRead(out var disposeOperation));
        Assert.Equal("/telemetry/operation - $aspire/dashboard/component/dispose", disposeOperation.Name);
    }

    [Fact]
    public async Task ComponentTelemetryContext_TelemetryDisabled_EndToEnd()
    {
        // Arrange
        var telemetryContext = new ComponentTelemetryContext(nameof(ComponentTelemetryContextTests));
        var telemetrySender = new TestDashboardTelemetrySender { IsTelemetryEnabled = false };
        var telemetryService = new DashboardTelemetryService(NullLogger<DashboardTelemetryService>.Instance, telemetrySender);
        var telemetryContextProvider = new ComponentTelemetryContextProvider(telemetryService);
        telemetryContextProvider.SetBrowserUserAgent("mozilla");
        await telemetryService.InitializeAsync();

        // Act & assert initialize
        telemetryContextProvider.Initialize(telemetryContext);
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out _));

        // Act & assert update properties
        telemetryContext.UpdateTelemetryProperties([new ComponentTelemetryProperty("Test", new AspireTelemetryProperty("Value"))]);
        Assert.Equal(3, telemetryContext.Properties.Count);
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out _));

        // Act & assert dispose
        telemetryContext.Dispose();
        Assert.False(telemetrySender.ContextChannel.Reader.TryRead(out _));
    }

    [Fact]
    public void ComponentTelemetryContext_DisposeWithoutInitialize_NoThrow()
    {
        // Arrange
        var telemetryContext = new ComponentTelemetryContext(nameof(ComponentTelemetryContextTests));

        // Act
        telemetryContext.Dispose();
    }
}
