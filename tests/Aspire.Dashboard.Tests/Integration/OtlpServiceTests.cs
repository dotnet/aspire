// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Configuration;
using Aspire.Hosting;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
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
    public async Task CallService_OtlpGrpcEndPoint_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var response = client.ExportAsync(new ExportLogsServiceRequest());
        var message = await response.ResponseAsync;
        var headers = await response.ResponseHeadersAsync;

        // Assert
        Assert.Null(headers.GetValue("content-security-policy"));
        Assert.Equal(0, message.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async Task CallService_OtlpGrpcEndPoint_RequiredApiKeyMissing_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task CallService_OtlpGrpcEndPoint_RequiredApiKeyWrong_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
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
    public async Task CallService_OtlpGrpcEndPoint_RequiredApiKeySent_Success()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
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
    public async Task CallService_OtlpGrpcEndPoint_RequiredSecondaryApiKeySent_Success()
    {
        // Arrange
        var apiKey = "TestKey123!";
        var secondaryApiKey = "!321yeKtseT";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.ConfigKey] = apiKey;
            config[DashboardConfigNames.DashboardOtlpSecondaryApiKeyName.ConfigKey] = secondaryApiKey;
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
        var client = new LogsService.LogsServiceClient(channel);

        var metadata = new Metadata
        {
            { OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName, secondaryApiKey }
        };

        // Act
        var response = await client.ExportAsync(new ExportLogsServiceRequest(), metadata);

        // Assert
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async Task CallService_OtlpGrpcEndPoint_ExternalFile_FileChanged_UseConfiguredKey()
    {
        // Arrange
        var apiKey = "TestKey123!";
        var configPath = Path.GetTempFileName();
        var configJson = new JsonObject
        {
            ["Dashboard"] = new JsonObject
            {
                ["Otlp"] = new JsonObject
                {
                    ["AuthMode"] = OtlpAuthMode.ApiKey.ToString(),
                    ["PrimaryApiKey"] = apiKey
                }
            }
        };
        File.WriteAllText(configPath, configJson.ToString());

        var testSink = new TestSink();
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardConfigFilePathName.ConfigKey] = configPath;
        }, testSink: testSink);
        await app.StartAsync();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var monitorRegistration = app.DashboardOptionsMonitor.OnChange((o, n) =>
        {
            tcs.TrySetResult();
        });

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"http://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
        var client = new LogsService.LogsServiceClient(channel);

        var metadata = new Metadata
        {
            { OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName, apiKey }
        };

        // Act 1
        var response1 = await client.ExportAsync(new ExportLogsServiceRequest(), metadata);

        // Assert 1
        Assert.Equal(0, response1.PartialSuccess.RejectedLogRecords);

        // Change config file
        configJson = new JsonObject
        {
            ["Dashboard"] = new JsonObject
            {
                ["Otlp"] = new JsonObject
                {
                    ["AuthMode"] = OtlpAuthMode.ApiKey.ToString(),
                    ["PrimaryApiKey"] = "Different"
                }
            }
        };
        File.WriteAllText(configPath, configJson.ToString());

        await tcs.Task;

        // Act 2
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest(), metadata).ResponseAsync);

        // Assert 2
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task CallService_BrowserEndPoint_Failure()
    {
        // Arrange
        X509Certificate2? clientCallbackCert = null;

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            // Change dashboard to HTTPS so the caller can negotiate a HTTP/2 connection.
            config[DashboardConfigNames.DashboardFrontendUrlName.ConfigKey] = "https://127.0.0.1:0";
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel(
            $"https://{app.FrontendEndPointAccessor().EndPoint}",
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
    public async Task CallService_OtlpEndpoint_RequiredClientCertificateMissing_Failure()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            // Change dashboard to HTTPS so the caller can negotiate a HTTP/2 connection.
            config[DashboardConfigNames.DashboardOtlpGrpcUrlName.ConfigKey] = "https://127.0.0.1:0";

            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ClientCertificate.ToString();
        });
        await app.StartAsync();

        using var channel = IntegrationTestHelpers.CreateGrpcChannel($"https://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}", _testOutputHelper);
        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var ex = await Assert.ThrowsAsync<RpcException>(() => client.ExportAsync(new ExportLogsServiceRequest()).ResponseAsync);

        // Assert
        // StatusCode can change depending upon order of execution inside HttpClient.
        Assert.True(ex.StatusCode is StatusCode.Unavailable or StatusCode.Internal, "gRPC call fails without cert.");
    }

    [Fact]
    public async Task CallService_OtlpEndpoint_RequiredClientCertificateValid_Success()
    {
        // Arrange
        X509Certificate2? clientCallbackCert = null;

        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            // Change dashboard to HTTPS so the caller can negotiate a HTTP/2 connection.
            config[DashboardConfigNames.DashboardOtlpGrpcUrlName.ConfigKey] = "https://127.0.0.1:0";

            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ClientCertificate.ToString();

            config["Dashboard:Otlp:CertificateAuthOptions:AllowedCertificateTypes"] = "SelfSigned";
            config["Dashboard:Otlp:CertificateAuthOptions:ValidateValidityPeriod"] = "false";
        });
        await app.StartAsync();

        var clientCertificates = new X509CertificateCollection(new[] { TestCertificateLoader.GetTestCertificate("eku.client.pfx") });
        using var channel = IntegrationTestHelpers.CreateGrpcChannel(
            $"https://{app.OtlpServiceGrpcEndPointAccessor().EndPoint}",
            _testOutputHelper,
            validationCallback: cert =>
            {
                clientCallbackCert = cert;
            },
            clientCertificates: clientCertificates);

        var client = new LogsService.LogsServiceClient(channel);

        // Act
        var response = await client.ExportAsync(new ExportLogsServiceRequest());

        // Assert
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_Logs_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:4318";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var request = new ExportLogsServiceRequest();
        using var content = new ByteArrayContent(request.ToByteArray());
        var response = await client.PostAsync("/v1/logs", content);

        // Act
        var message = await response.Content.ReadFromJsonAsync<ExportLogsServiceResponse>();

        // Assert
        Assert.Equal(0, message!.PartialSuccess.RejectedLogRecords);
        Assert.True(string.IsNullOrWhiteSpace(message.PartialSuccess.ErrorMessage), "error message should be empty");
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_Traces_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:4318";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var request = new ExportTraceServiceRequest();
        using var content = new ByteArrayContent(request.ToByteArray());
        var response = await client.PostAsync("/v1/traces", content);

        // Act
        var message = await response.Content.ReadFromJsonAsync<ExportTraceServiceResponse>();

        // Assert
        Assert.Equal(0, message!.PartialSuccess.RejectedSpans);
        Assert.True(string.IsNullOrWhiteSpace(message.PartialSuccess.ErrorMessage), "error message should be empty");
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_Metrics_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:4318";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var request = new ExportMetricsServiceRequest();
        using var content = new ByteArrayContent(request.ToByteArray());
        var response = await client.PostAsync("/v1/metrics", content);

        // Act
        var message = await response.Content.ReadFromJsonAsync<ExportMetricsServiceResponse>();

        // Assert
        Assert.Equal(0, message!.PartialSuccess.RejectedDataPoints);
        Assert.True(string.IsNullOrWhiteSpace(message.PartialSuccess.ErrorMessage), "error message should be empty");
    }
}
