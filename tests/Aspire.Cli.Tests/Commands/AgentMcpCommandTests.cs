// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Mcp;
using Aspire.Cli.Tests.Mcp;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Tests.Commands;

/// <summary>
/// In-process unit tests for AgentMcpCommand that test the MCP server functionality
/// without starting a new CLI process. The IO communication between the MCP server
/// and test client is abstracted using in-memory pipes via DI.
/// </summary>
public class AgentMcpCommandTests(ITestOutputHelper outputHelper) : IAsyncLifetime
{
    private TemporaryWorkspace _workspace = null!;
    private ServiceProvider _serviceProvider = null!;
    private TestMcpServerTransport _testTransport = null!;
    private McpClient _mcpClient = null!;
    private Task _serverRunTask = null!;
    private CancellationTokenSource _cts = null!;
    private ILoggerFactory _loggerFactory = null!;

    public async ValueTask InitializeAsync()
    {
        _cts = new CancellationTokenSource();
        _workspace = TemporaryWorkspace.Create(outputHelper);

        // Create the test transport with in-memory pipes
        _loggerFactory = LoggerFactory.Create(builder => builder.AddXunit(outputHelper));
        _testTransport = new TestMcpServerTransport(_loggerFactory);

        // Create services using CliTestHelper with custom MCP transport and test docs service
        var services = CliTestHelper.CreateServiceCollection(_workspace, outputHelper, options =>
        {
            // Override the MCP transport with our test transport
            options.McpServerTransportFactory = _ => _testTransport.ServerTransport;
            // Override the docs index service with a test implementation that doesn't make network calls
            options.DocsIndexServiceFactory = _ => new TestDocsIndexService();
        });

        _serviceProvider = services.BuildServiceProvider();

        // Get the AgentMcpCommand from DI and start the server
        var agentMcpCommand = _serviceProvider.GetRequiredService<AgentMcpCommand>();
        var rootCommand = _serviceProvider.GetRequiredService<RootCommand>();
        var parseResult = rootCommand.Parse("agent mcp");

        // Start the MCP server in the background
        _serverRunTask = Task.Run(async () =>
        {
            try
            {
                await agentMcpCommand.ExecuteCommandAsync(parseResult, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }, _cts.Token);

        // Wait a brief moment for the server to start
        await Task.Delay(100, _cts.Token);

        // Create and connect the MCP client using the test transport's client side
        _mcpClient = await _testTransport.CreateClientAsync(_loggerFactory, _cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_mcpClient is not null)
        {
            await _mcpClient.DisposeAsync();
        }

        await _cts.CancelAsync();

        try
        {
            if (_serverRunTask is not null)
            {
                await _serverRunTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (TimeoutException)
        {
            // Server didn't stop in time, but that's OK for tests
        }

        _testTransport?.Dispose();
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
        _workspace?.Dispose();
        _loggerFactory?.Dispose();
        _cts?.Dispose();
    }

    [Fact]
    public async Task McpServer_ListTools_ReturnsExpectedTools()
    {
        // Act
        var tools = await _mcpClient.ListToolsAsync(cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(tools);
        Assert.Collection(tools.OrderBy(t => t.Name),
            tool => AssertTool(KnownMcpTools.Doctor, tool),
            tool => AssertTool(KnownMcpTools.ExecuteResourceCommand, tool),
            tool => AssertTool(KnownMcpTools.GetDoc, tool),
            tool => AssertTool(KnownMcpTools.ListAppHosts, tool),
            tool => AssertTool(KnownMcpTools.ListConsoleLogs, tool),
            tool => AssertTool(KnownMcpTools.ListDocs, tool),
            tool => AssertTool(KnownMcpTools.ListIntegrations, tool),
            tool => AssertTool(KnownMcpTools.ListResources, tool),
            tool => AssertTool(KnownMcpTools.ListStructuredLogs, tool),
            tool => AssertTool(KnownMcpTools.ListTraceStructuredLogs, tool),
            tool => AssertTool(KnownMcpTools.ListTraces, tool),
            tool => AssertTool(KnownMcpTools.RefreshTools, tool),
            tool => AssertTool(KnownMcpTools.SearchDocs, tool),
            tool => AssertTool(KnownMcpTools.SelectAppHost, tool));

        static void AssertTool(string expectedName, McpClientTool tool)
        {
            Assert.Equal(expectedName, tool.Name);
            Assert.False(string.IsNullOrEmpty(tool.Description), $"Tool '{tool.Name}' should have a description");
            Assert.NotEqual(default, tool.JsonSchema);
        }
    }

    [Fact]
    public async Task McpServer_CallTool_ListAppHosts_ReturnsResult()
    {
        // Act
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.ListAppHosts,
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.IsError);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
        Assert.Contains("App hosts", textContent.Text);
    }

    [Fact]
    public async Task McpServer_CallTool_ListIntegrations_ReturnsResult()
    {
        // Act
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.ListIntegrations,
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        // The result may be an error (if NuGet search fails) or success
        // Either way, we verify the tool is reachable and responds
        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
    }

    [Fact]
    public async Task McpServer_CallTool_Doctor_ReturnsResult()
    {
        // Act
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.Doctor,
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError is null or false, $"Tool returned error: {GetResultText(result)}");
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task McpServer_CallTool_RefreshTools_ReturnsResult()
    {
        // Act
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.RefreshTools,
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError is null or false, $"Tool returned error: {GetResultText(result)}");
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
        Assert.Contains("Tools refreshed", textContent.Text);
    }

    [Fact]
    public async Task McpServer_CallTool_ListResources_ReturnsErrorWhenNoAppHost()
    {
        // Act - Should throw an exception when no AppHost is running
        var exception = await Assert.ThrowsAsync<McpProtocolException>(async () =>
        {
            await _mcpClient.CallToolAsync(
                KnownMcpTools.ListResources,
                cancellationToken: _cts.Token).DefaultTimeout();
        });

        // Assert - The exception message should indicate no AppHost is running
        Assert.Contains("No Aspire AppHost is currently running", exception.Message);
    }

    [Fact]
    public async Task McpServer_CallTool_UnknownTool_ReturnsError()
    {
        // Act - Should throw an exception for unknown tool
        var exception = await Assert.ThrowsAsync<McpProtocolException>(async () =>
        {
            await _mcpClient.CallToolAsync(
                "unknown_tool_that_does_not_exist",
                cancellationToken: _cts.Token).DefaultTimeout();
        });

        // Assert - The exception message should indicate unknown tool
        Assert.Contains("Unknown tool", exception.Message);
    }

    [Fact]
    public async Task McpServer_ServerInfo_HasCorrectName()
    {
        // Assert
        Assert.NotNull(_mcpClient.ServerInfo);
        Assert.Equal("aspire-mcp-server", _mcpClient.ServerInfo.Name);
    }

    [Fact]
    public async Task McpServer_CallTool_SelectAppHost_WithEmptyPath_ReturnsError()
    {
        // Act - Empty path should return an error
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.SelectAppHost,
            new Dictionary<string, object?>
            {
                ["appHostPath"] = ""
            },
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
        Assert.Contains("appHostPath", textContent.Text);
    }

    [Fact]
    public async Task McpServer_CallTool_ListDocs_ReturnsResult()
    {
        // Act
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.ListDocs,
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError is null or false, $"Tool returned error: {GetResultText(result)}");
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
        // Verify it returns the test documents
        Assert.Contains("getting-started", textContent.Text);
        Assert.Contains("fundamentals/app-host", textContent.Text);
    }

    [Fact]
    public async Task McpServer_CallTool_SearchDocs_WithQuery_ReturnsResults()
    {
        // Act - Search for "Azure" which matches "Deploy to Azure" in test data
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.SearchDocs,
            new Dictionary<string, object?>
            {
                ["query"] = "Azure"
            },
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError is null or false, $"Tool returned error: {GetResultText(result)}");
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
        // Verify it finds the Azure-related test document
        Assert.Contains("deployment/azure", textContent.Text);
    }

    [Fact]
    public async Task McpServer_CallTool_GetDoc_WithSlug_ReturnsResult()
    {
        // Act - Use a slug that exists in the test docs index service
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.GetDoc,
            new Dictionary<string, object?>
            {
                ["slug"] = "fundamentals/app-host"
            },
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert - Tool should respond with the test document content
        Assert.NotNull(result);
        Assert.True(result.IsError is null or false, $"Tool returned error: {GetResultText(result)}");
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);
        Assert.Contains("App Host", textContent.Text);
    }

    private static string GetResultText(CallToolResult result)
    {
        if (result.Content?.FirstOrDefault() is TextContentBlock textContent)
        {
            return textContent.Text;
        }

        return string.Empty;
    }
}
