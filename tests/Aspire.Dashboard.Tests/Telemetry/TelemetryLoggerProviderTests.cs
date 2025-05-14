// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Dashboard.Tests.Telemetry;

public class TelemetryLoggerProviderTests
{
    [Fact]
    public async Task Log_DifferentCategoryAndEventIds_WriteTelemetryForBlazorUnhandedErrorAsync()
    {
        // Arrange
        var telemetrySender = new TestDashboardTelemetrySender { IsTelemetryEnabled = true };
        await telemetrySender.TryStartTelemetrySessionAsync();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<DashboardTelemetryService>()
            .AddSingleton<IDashboardTelemetrySender>(telemetrySender)
            .AddLogging()
            .AddSingleton<ILoggerProvider, TelemetryLoggerProvider>()
            .BuildServiceProvider();

        var loggerProvider = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Act & assert 1
        var testLogger = loggerProvider.CreateLogger("testLogger");
        testLogger.Log(LogLevel.Error, TelemetryLoggerProvider.CircuitUnhandledExceptionEventId, "Test message");
        Assert.False(telemetrySender.ContextChannel.Reader.TryPeek(out _));

        // Act & assert 2
        var circuitHostLogger = loggerProvider.CreateLogger(TelemetryLoggerProvider.CircuitHostLogCategory);
        circuitHostLogger.LogInformation("Test log message");
        Assert.False(telemetrySender.ContextChannel.Reader.TryPeek(out _));

        // Act & assert 3
        circuitHostLogger.Log(LogLevel.Error, TelemetryLoggerProvider.CircuitUnhandledExceptionEventId, "Test message");
        Assert.False(telemetrySender.ContextChannel.Reader.TryPeek(out _));

        // Act & assert 4
        circuitHostLogger.Log(LogLevel.Error, TelemetryLoggerProvider.CircuitUnhandledExceptionEventId, new InvalidOperationException("Exception message"), "Test message");
        Assert.True(telemetrySender.ContextChannel.Reader.TryPeek(out var context));
        Assert.Equal("/telemetry/fault - $aspire/dashboard/error", context.Name);
    }
}
