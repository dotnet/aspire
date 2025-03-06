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
        var telemetryService = await CreateTelemetryServiceAsync(new TestDashboardOptions(new DashboardOptions()));

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetryEnabled);
    }

    [Fact]
    public async Task CreateTelemetryService_WithValidDebugSession_ButTelemetryDisabled_ShowsTelemetryUnsupported()
    {
        var telemetryService = await CreateTelemetryServiceAsync(new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test",
                TelemetryOptOut = true
            }
        }), new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetryEnabled);
    }

    [Fact]
    public async Task CreateTelemetryService_NoSender_ReturnsCorrectValuesAndDoesNotThrow()
    {
        var telemetryService = await CreateTelemetryServiceAsync(new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test",
                TelemetryOptOut = true
            }
        }), initializeWithSender: false);

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetryEnabled);

        Assert.Equal(Guid.Empty, telemetryService.PostFault(string.Empty, string.Empty, FaultSeverity.Crash));
    }

    [Theory]
    [MemberData(nameof(CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported_MemberData))]
    public async Task CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported(HttpResponseMessage? telemetryEnabledResponse, HttpResponseMessage? startTelemetryResponse, bool expectedTelemetryEnabled)
    {
        var telemetryService = await CreateTelemetryServiceAsync(new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test"
            }
        }), telemetryEnabledResponse, startTelemetryResponse);

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

    private static async Task<DashboardTelemetryService> CreateTelemetryServiceAsync(TestDashboardOptions options, HttpResponseMessage? telemetryEnabledResponse = null, HttpResponseMessage? startTelemetrySessionResponse = null, bool initializeWithSender = true)
    {
        var telemetryService = new DashboardTelemetryService(options, new Logger<DashboardTelemetryService>(new TestLoggerFactory(new TestSink(), true)));
        await telemetryService.InitializeAsync(initializeWithSender ? new TestDashboardTelemetrySender(telemetryEnabledResponse, startTelemetrySessionResponse) : null);

        return telemetryService;
    }

    public class TestDashboardTelemetrySender(HttpResponseMessage? telemetryEnabledResponse, HttpResponseMessage? startTelemetrySessionResponse) : IDashboardTelemetrySender
    {
        public Task<HttpResponseMessage> GetTelemetryEnabledAsync()
        {
            if (telemetryEnabledResponse is null)
            {
                throw new HttpRequestException("No response provided");
            }

            return Task.FromResult(telemetryEnabledResponse);
        }

        public Task<HttpResponseMessage> StartTelemetrySessionAsync()
        {
            if (startTelemetrySessionResponse is null)
            {
                throw new HttpRequestException("No response provided");
            }

            return Task.FromResult(startTelemetrySessionResponse);
        }

        public List<Guid> MakeRequest(int generatedGuids, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>> requestFunc)
        {
            return [];
        }

        public void Dispose()
        {
        }
    }

    public class TestDashboardOptions(DashboardOptions value) : IOptions<DashboardOptions>
    {
        public DashboardOptions Value { get; } = value;
    }
}
