// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Packaging;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace Aspire.Cli.Tests.Commands;

public class McpStartCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void McpStartCommand_InitializesWithBuiltInTools()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var auxiliaryBackchannelMonitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        // Act
        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            auxiliaryBackchannelMonitor,
            loggerFactory,
            logger,
            packagingService);

        // Assert
        Assert.NotNull(command);
        
        // Verify the command has the expected built-in CLI tools by checking the private field
        var cliToolsField = typeof(McpStartCommand).GetField("_cliTools",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(cliToolsField);
        
        var cliTools = cliToolsField.GetValue(command) as System.Collections.IDictionary;
        Assert.NotNull(cliTools);
        
        var toolNames = cliTools.Keys.Cast<string>().OrderBy(name => name).ToArray();
        Assert.Collection(toolNames,
            name => Assert.Equal("get_integration_docs", name),
            name => Assert.Equal("list_apphosts", name),
            name => Assert.Equal("list_integrations", name),
            name => Assert.Equal("select_apphost", name));
    }

    [Fact]
    public void AuxiliaryBackchannelMonitor_RaisesEventWhenSelectedAppHostPathChanges()
    {
        // Arrange
        var monitor = new TestAuxiliaryBackchannelMonitor();
        var eventRaised = false;
        var eventCount = 0;

        monitor.SelectedAppHostChanged += () =>
        {
            eventRaised = true;
            eventCount++;
        };

        // Act
        monitor.SelectedAppHostPath = "/path/to/apphost1";

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void AuxiliaryBackchannelMonitor_DoesNotRaiseEventForSamePathValue()
    {
        // Arrange
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.SelectedAppHostPath = "/path/to/apphost1";
        
        var eventCount = 0;
        monitor.SelectedAppHostChanged += () =>
        {
            eventCount++;
        };

        // Act - Set the same path again
        monitor.SelectedAppHostPath = "/path/to/apphost1";

        // Assert - Event should not be raised
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void AuxiliaryBackchannelMonitor_RaisesEventMultipleTimesForDifferentPaths()
    {
        // Arrange
        var monitor = new TestAuxiliaryBackchannelMonitor();
        var paths = new List<string?>();

        monitor.SelectedAppHostPath = "/path/to/apphost1";

        monitor.SelectedAppHostChanged += () =>
        {
            paths.Add(monitor.SelectedAppHostPath);
        };

        // Act
        monitor.SelectedAppHostPath = "/path/to/apphost2";
        monitor.SelectedAppHostPath = "/path/to/apphost3";
        monitor.SelectedAppHostPath = null;

        // Assert
        Assert.Equal(3, paths.Count);
        Assert.Equal("/path/to/apphost2", paths[0]);
        Assert.Equal("/path/to/apphost3", paths[1]);
        Assert.Null(paths[2]);
    }

    [Fact]
    public void AuxiliaryBackchannelMonitor_RaisesEventWhenChangingFromNullToValue()
    {
        // Arrange
        var monitor = new TestAuxiliaryBackchannelMonitor();
        var eventRaised = false;

        monitor.SelectedAppHostChanged += () =>
        {
            eventRaised = true;
        };

        // Act
        monitor.SelectedAppHostPath = "/path/to/apphost";

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void AuxiliaryBackchannelMonitor_RaisesEventWhenChangingFromValueToNull()
    {
        // Arrange
        var monitor = new TestAuxiliaryBackchannelMonitor();
        monitor.SelectedAppHostPath = "/path/to/apphost";
        
        var eventRaised = false;
        monitor.SelectedAppHostChanged += () =>
        {
            eventRaised = true;
        };

        // Act
        monitor.SelectedAppHostPath = null;

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void GetSelectedConnection_ReturnsNull_WhenNoConnectionsExist()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>() as TestAuxiliaryBackchannelMonitor;
        Assert.NotNull(monitor);

        monitor.ClearConnections();

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            monitor,
            loggerFactory,
            logger,
            packagingService);

        // Act - Use reflection to call GetSelectedConnection
        var method = typeof(McpStartCommand).GetMethod("GetSelectedConnection",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(command, null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSelectedConnection_ReturnsSingleConnection_WhenOnlyOneInScopeExists()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>() as TestAuxiliaryBackchannelMonitor;
        Assert.NotNull(monitor);

        monitor.ClearConnections();

        // Create a mock connection
        var connection = CreateMockConnection("/path/to/apphost", isInScope: true);
        monitor.AddConnection("connection1", connection);

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            monitor,
            loggerFactory,
            logger,
            packagingService);

        // Act
        var method = typeof(McpStartCommand).GetMethod("GetSelectedConnection",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(command, null) as AppHostAuxiliaryBackchannel;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/path/to/apphost", result.AppHostInfo?.AppHostPath);
    }

    [Fact]
    public void GetSelectedConnection_ThrowsException_WhenMultipleInScopeConnectionsExist()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>() as TestAuxiliaryBackchannelMonitor;
        Assert.NotNull(monitor);

        monitor.ClearConnections();

        // Create multiple in-scope connections
        var connection1 = CreateMockConnection("/path/to/apphost1", isInScope: true);
        var connection2 = CreateMockConnection("/path/to/apphost2", isInScope: true);
        monitor.AddConnection("connection1", connection1);
        monitor.AddConnection("connection2", connection2);

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            monitor,
            loggerFactory,
            logger,
            packagingService);

        // Act
        var method = typeof(McpStartCommand).GetMethod("GetSelectedConnection",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Assert
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(command, null));
        Assert.NotNull(ex.InnerException);
        Assert.IsType<McpProtocolException>(ex.InnerException);
        Assert.Contains("Multiple Aspire AppHosts are running", ex.InnerException.Message);
    }

    [Fact]
    public void GetSelectedConnection_ReturnsSelectedConnection_WhenSpecificPathIsSet()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>() as TestAuxiliaryBackchannelMonitor;
        Assert.NotNull(monitor);

        monitor.ClearConnections();

        // Create multiple connections
        var connection1 = CreateMockConnection("/path/to/apphost1", isInScope: true);
        var connection2 = CreateMockConnection("/path/to/apphost2", isInScope: true);
        monitor.AddConnection("connection1", connection1);
        monitor.AddConnection("connection2", connection2);

        // Select a specific AppHost
        monitor.SelectedAppHostPath = "/path/to/apphost2";

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            monitor,
            loggerFactory,
            logger,
            packagingService);

        // Act
        var method = typeof(McpStartCommand).GetMethod("GetSelectedConnection",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(command, null) as AppHostAuxiliaryBackchannel;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/path/to/apphost2", result.AppHostInfo?.AppHostPath);
    }

    [Fact]
    public void GetSelectedConnection_AutoSelectsSingleOutOfScopeConnection_WhenNoInScopeExists()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>() as TestAuxiliaryBackchannelMonitor;
        Assert.NotNull(monitor);

        monitor.ClearConnections();

        // Create a single out-of-scope connection
        var connection = CreateMockConnection("/path/to/apphost", isInScope: false);
        monitor.AddConnection("connection1", connection);

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            monitor,
            loggerFactory,
            logger,
            packagingService);

        // Act
        var method = typeof(McpStartCommand).GetMethod("GetSelectedConnection",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(command, null) as AppHostAuxiliaryBackchannel;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/path/to/apphost", result.AppHostInfo?.AppHostPath);
        Assert.False(result.IsInScope);
    }

    [Fact]
    public void GetSelectedConnection_ThrowsException_WhenMultipleOutOfScopeConnectionsExist()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IAuxiliaryBackchannelMonitor>() as TestAuxiliaryBackchannelMonitor;
        Assert.NotNull(monitor);

        monitor.ClearConnections();

        // Create multiple out-of-scope connections
        var connection1 = CreateMockConnection("/path/to/apphost1", isInScope: false);
        var connection2 = CreateMockConnection("/path/to/apphost2", isInScope: false);
        monitor.AddConnection("connection1", connection1);
        monitor.AddConnection("connection2", connection2);

        var interactionService = provider.GetRequiredService<IInteractionService>();
        var features = provider.GetRequiredService<IFeatures>();
        var updateNotifier = provider.GetRequiredService<ICliUpdateNotifier>();
        var executionContext = provider.GetRequiredService<CliExecutionContext>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<McpStartCommand>();
        var packagingService = provider.GetRequiredService<IPackagingService>();

        var command = new McpStartCommand(
            interactionService,
            features,
            updateNotifier,
            executionContext,
            monitor,
            loggerFactory,
            logger,
            packagingService);

        // Act
        var method = typeof(McpStartCommand).GetMethod("GetSelectedConnection",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        // Assert
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(command, null));
        Assert.NotNull(ex.InnerException);
        Assert.IsType<McpProtocolException>(ex.InnerException);
        Assert.Contains("No Aspire AppHosts are running in the scope", ex.InnerException.Message);
    }

    [Fact]
    public void AuxiliaryBackchannelMonitor_EventTriggersCorrectSequence()
    {
        // Arrange
        var monitor = new TestAuxiliaryBackchannelMonitor();
        var events = new List<(string Action, string? Path)>();

        monitor.SelectedAppHostChanged += () =>
        {
            events.Add(("Changed", monitor.SelectedAppHostPath));
        };

        // Act
        events.Add(("Initial", null));
        monitor.SelectedAppHostPath = "/path/to/apphost1";
        monitor.SelectedAppHostPath = "/path/to/apphost1"; // Same value, no event
        monitor.SelectedAppHostPath = "/path/to/apphost2";
        monitor.SelectedAppHostPath = null;
        monitor.SelectedAppHostPath = null; // Same value (null), no event

        // Assert
        Assert.Equal(4, events.Count);
        Assert.Equal(("Initial", (string?)null), events[0]);
        Assert.Equal(("Changed", "/path/to/apphost1"), events[1]);
        Assert.Equal(("Changed", "/path/to/apphost2"), events[2]);
        Assert.Equal(("Changed", (string?)null), events[3]);
    }

    /// <summary>
    /// Creates a mock AppHostAuxiliaryBackchannel connection for testing.
    /// </summary>
    private static AppHostAuxiliaryBackchannel CreateMockConnection(string appHostPath, bool isInScope)
    {
        var mcpInfo = new DashboardMcpConnectionInfo
        {
            EndpointUrl = "http://localhost:5000/mcp",
            ApiToken = "test-token"
        };

        var appHostInfo = new AppHostInformation
        {
            AppHostPath = appHostPath,
            ProcessId = 12345,
            CliProcessId = 67890
        };

        // Use reflection to create the connection since the constructor is internal
        var constructor = typeof(AppHostAuxiliaryBackchannel).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(string), typeof(string), typeof(StreamJsonRpc.JsonRpc), typeof(DashboardMcpConnectionInfo), typeof(AppHostInformation), typeof(bool), typeof(ILogger)],
            null);

        Assert.NotNull(constructor);

        // Create a dummy JsonRpc instance (we won't use it in these tests)
        // Use a network stream that won't be closed
        var clientStream = new System.IO.MemoryStream();
        var serverStream = new System.IO.MemoryStream();
        
        // Create JsonRpc with the stream but don't start listening
        var rpc = new StreamJsonRpc.JsonRpc(clientStream, serverStream);

        var connection = constructor.Invoke(
        [
            "test-hash",
            "/tmp/test.sock",
            rpc,
            mcpInfo,
            appHostInfo,
            isInScope,
            null
        ]) as AppHostAuxiliaryBackchannel;

        Assert.NotNull(connection);
        return connection;
    }
}
