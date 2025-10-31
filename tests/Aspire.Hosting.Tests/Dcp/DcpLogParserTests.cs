// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests.Dcp;

public sealed class DcpLogParserTests
{
    [Fact]
    public void TryParseDcpLog_ValidInfoLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\tinfo\tdcpctrl.ServiceReconciler\tservice /apigateway is now in state Ready";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Equal("service /apigateway is now in state Ready", message);
        Assert.Equal(LogLevel.Information, logLevel);
        Assert.Equal("dcpctrl.ServiceReconciler", category);
    }

    [Fact]
    public void TryParseDcpLog_ValidErrorLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\terror\tdcpctrl.ExecutableReconciler\tFailed to start a process";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Equal("Failed to start a process", message);
        Assert.Equal(LogLevel.Error, logLevel);
        Assert.Equal("dcpctrl.ExecutableReconciler", category);
    }

    [Fact]
    public void TryParseDcpLog_ValidWarningLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\twarning\tdcpctrl.TestReconciler\tWarning message";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Equal("Warning message", message);
        Assert.Equal(LogLevel.Warning, logLevel);
        Assert.Equal("dcpctrl.TestReconciler", category);
    }

    [Fact]
    public void TryParseDcpLog_ValidDebugLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\tdebug\tdcpctrl.TestReconciler\tDebug message";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Equal("Debug message", message);
        Assert.Equal(LogLevel.Debug, logLevel);
        Assert.Equal("dcpctrl.TestReconciler", category);
    }

    [Fact]
    public void TryParseDcpLog_ValidTraceLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\ttrace\tdcpctrl.TestReconciler\tTrace message";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Equal("Trace message", message);
        Assert.Equal(LogLevel.Trace, logLevel);
        Assert.Equal("dcpctrl.TestReconciler", category);
    }

    [Fact]
    public void TryParseDcpLog_WithCarriageReturn_TrimsSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\tinfo\tdcpctrl.ServiceReconciler\ttest message\r";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out _);

        // Assert
        Assert.True(result);
        Assert.Equal("test message", message);
        Assert.Equal(LogLevel.Information, logLevel);
    }

    [Fact]
    public void TryParseDcpLog_WithJsonPayload_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\tinfo\tdcpctrl.ServiceReconciler\tservice /apigateway is now in state Ready\t{\"ServiceName\": {\"name\":\"apigateway\"}}";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out _);

        // Assert
        Assert.True(result);
        Assert.Equal("service /apigateway is now in state Ready\t{\"ServiceName\": {\"name\":\"apigateway\"}}", message);
        Assert.Equal(LogLevel.Information, logLevel);
    }

    [Fact]
    public void TryParseDcpLog_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var logLine = "This is not a DCP formatted log";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out _, out _, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParseDcpLog_MissingTabs_ReturnsFalse()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700 info message";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out _, out _, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParseDcpLog_StringOverload_ValidErrorLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\terror\tdcpctrl.ExecutableReconciler\tFailed to start a process";

        // Act
        var result = DcpLogParser.TryParseDcpLog(logLine, out var message, out var logLevel, out var isErrorLevel);

        // Assert
        Assert.True(result);
        Assert.Equal("Failed to start a process", message);
        Assert.Equal(LogLevel.Error, logLevel);
        Assert.True(isErrorLevel);
    }

    [Fact]
    public void TryParseDcpLog_StringOverload_ValidInfoLog_ParsesSuccessfully()
    {
        // Arrange
        var logLine = "2023-09-19T20:40:50.509-0700\tinfo\tdcpctrl.ServiceReconciler\tservice /apigateway is now in state Ready";

        // Act
        var result = DcpLogParser.TryParseDcpLog(logLine, out var message, out var logLevel, out var isErrorLevel);

        // Assert
        Assert.True(result);
        Assert.Equal("service /apigateway is now in state Ready", message);
        Assert.Equal(LogLevel.Information, logLevel);
        Assert.False(isErrorLevel);
    }

    [Fact]
    public void TryParseDcpLog_StringOverload_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var logLine = "This is not a DCP formatted log";

        // Act
        var result = DcpLogParser.TryParseDcpLog(logLine, out _, out _, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryParseDcpLog_RealWorldExampleFromIssue_ParsesSuccessfully()
    {
        // Arrange - Example from the issue
        var logLine = "2023-09-19T20:40:50.509-0700\tinfo\tdcpctrl.ExecutableReconciler\tStarting process...\t{\"Executable\": \"/python-api-duafrsvy\", \"Reconciliation\": 3, \"Cmd\": \"d:\\\\dev\\\\git\\\\dogfood\\\\hellopython\\\\pyapp\\\\.venv\\\\Scripts\\\\python.exe\", \"Args\": [\"main.py\"]}";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Contains("Starting process...", message);
        Assert.Equal(LogLevel.Information, logLevel);
        Assert.Equal("dcpctrl.ExecutableReconciler", category);
    }

    [Theory]
    [InlineData("error", LogLevel.Error)]
    [InlineData("warning", LogLevel.Warning)]
    [InlineData("info", LogLevel.Information)]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("trace", LogLevel.Trace)]
    public void TryParseDcpLog_ParsesAllLogLevels(string dcpLogLevel, LogLevel expectedLogLevel)
    {
        // Arrange
        var logLine = $"2023-09-19T20:40:50.509-0700\t{dcpLogLevel}\tdcpctrl.TestReconciler\tTest message";
        var bytes = Encoding.UTF8.GetBytes(logLine);

        // Act
        var result = DcpLogParser.TryParseDcpLog(bytes.AsSpan(), out var message, out var logLevel, out var category);

        // Assert
        Assert.True(result);
        Assert.Equal("Test message", message);
        Assert.Equal(expectedLogLevel, logLevel);
        Assert.Equal("dcpctrl.TestReconciler", category);
    }
}
