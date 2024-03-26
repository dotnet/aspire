// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Authentication.OtlpApiKey;
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

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceEndPointAccessor().EndPoint}", _testOutputHelper);
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var response = await client.ExportAsync(new ExportLogsServiceRequest());

        // Assert
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async void CallService_OtlpEndPoint_RequiredApiKeyMissing_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardWebApplication.OtlpAuthModeKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardWebApplication.OtlpApiKeyKey] = apiKey;
        });
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"http://{app.OtlpServiceEndPointAccessor().EndPoint}");
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async void CallService_OtlpEndPoint_RequiredApiKeyWrong_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardWebApplication.OtlpAuthModeKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardWebApplication.OtlpApiKeyKey] = apiKey;
        });
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"http://{app.OtlpServiceEndPointAccessor().EndPoint}");
        var client = new LogsService.LogsServiceClient(channel);

        var metadata = new Metadata
        {
            { OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName, "WRONG" }
        };

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest(), metadata).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async void CallService_OtlpEndPoint_RequiredApiKeySent_Success()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardWebApplication.OtlpAuthModeKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardWebApplication.OtlpApiKeyKey] = apiKey;
        });
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"http://{app.OtlpServiceEndPointAccessor().EndPoint}");
        var client = new LogsService.LogsServiceClient(channel);

        var metadata = new Metadata
        {
            { OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName, apiKey }
        };

        // Act
        var response = await client.ExportAsync(new ExportLogsServiceRequest(), metadata);

        // Assert
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async void CallService_BrowserEndPoint_Failure()
    {
        // Arrange
        X509Certificate2? clientCallbackCert = null;

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            // Change dashboard to HTTPS so the caller can negotiate a HTTP/2 connection.
            config[DashboardWebApplication.DashboardUrlVariableName] = "https://127.0.0.1:0";
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel(
            $"https://{app.BrowserEndPointAccessor().EndPoint}",
            _testOutputHelper,
            validationCallback: cert =>
            {
                clientCallbackCert = cert;
            });
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
        Assert.NotNull(clientCallbackCert);
        Assert.Equal(TestCertificateLoader.GetTestCertificate().Thumbprint, clientCallbackCert.Thumbprint);
    }

    [Fact]
    public async void CallService_OtlpEndpoint_RequiredClientCertificateMissing_Failure()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            // Change dashboard to HTTPS so the caller can negotiate a HTTP/2 connection.
            config[DashboardWebApplication.DashboardOtlpUrlVariableName] = "https://127.0.0.1:0";

            config[DashboardWebApplication.OtlpAuthModeKey] = OtlpAuthMode.ClientCertificate.ToString();
        });
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"https://{app.OtlpServiceEndPointAccessor().EndPoint}", new()
        {
            HttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    return true;
                }
            }
        });
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unavailable, ex.StatusCode);
    }

    [Fact]
    public async void CallService_OtlpEndpoint_RequiredClientCertificateValid_Success()
    {
        // Arrange
        X509Certificate2? clientCallbackCert = null;

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            // Change dashboard to HTTPS so the caller can negotiate a HTTP/2 connection.
            config[DashboardWebApplication.DashboardOtlpUrlVariableName] = "https://127.0.0.1:0";

            config[DashboardWebApplication.OtlpAuthModeKey] = OtlpAuthMode.ClientCertificate.ToString();

            config["CertificateAuthentication:AllowedCertificateTypes"] = "SelfSigned";
            config["CertificateAuthentication:ValidateValidityPeriod"] = "false";
        });
        await app.StartAsync();

        using var channel = GrpcChannel.ForAddress($"https://{app.OtlpServiceEndPointAccessor().EndPoint}", new()
        {
            HttpHandler = new SocketsHttpHandler()
            {
                SslOptions =
                {
                    RemoteCertificateValidationCallback = (message, cert, chain, errors) =>
                    {
                        clientCallbackCert = (X509Certificate2)cert!;
                        return true;
                    },
                    ClientCertificates = new X509CertificateCollection(new [] { TestCertificateLoader.GetTestCertificate("eku.client.pfx") })
                }
            }
        });
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var response = await client.ExportAsync(new ExportLogsServiceRequest());

        // Assert
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }
}
