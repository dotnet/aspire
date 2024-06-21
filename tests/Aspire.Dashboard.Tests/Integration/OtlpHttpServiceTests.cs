// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Otlp.Http;
using Aspire.Dashboard.Tests.TelemetryRepositoryTests;
using Aspire.Hosting;
using Google.Protobuf;
using Microsoft.AspNetCore.InternalTesting;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Logs.V1;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Dashboard.Tests.Integration;

public class OtlpHttpServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OtlpHttpServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_BigData_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var request = CreateExportLogsServiceRequest(logRecordsCount: 10000);

        var content = new ByteArrayContent(request.ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        // Act
        var responseMessage = await httpClient.PostAsync("/v1/logs", content);
        responseMessage.EnsureSuccessStatusCode();

        var response = ExportLogsServiceResponse.Parser.ParseFrom(await responseMessage.Content.ReadAsByteArrayAsync());

        // Assert
        Assert.Equal(OtlpHttpEndpointsBuilder.ProtobufContentType, responseMessage.Content.Headers.GetValues("content-type").Single());
        Assert.False(responseMessage.Headers.Contains("content-security-policy"));
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_ExceedRequestLimit_Failure()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var request = CreateExportLogsServiceRequest(logRecordsCount: 100000);

        var content = new ByteArrayContent(request.ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        // Act
        var responseMessage = await httpClient.PostAsync("/v1/logs", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, responseMessage.StatusCode);
    }

    private static ExportLogsServiceRequest CreateExportLogsServiceRequest(int logRecordsCount)
    {
        var scopeLogs = new ScopeLogs
        {
            Scope = TestHelpers.CreateScope("TestLogger")
        };
        for (var i = 0; i < logRecordsCount; i++)
        {
            scopeLogs.LogRecords.Add(TestHelpers.CreateLogRecord(message: $"This is the test log message {i}. The quick brown fox jumped over the lazy dog. Peter Pipper picked a patch of pickled peppers."));
        }

        var request = new ExportLogsServiceRequest();
        request.ResourceLogs.Add(new ResourceLogs
        {
            Resource = TestHelpers.CreateResource(),
            ScopeLogs = { scopeLogs }
        });
        return request;
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_RequiredApiKeyMissing_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var content = new ByteArrayContent(new ExportLogsServiceRequest().ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        // Act
        var responseMessage = await httpClient.PostAsync("/v1/logs", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_RequiredApiKeyWrong_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var content = new ByteArrayContent(new ExportLogsServiceRequest().ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/logs");
        requestMessage.Content = content;
        requestMessage.Headers.TryAddWithoutValidation(OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName, "WRONG");

        // Act
        var responseMessage = await httpClient.SendAsync(requestMessage);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
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

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.OtlpServiceHttpEndPointAccessor().EndPoint}");

        var content = new ByteArrayContent(new ExportLogsServiceRequest().ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/logs");
        requestMessage.Content = content;
        requestMessage.Headers.TryAddWithoutValidation(OtlpApiKeyAuthenticationHandler.ApiKeyHeaderName, apiKey);

        // Act
        var responseMessage = await httpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        var response = ExportLogsServiceResponse.Parser.ParseFrom(await responseMessage.Content.ReadAsByteArrayAsync());

        // Assert
        Assert.Equal(OtlpHttpEndpointsBuilder.ProtobufContentType, responseMessage.Content.Headers.GetValues("content-type").Single());
        Assert.False(responseMessage.Headers.Contains("content-security-policy"));
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
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

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"https://{app.FrontendEndPointAccessor().EndPoint}",
            validationCallback: cert =>
            {
                clientCallbackCert = cert;
            });

        var content = new ByteArrayContent(new ExportLogsServiceRequest().ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        // Act
        var responseMessage = await httpClient.PostAsync("/v1/logs", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
        Assert.NotNull(clientCallbackCert);
        Assert.Equal(TestCertificateLoader.GetTestCertificate().Thumbprint, clientCallbackCert.Thumbprint);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData(null)]
    public async Task CallService_OtlpHttpEndPoint_UnsupportedContentType_Failure(string? contentType)
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
        if (contentType != null)
        {
            content.Headers.TryAddWithoutValidation("content-type", contentType);
        }

        // Act
        var responseMessage = await client.PostAsync("/v1/logs", content);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, responseMessage.StatusCode);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task CallService_OtlpHttpEndPoint_UnsupportedMethods_Failure(string method)
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var content = new ByteArrayContent(new ExportLogsServiceRequest().ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);
        var requestMessage = new HttpRequestMessage(new HttpMethod(method), "/v1/logs");
        requestMessage.Content = content;

        // Act
        var responseMessage = await client.SendAsync(requestMessage);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, responseMessage.StatusCode);
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_Logs_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var request = new ExportLogsServiceRequest();
        using var content = new ByteArrayContent(request.ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        var responseMessage = await client.PostAsync("/v1/logs", content);
        responseMessage.EnsureSuccessStatusCode();

        // Act
        var response = ExportLogsServiceResponse.Parser.ParseFrom(await responseMessage.Content.ReadAsByteArrayAsync());

        // Assert
        Assert.Equal(OtlpHttpEndpointsBuilder.ProtobufContentType, responseMessage.Content.Headers.GetValues("content-type").Single());
        Assert.False(responseMessage.Headers.Contains("content-security-policy"));
        Assert.Equal(0, response.PartialSuccess.RejectedLogRecords);
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_Traces_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var request = new ExportTraceServiceRequest();
        using var content = new ByteArrayContent(request.ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        var responseMessage = await client.PostAsync("/v1/traces", content);
        responseMessage.EnsureSuccessStatusCode();

        // Act
        var response = ExportTraceServiceResponse.Parser.ParseFrom(await responseMessage.Content.ReadAsByteArrayAsync());

        // Assert
        Assert.Equal(OtlpHttpEndpointsBuilder.ProtobufContentType, responseMessage.Content.Headers.GetValues("content-type").Single());
        Assert.False(responseMessage.Headers.Contains("content-security-policy"));
        Assert.Equal(0, response.PartialSuccess.RejectedSpans);
    }

    [Fact]
    public async Task CallService_OtlpHttpEndPoint_Metrics_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, dictionary =>
        {
            dictionary[DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0";
        });
        await app.StartAsync();

        var endpoint = app.OtlpServiceHttpEndPointAccessor();
        using var client = new HttpClient { BaseAddress = new Uri($"http://{endpoint.EndPoint}") };

        var request = new ExportMetricsServiceRequest();
        using var content = new ByteArrayContent(request.ToByteArray());
        content.Headers.TryAddWithoutValidation("content-type", OtlpHttpEndpointsBuilder.ProtobufContentType);

        var responseMessage = await client.PostAsync("/v1/metrics", content);
        responseMessage.EnsureSuccessStatusCode();

        // Act
        var response = ExportMetricsServiceResponse.Parser.ParseFrom(await responseMessage.Content.ReadAsByteArrayAsync());

        // Assert
        Assert.Equal(OtlpHttpEndpointsBuilder.ProtobufContentType, responseMessage.Content.Headers.GetValues("content-type").Single());
        Assert.False(responseMessage.Headers.Contains("content-security-policy"));
        Assert.Equal(0, response.PartialSuccess.RejectedDataPoints);
    }
}
