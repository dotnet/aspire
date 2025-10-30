// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json.Nodes;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Mcp;
using Aspire.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration;

public class McpServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public McpServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CallService_McpEndPoint_Success()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.McpEndPointAccessor().EndPoint}");

        var request = CreateListToolsRequest();

        // Act
        var responseMessage = await httpClient.SendAsync(request).DefaultTimeout(TestConstants.LongTimeoutDuration);
        responseMessage.EnsureSuccessStatusCode();

        var responseData = await GetDataFromSseResponseAsync(responseMessage);

        // Assert
        var jsonResponse = JsonNode.Parse(responseData!)!;
        var tools = jsonResponse["result"]!["tools"]!.AsArray();

        Assert.NotEmpty(tools);
    }

    [Fact]
    public async Task CallService_McpEndPointDisabled_Failure()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardMcpDisableName.ConfigKey] = "true";
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.McpEndPointAccessor().EndPoint}");

        var request = CreateListToolsRequest();

        // Act
        var responseMessage = await httpClient.SendAsync(request).DefaultTimeout(TestConstants.LongTimeoutDuration);

        // Assert
        Assert.False(responseMessage.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CallService_McpEndPoint_RequiredApiKeyWrong_Failure()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardMcpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardMcpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.McpEndPointAccessor().EndPoint}");

        var requestMessage = CreateListToolsRequest();

        // Act
        var responseMessage = await httpClient.SendAsync(requestMessage).DefaultTimeout(TestConstants.LongTimeoutDuration);

        // Assert
        Assert.False(responseMessage.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CallService_McpEndPoint_RequiredApiKeySent_Success()
    {
        // Arrange
        var apiKey = "TestKey123!";
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.DashboardMcpAuthModeName.ConfigKey] = OtlpAuthMode.ApiKey.ToString();
            config[DashboardConfigNames.DashboardMcpPrimaryApiKeyName.ConfigKey] = apiKey;
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.McpEndPointAccessor().EndPoint}");

        var requestMessage = CreateListToolsRequest();
        requestMessage.Headers.TryAddWithoutValidation(McpApiKeyAuthenticationHandler.ApiKeyHeaderName, apiKey);

        // Act
        var responseMessage = await httpClient.SendAsync(requestMessage).DefaultTimeout(TestConstants.LongTimeoutDuration);
        responseMessage.EnsureSuccessStatusCode();

        var responseData = await GetDataFromSseResponseAsync(responseMessage);

        // Assert
        var jsonResponse = JsonNode.Parse(responseData!)!;
        var tools = jsonResponse["result"]!["tools"]!.AsArray();

        Assert.NotEmpty(tools);
    }

    [Fact]
    public async Task CallService_NoResourceService_ResourceToolsNotRegistered()
    {
        // Arrange - Create dashboard without configuring resource service URL
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.McpEndPointAccessor().EndPoint}");

        var request = CreateListToolsRequest();

        // Act
        var responseMessage = await httpClient.SendAsync(request).DefaultTimeout(TestConstants.LongTimeoutDuration);
        responseMessage.EnsureSuccessStatusCode();

        var responseData = await GetDataFromSseResponseAsync(responseMessage);

        // Assert
        var jsonResponse = JsonNode.Parse(responseData!)!;
        var tools = jsonResponse["result"]!["tools"]!.AsArray();

        // Verify that telemetry tools are available
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_structured_logs");
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_traces");
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_trace_structured_logs");

        // Verify that resource tools are NOT available
        Assert.DoesNotContain(tools, t => t!["name"]?.ToString() == "list_resources");
        Assert.DoesNotContain(tools, t => t!["name"]?.ToString() == "list_console_logs");
        Assert.DoesNotContain(tools, t => t!["name"]?.ToString() == "execute_resource_command");
    }

    [Fact]
    public async Task CallService_WithResourceService_ResourceToolsRegistered()
    {
        // Arrange - Create dashboard with resource service URL configured
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper, config =>
        {
            config[DashboardConfigNames.ResourceServiceUrlName.ConfigKey] = "http://localhost:5000";
            config[DashboardConfigNames.ResourceServiceClientAuthModeName.ConfigKey] = nameof(ResourceClientAuthMode.Unsecured);
        });
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.McpEndPointAccessor().EndPoint}");

        var request = CreateListToolsRequest();

        // Act
        var responseMessage = await httpClient.SendAsync(request).DefaultTimeout(TestConstants.LongTimeoutDuration);
        responseMessage.EnsureSuccessStatusCode();

        var responseData = await GetDataFromSseResponseAsync(responseMessage);

        // Assert
        var jsonResponse = JsonNode.Parse(responseData!)!;
        var tools = jsonResponse["result"]!["tools"]!.AsArray();

        // Verify that telemetry tools are available
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_structured_logs");
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_traces");
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_trace_structured_logs");

        // Verify that resource tools ARE available
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_resources");
        Assert.Contains(tools, t => t!["name"]?.ToString() == "list_console_logs");
        Assert.Contains(tools, t => t!["name"]?.ToString() == "execute_resource_command");
    }

    [Fact]
    public async Task CallService_BrowserEndPoint_Failure()
    {
        // Arrange
        await using var app = IntegrationTestHelpers.CreateDashboardWebApplication(_testOutputHelper);
        await app.StartAsync().DefaultTimeout();

        using var httpClient = IntegrationTestHelpers.CreateHttpClient($"http://{app.FrontendSingleEndPointAccessor().EndPoint}");

        var request = CreateListToolsRequest();

        // Act
        var responseMessage = await httpClient.SendAsync(request).DefaultTimeout(TestConstants.LongTimeoutDuration);

        // Assert
        Assert.False(responseMessage.IsSuccessStatusCode);
    }

    internal static HttpRequestMessage CreateListToolsRequest()
    {
        var json =
            """
            {
              "jsonrpc": "2.0",
              "id": "1",
              "method": "tools/list",
              "params": {}
            }
            """;
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        content.Headers.TryAddWithoutValidation("content-type", "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation("accept", "application/json");
        request.Headers.TryAddWithoutValidation("accept", "text/event-stream");
        return request;
    }

    internal static async Task<string?> GetDataFromSseResponseAsync(HttpResponseMessage response)
    {
        string responseText = await response.Content.ReadAsStringAsync();

        // Find the line that starts with "data:"
        var dataLine = Array.Find(responseText.Split('\n'), line => line.StartsWith("data:"));
        if (dataLine != null)
        {
            return dataLine.Substring("data:".Length).Trim();
        }

        return null;
    }
}
