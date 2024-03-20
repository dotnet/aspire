// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class StartupTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public StartupTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task EndPointAccessors_AppStarted_EndPointPortsAssigned()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);

        // Act
        await app.StartAsync();

        // Assert
        AssertDynamicIPEndpoint(app.BrowserEndPointAccessor);
        AssertDynamicIPEndpoint(app.OtlpServiceEndPointAccessor);
    }

    [Fact]
    public async Task Configuration_BrowserAndOtlpEndpointSame_EndPointPortsAssigned()
    {
        // Arrange
        DashboardWebApplication? app = null;
        try
        {
            await ServerRetryHelper.BindPortsWithRetry(async port =>
            {
                app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper,
                    additionalConfiguration: initialData =>
                    {
                        initialData[DashboardWebApplication.DashboardUrlVariableName] = $"http://127.0.0.1:{port}";
                        initialData[DashboardWebApplication.DashboardOtlpUrlVariableName] = $"http://127.0.0.1:{port}";
                    });

                // Act
                await app.StartAsync();
            }, NullLogger.Instance);

            // Assert
            Debug.Assert(app != null);
            Assert.Equal(app.BrowserEndPointAccessor().EndPoint.Port, app.OtlpServiceEndPointAccessor().EndPoint.Port);

            // Check browser access
            using var httpClient = new HttpClient { BaseAddress = new Uri($"http://{app.BrowserEndPointAccessor().EndPoint}") };
            var request = new HttpRequestMessage(HttpMethod.Get, "/") { Version = HttpVersion.Version20, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Check OTLP service
            using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.BrowserEndPointAccessor().EndPoint}", _testOutputHelper);
            var client = new LogsService.LogsServiceClient(channel);
            var serviceResponse = await client.ExportAsync(new ExportLogsServiceRequest());
            Assert.Equal(0, serviceResponse.PartialSuccess.RejectedLogRecords);
        }
        finally
        {
            if (app is not null)
            {
                await app.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task LogOutput_DynamicPort_PortResolvedInLogs()
    {
        // Arrange
        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, testSink: testSink);

        // Act
        await app.StartAsync();

        // Assert
        var l = testSink.Writes.Where(w => w.LoggerName == typeof(DashboardWebApplication).FullName).ToList();
        Assert.Collection(l,
            w =>
            {
                Assert.Equal("Aspire version: {Version}", GetValue(w.State, "{OriginalFormat}"));
            },
            w =>
            {
                Assert.Equal("Now listening on: {DashboardUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "DashboardUri")!);
                Assert.NotEqual(0, uri.Port);
            },
            w =>
            {
                Assert.Equal("OTLP server running at: {OtlpEndpointUri}", GetValue(w.State, "{OriginalFormat}"));

                var uri = new Uri((string)GetValue(w.State, "OtlpEndpointUri")!);
                Assert.NotEqual(0, uri.Port);
            });

        object? GetValue(object? values, string key)
        {
            var list = values as IReadOnlyList<KeyValuePair<string, object>>;
            return list?.SingleOrDefault(kvp => kvp.Key == key).Value;
        }
    }

    [Fact]
    public async void EndPointAccessors_AppStarted_BrowserGet_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);

        // Act
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri($"http://{app.BrowserEndPointAccessor().EndPoint}") };

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    private static void AssertDynamicIPEndpoint(Func<EndpointInfo> endPointAccessor)
    {
        // Check that the specified dynamic port of 0 is overridden with the actual port number.
        var ipEndPoint = endPointAccessor().EndPoint;
        Assert.NotEqual(0, ipEndPoint.Port);
    }
}
