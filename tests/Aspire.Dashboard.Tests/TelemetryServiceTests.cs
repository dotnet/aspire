// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class TelemetryServiceTests
{
    [Fact]
    public async Task CreateTelemetryService_WithoutValidDebugSession_ShowsTelemetryUnsupported()
    {
        var telemetryService = CreateTelemetryService(new TestDashboardOptions(new DashboardOptions()), []);
        await telemetryService.InitializeAsync();

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetryEnabled);
    }

    [Fact]
    public async Task CreateTelemetryService_WithValidDebugSession_ButTelemetryDisabled_ShowsTelemetryUnsupported()
    {
        var telemetryService = CreateTelemetryService(new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test",
                TelemetryOptOut = true
            }
        }), [new HttpResponseMessage(HttpStatusCode.NotFound)]);

        await telemetryService.InitializeAsync();

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetryEnabled);
    }

    [Theory]
    [MemberData(nameof(CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported_MemberData))]
    public async Task CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported(HttpResponseMessage? telemetryEnabledResponse, HttpResponseMessage? startTelemetryResponse, bool expectedTelemetryEnabled)
    {
        var telemetryService = CreateTelemetryService(new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test"
            }
        }), [telemetryEnabledResponse, startTelemetryResponse]);

        await telemetryService.InitializeAsync();

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.Equal(expectedTelemetryEnabled, telemetryService.IsTelemetryEnabled);
    }

    public static TheoryData<HttpResponseMessage?, HttpResponseMessage?, bool> CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported_MemberData()
    {
        return new TheoryData<HttpResponseMessage?, HttpResponseMessage?, bool>
        {
            // No result (connection refused)
            { null, null, false },
            // 404 (old version of VS/VSC)
            { new HttpResponseMessage(HttpStatusCode.NotFound), null, false},
            // 200 OK but false (telemetry not supported)
            { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new TelemetryEnabledResponse(IsEnabled: false))) }, null, false },
            // 200 OK but true (telemetry supported)
            { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new TelemetryEnabledResponse(IsEnabled: true))) }, new HttpResponseMessage(HttpStatusCode.OK), true },
        };
    }

    private static AspireTelemetryService CreateTelemetryService(TestDashboardOptions options, IEnumerable<HttpResponseMessage?> responseMessages)
    {
        return new AspireTelemetryService(
            options,
            new TestTelemetrySender(
                new Queue<HttpResponseMessage?>(responseMessages)),
            new Logger<AspireTelemetryService>(new TestLoggerFactory(new TestSink(), true)));
    }

    public class TestTelemetrySender(Queue<HttpResponseMessage?> messages) : AspireTelemetryService.ITelemetrySender
    {
        public Task<HttpResponseMessage> MakeRequestAsync(HttpClient client, Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            // If we don't care about any future response, just return OK
            if (messages.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            var message = messages.Dequeue();
            if (message is null)
            {
                throw new HttpRequestException("Simulated failed request");
            }

            return Task.FromResult(message);
        }
    }

    public class TestDashboardOptions(DashboardOptions value) : IOptions<DashboardOptions>
    {
        public DashboardOptions Value { get; } = value;
    }
}
