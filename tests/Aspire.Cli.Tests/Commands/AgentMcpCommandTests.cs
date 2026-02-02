// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Mcp;
using Aspire.Cli.Tests.Mcp;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Threading.Channels;

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
    private AgentMcpCommand _agentMcpCommand = null!;
    private Task _serverRunTask = null!;
    private CancellationTokenSource _cts = null!;
    private ILoggerFactory _loggerFactory = null!;
    private TestAuxiliaryBackchannelMonitor _backchannelMonitor = null!;

    public async ValueTask InitializeAsync()
    {
        _cts = new CancellationTokenSource();
        _workspace = TemporaryWorkspace.Create(outputHelper);

        // Create the test transport with in-memory pipes
        _loggerFactory = LoggerFactory.Create(builder => builder.AddXunit(outputHelper));
        _testTransport = new TestMcpServerTransport(_loggerFactory);

        // Create a backchannel monitor that we can configure for resource tool tests
        _backchannelMonitor = new TestAuxiliaryBackchannelMonitor();

        // Create services using CliTestHelper with custom MCP transport and test docs service
        var services = CliTestHelper.CreateServiceCollection(_workspace, outputHelper, options =>
        {
            // Override the MCP transport with our test transport
            options.McpServerTransportFactory = _ => _testTransport.ServerTransport;
            // Override the docs index service with a test implementation that doesn't make network calls
            options.DocsIndexServiceFactory = _ => new TestDocsIndexService();
            // Override the backchannel monitor with our test implementation
            options.AuxiliaryBackchannelMonitorFactory = _ => _backchannelMonitor;
        });

        _serviceProvider = services.BuildServiceProvider();

        // Get the AgentMcpCommand from DI and start the server
        _agentMcpCommand = _serviceProvider.GetRequiredService<AgentMcpCommand>();
        var rootCommand = _serviceProvider.GetRequiredService<RootCommand>();
        var parseResult = rootCommand.Parse("agent mcp");

        // Start the MCP server in the background
        _serverRunTask = Task.Run(async () =>
        {
            try
            {
                await _agentMcpCommand.ExecuteCommandAsync(parseResult, _cts.Token);
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
    public async Task McpServer_ListTools_IncludesResourceMcpTools()
    {
        // Arrange - Create a mock backchannel with a resource that has MCP tools
        var mockBackchannel = new TestAppHostAuxiliaryBackchannel
        {
            Hash = "test-apphost-hash",
            IsInScope = true,
            AppHostInfo = new AppHostInformation
            {
                AppHostPath = Path.Combine(_workspace.WorkspaceRoot.FullName, "TestAppHost", "TestAppHost.csproj"),
                ProcessId = 12345
            },
            ResourceSnapshots =
            [
                new ResourceSnapshot
                {
                    Name = "test-resource",
                    DisplayName = "Test Resource",
                    ResourceType = "Container",
                    State = "Running",
                    McpServer = new ResourceSnapshotMcpServer
                    {
                        EndpointUrl = "http://localhost:8080/mcp",
                        Tools =
                        [
                            new Tool
                            {
                                Name = "resource_tool_one",
                                Description = "A test tool from the resource"
                            },
                            new Tool
                            {
                                Name = "resource_tool_two",
                                Description = "Another test tool from the resource"
                            }
                        ]
                    }
                }
            ]
        };

        // Register the mock backchannel
        _backchannelMonitor.AddConnection(mockBackchannel.Hash, mockBackchannel.SocketPath, mockBackchannel);

        // First call refresh_tools to discover the resource tools
        await _mcpClient.CallToolAsync(KnownMcpTools.RefreshTools, cancellationToken: _cts.Token).DefaultTimeout();

        // Act - List all tools
        var tools = await _mcpClient.ListToolsAsync(cancellationToken: _cts.Token).DefaultTimeout();

        // Assert - Verify resource tools are included
        Assert.NotNull(tools);

        // The resource tools should be exposed with a prefixed name: {resource_name}_{tool_name}
        // Resource name "test-resource" becomes "test_resource" (dashes replaced with underscores)
        var resourceToolOne = tools.FirstOrDefault(t => t.Name == "test_resource_resource_tool_one");
        var resourceToolTwo = tools.FirstOrDefault(t => t.Name == "test_resource_resource_tool_two");

        Assert.NotNull(resourceToolOne);
        Assert.NotNull(resourceToolTwo);

        Assert.Equal("A test tool from the resource", resourceToolOne.Description);
        Assert.Equal("Another test tool from the resource", resourceToolTwo.Description);
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
    public async Task McpServer_CallTool_RefreshTools_ReturnsResult()
    {
        // Arrange - Set up a channel to receive the ToolListChanged notification
        var notificationChannel = Channel.CreateUnbounded<JsonRpcNotification>();
        await using var notificationHandler = _mcpClient.RegisterNotificationHandler(
            NotificationMethods.ToolListChangedNotification,
            (notification, cancellationToken) =>
            {
                notificationChannel.Writer.TryWrite(notification);
                return default;
            });

        // Act
        var result = await _mcpClient.CallToolAsync(
            KnownMcpTools.RefreshTools,
            cancellationToken: _cts.Token).DefaultTimeout();

        // Assert - Verify result
        Assert.NotNull(result);
        Assert.True(result.IsError is null or false, $"Tool returned error: {GetResultText(result)}");
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        var textContent = result.Content[0] as TextContentBlock;
        Assert.NotNull(textContent);

        // Verify the exact text content with the correct tool count
        var expectedToolCount = _agentMcpCommand.KnownTools.Count;
        Assert.Equal($"Tools refreshed: {expectedToolCount} tools available", textContent.Text);

        // Assert - Verify the ToolListChanged notification was received
        var notification = await notificationChannel.Reader.ReadAsync(_cts.Token).AsTask().DefaultTimeout();
        Assert.NotNull(notification);
        Assert.Equal(NotificationMethods.ToolListChangedNotification, notification.Method);
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
