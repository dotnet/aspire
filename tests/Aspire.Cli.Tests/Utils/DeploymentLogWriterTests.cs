// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class DeploymentLogWriterTests
{
    [Fact]
    public void DeploymentLogWriter_CreatesLogFile()
    {
        // Arrange
        var appHostSha = "TEST_SHA_" + Guid.NewGuid().ToString("N");
        
        // Act & Assert
        using (var writer = new DeploymentLogWriter(appHostSha))
        {
            Assert.NotNull(writer.LogFilePath);
            Assert.True(File.Exists(writer.LogFilePath));
            
            // Verify the log file is in the expected directory structure
            Assert.Contains(".aspire", writer.LogFilePath);
            Assert.Contains("deployments", writer.LogFilePath);
            Assert.Contains(appHostSha, writer.LogFilePath);
        }
    }

    [Fact]
    public void DeploymentLogWriter_WritesLinesToFile()
    {
        // Arrange
        var appHostSha = "TEST_SHA_" + Guid.NewGuid().ToString("N");
        var testMessage1 = "Test message 1";
        var testMessage2 = "Test message 2";
        string logFilePath;
        
        // Act
        using (var writer = new DeploymentLogWriter(appHostSha))
        {
            logFilePath = writer.LogFilePath;
            writer.WriteLine(testMessage1);
            writer.WriteLine(testMessage2);
        }
        
        // Assert
        Assert.True(File.Exists(logFilePath));
        var logContent = File.ReadAllText(logFilePath);
        Assert.Contains(testMessage1, logContent);
        Assert.Contains(testMessage2, logContent);
        
        // Verify timestamps are present
        Assert.Matches(@"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]", logContent);
        
        // Cleanup
        try
        {
            File.Delete(logFilePath);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Fact]
    public void DeploymentLogWriter_CreatesDirectoryStructure()
    {
        // Arrange
        var appHostSha = "TEST_SHA_" + Guid.NewGuid().ToString("N");
        
        // Act
        using (var writer = new DeploymentLogWriter(appHostSha))
        {
            var logDirectory = Path.GetDirectoryName(writer.LogFilePath);
            
            // Assert
            Assert.NotNull(logDirectory);
            Assert.True(Directory.Exists(logDirectory));
        }
    }

    [Fact]
    public void DeploymentLogWriter_LogFileNameContainsTimestamp()
    {
        // Arrange
        var appHostSha = "TEST_SHA_" + Guid.NewGuid().ToString("N");
        
        // Act
        using (var writer = new DeploymentLogWriter(appHostSha))
        {
            var logFileName = Path.GetFileName(writer.LogFilePath);
            
            // Assert - verify the file name matches the expected timestamp pattern (yyyyMMdd-HHmmss.log)
            Assert.Matches(@"\d{8}-\d{6}\.log", logFileName);
        }
    }
}
