// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Telemetry;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Telemetry;

public class DashboardTelemetryServiceTests
{
    private readonly ITestOutputHelper _output;

    public DashboardTelemetryServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 0)]
    public async Task CreateTelemetryService_WithoutValidDebugSession_ShowsTelemetryUnsupported(bool enabled, int expectCount)
    {
        var sender = new TestDashboardTelemetrySender { IsTelemetryEnabled = enabled };
        var telemetryService = await CreateTelemetryServiceAsync(sender);

        var context = telemetryService.PostUserTask("testTask", TelemetryResult.Success);
        telemetryService.PostOperation("testOperation", TelemetryResult.Success, correlatedWith: context.Properties);

        await sender.DisposeAsync();

        var count = 0;
        await foreach (var item in sender.ContextChannel.Reader.ReadAllAsync())
        {
            count++;
        }

        Assert.Equal(expectCount, count);
    }

    [Fact]
    public async Task WriteTelemetry_IntegrationTest_TelemetrySent()
    {
        var testSink = new TestSink();
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.AddProvider(new TestLoggerProvider(testSink));
            b.AddXunit(_output);
        });

        var options = new TestDashboardOptions(new DashboardOptions
        {
            DebugSession = new DebugSession
            {
                Address = "http://localhost:5000",
                ServerCertificate = string.Empty,
                Token = "test"
            }
        });

        var userTaskCorrelationId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<PostOperationRequest>(TaskCreationOptions.RunContinuationsAsynchronously);

        var telemetrySender = new DashboardTelemetrySender(options, loggerFactory.CreateLogger<DashboardTelemetrySender>());
        telemetrySender.CreateHandler = handler => new TestHttpMessageHandler(
            (request, cancellationToken) =>
            {
                if (request.RequestUri!.AbsolutePath == TelemetryEndpoints.TelemetryEnabled)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new TelemetryEnabledResponse(IsEnabled: true)) ?? string.Empty)
                    });
                }
                else if (request.RequestUri!.AbsolutePath == TelemetryEndpoints.TelemetryStart)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
                else if (request.RequestUri!.AbsolutePath == TelemetryEndpoints.TelemetryPostProperty)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
                else if (request.RequestUri!.AbsolutePath == TelemetryEndpoints.TelemetryPostUserTask)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new TelemetryEventCorrelation { Id = userTaskCorrelationId }))
                    });
                }
                else if (request.RequestUri!.AbsolutePath == TelemetryEndpoints.TelemetryPostOperation)
                {
                    var requestContent = (JsonContent)request.Content!;
                    tcs.SetResult((PostOperationRequest) requestContent.Value!);

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new TelemetryEventCorrelation { Id = Guid.NewGuid() }))
                    });
                }
                else
                {
                    throw new InvalidCastException($"Unexpected path: {request.RequestUri}");
                }
            });

        var telemetryService = await CreateTelemetryServiceAsync(telemetrySender, loggerFactory: loggerFactory);
        await telemetryService.InitializeAsync();

        var context = telemetryService.PostUserTask("testTask", TelemetryResult.Success);
        telemetryService.PostOperation("testOperation", TelemetryResult.Success, correlatedWith: context.Properties);

        var operationRequest = await tcs.Task.DefaultTimeout();
        var correlatedProperty = Assert.Single(operationRequest.CorrelatedWith!);

        Assert.Equal(userTaskCorrelationId, correlatedProperty.Id);

        await telemetrySender.DisposeAsync();

        Assert.False(testSink.Writes.Any(w => w.LogLevel >= LogLevel.Warning), "Test ran without any warnings or errors logged.");
    }

    private static async Task<DashboardTelemetryService> CreateTelemetryServiceAsync(IDashboardTelemetrySender? dashboardTelemetrySender = null, ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;

        var telemetryService = new DashboardTelemetryService(loggerFactory.CreateLogger<DashboardTelemetryService>(), dashboardTelemetrySender ?? new TestDashboardTelemetrySender());
        await telemetryService.InitializeAsync();

        return telemetryService;
    }

    public class TestDashboardTelemetrySender : IDashboardTelemetrySender
    {
        public bool IsTelemetryEnabled { get; init; }
        public Channel<OperationContext> ContextChannel { get; } = Channel.CreateUnbounded<OperationContext>();

        public Task<bool> TryStartTelemetrySessionAsync()
        {
            return Task.FromResult(IsTelemetryEnabled);
        }

        public void MakeRequest(OperationContext context, Func<HttpClient, Func<OperationContextProperty, object>, Task> requestFunc)
        {
            ContextChannel.Writer.TryWrite(context);
        }

        public ValueTask DisposeAsync()
        {
            ContextChannel.Writer.Complete();
            return ValueTask.CompletedTask;
        }
    }

    public class TestDashboardOptions(DashboardOptions value) : IOptions<DashboardOptions>
    {
        public DashboardOptions Value { get; } = value;
    }
}
