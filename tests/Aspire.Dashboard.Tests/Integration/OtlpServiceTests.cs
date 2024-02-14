// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration;

public class OtlpServiceTests
{
    [Fact]
    public async void CallService_OtlpEndPoint_Success()
    {
        // Arrange
        var configBuilder = new ConfigurationManager()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_URLS"] = "http://127.0.0.1:0",
                ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://127.0.0.1:0"
            });

        await using var app = new DashboardWebApplication(configBuilder.Build());
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"http://{app.OtlpServiceEndPointAccessor()}");
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var response = await client.ExportAsync(new ExportLogsServiceRequest());

        // Assert
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async void CallService_BrowserEndPoint_Failure()
    {
        // Arrange
        var configBuilder = new ConfigurationManager()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_URLS"] = "https://127.0.0.1:0",
                ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://127.0.0.1:0"
            });

        await using var app = new DashboardWebApplication(configBuilder.Build());
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"https://{app.BrowserEndPointAccessor()}", new()
        {
            HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }
        });
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.PermissionDenied, ex.StatusCode);
    }
}
