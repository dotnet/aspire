// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class ErrorLoggerTests
{
    [Fact]
    public void LogError_Exception_CreatesLogFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var executionContext = new CliExecutionContext(
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(Path.Combine(tempDir, "hives")),
            new DirectoryInfo(Path.Combine(tempDir, "cache")),
            new DirectoryInfo(Path.Combine(tempDir, "sdks")),
            debugMode: false);

        try
        {
            var logger = new ErrorLogger(executionContext);
            var exception = new InvalidOperationException("Test exception");

            // Act
            var logFilePath = logger.LogError(exception, "test command");

            // Assert
            Assert.True(File.Exists(logFilePath), "Log file should exist");
            var logContent = File.ReadAllText(logFilePath);
            Assert.Contains("Test exception", logContent);
            Assert.Contains("InvalidOperationException", logContent);
            Assert.Contains("test command", logContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void LogError_Message_CreatesLogFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var executionContext = new CliExecutionContext(
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(Path.Combine(tempDir, "hives")),
            new DirectoryInfo(Path.Combine(tempDir, "cache")),
            new DirectoryInfo(Path.Combine(tempDir, "sdks")),
            debugMode: false);

        try
        {
            var logger = new ErrorLogger(executionContext);

            // Act
            var logFilePath = logger.LogError("Test error message", "Test details", "test command");

            // Assert
            Assert.True(File.Exists(logFilePath), "Log file should exist");
            var logContent = File.ReadAllText(logFilePath);
            Assert.Contains("Test error message", logContent);
            Assert.Contains("Test details", logContent);
            Assert.Contains("test command", logContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void GetLogsDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var executionContext = new CliExecutionContext(
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(Path.Combine(tempDir, "hives")),
            new DirectoryInfo(Path.Combine(tempDir, "cache")),
            new DirectoryInfo(Path.Combine(tempDir, "sdks")),
            debugMode: false);

        try
        {
            var logger = new ErrorLogger(executionContext);

            // Act
            var logsDir = logger.GetLogsDirectory();

            // Assert
            Assert.Contains(".aspire", logsDir.FullName);
            Assert.Contains("logs", logsDir.FullName);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
