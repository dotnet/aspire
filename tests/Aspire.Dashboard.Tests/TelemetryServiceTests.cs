// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model.BrowserStorage;
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
        var telemetryService = CreateTelemetryService(new TestDashboardOptions(new DashboardOptions()), new TestLocalStorage(null), []);
        await telemetryService.InitializeAsync();

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetrySupported);
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
                TelemetryEnabled = false
            }
        }), new TestLocalStorage(null), [new HttpResponseMessage(HttpStatusCode.NotFound)]);

        await telemetryService.InitializeAsync();

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.False(telemetryService.IsTelemetrySupported);
        Assert.False(telemetryService.IsTelemetryEnabled);
    }

    [Theory]
    [MemberData(nameof(CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported_MemberData))]
    public async Task CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported(HttpResponseMessage? telemetryEnabledResponse, HttpResponseMessage? startTelemetryResponse, bool? localStorageTelemetryOptOutValue, bool expectedTelemetrySupported, bool expectedTelemetryEnabled)
    {
        var telemetryService = CreateTelemetryService(new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test"
            }
        }), new TestLocalStorage(localStorageTelemetryOptOutValue), [telemetryEnabledResponse, startTelemetryResponse]);

        await telemetryService.InitializeAsync();

        Assert.True(telemetryService.IsTelemetryInitialized);
        Assert.Equal(expectedTelemetrySupported, telemetryService.IsTelemetrySupported);
        Assert.Equal(expectedTelemetryEnabled, telemetryService.IsTelemetryEnabled);
    }

    public static TheoryData<HttpResponseMessage?, HttpResponseMessage?, bool?, bool, bool> CreateTelemetryService_WithValidDebugSession_DifferentServerResponses_ShowsTelemetrySupported_MemberData()
    {
        return new TheoryData<HttpResponseMessage?, HttpResponseMessage?, bool?, bool, bool>
        {
            // No result (connection refused)
            { null, null, null, false, false },
            // 404 (old version of VS/VSC)
            { new HttpResponseMessage(HttpStatusCode.NotFound), null, null, false, false},
            // 200 OK but false (telemetry not supported)
            { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new TelemetryEnabledResponse(IsEnabled: false))) }, null, null, false, false },
            // 200 OK but true (telemetry supported, no saved telemetry opt-in value)
            { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new TelemetryEnabledResponse(IsEnabled: true))) }, new HttpResponseMessage(HttpStatusCode.OK), null, true, true },
            // 200 OK but true (telemetry supported, saved telemetry opt-in value is false)
            { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new TelemetryEnabledResponse(IsEnabled: true))) }, new HttpResponseMessage(HttpStatusCode.OK), false, true, false },
        };
    }

    private static AspireTelemetryService CreateTelemetryService(TestDashboardOptions options, TestLocalStorage localStorage, IEnumerable<HttpResponseMessage?> responseMessages)
    {
        return new AspireTelemetryService(
            options,
            localStorage,
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

    public class TestDashboardOptions(DashboardOptions value) : IOptions<Configuration.DashboardOptions>
    {
        public DashboardOptions Value { get; } = value;
    }

    public class TestLocalStorage(bool? value) : ILocalStorage
    {
        private bool? _currentValue = value;
        public Task<StorageResult<TValue>> GetAsync<TValue>(string key) => FromResult<TValue>();

        public Task SetAsync<TValue>(string key, TValue value)
        {
            _currentValue = bool.Parse(JsonSerializer.Serialize(value));
            return Task.CompletedTask;
        }

        public Task<StorageResult<TValue>> GetUnprotectedAsync<TValue>(string key) => FromResult<TValue>();

        public Task SetUnprotectedAsync<TValue>(string key, TValue value)
        {
            _currentValue = bool.Parse(JsonSerializer.Serialize(value));
            return Task.CompletedTask;
        }

        private Task<StorageResult<TValue>> FromResult<TValue>()
        {
            if (typeof(TValue) != typeof(AspireTelemetryService.TelemetrySettings))
            {
                throw new ArgumentException("Invalid type");
            }

            if (_currentValue is null)
            {
                return Task.FromResult(new StorageResult<TValue>(false, default));
            }

            return Task.FromResult(new StorageResult<TValue>(_currentValue.Value, JsonSerializer.Deserialize<TValue>(JsonSerializer.Serialize(new AspireTelemetryService.TelemetrySettings(IsEnabled: _currentValue.Value)))));
        }
    }
}
