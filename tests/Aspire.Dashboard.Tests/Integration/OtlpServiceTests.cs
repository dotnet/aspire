// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.InternalTesting;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class OtlpServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OtlpServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async void CallService_OtlpEndPoint_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
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
        X509Certificate2? clientCallbackCert = null;

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"https://{app.BrowserEndPointAccessor()}", new()
        {
            HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    clientCallbackCert = cert;
                    return true;
                }
            }
        });
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.PermissionDenied, ex.StatusCode);
        Assert.NotNull(clientCallbackCert);
        Assert.Equal(TestCertificateLoader.GetTestCertificate().Thumbprint, clientCallbackCert.Thumbprint);
    }
}
