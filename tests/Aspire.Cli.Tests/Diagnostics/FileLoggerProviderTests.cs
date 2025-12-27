// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Aspire.Cli.Tests.Diagnostics;

public class FileLoggerProviderTests
{
    [Fact]
    public async Task FileLoggerProvider_CreatesDirectoryWithTimestamp()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var homeDir = new DirectoryInfo(tempDir);
        var workingDir = new DirectoryInfo(Path.GetTempPath());
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir, "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir, "cache"));
        var sdksDir = new DirectoryInfo(Path.Combine(tempDir, "sdks"));
        
        var context = new CliExecutionContext(workingDir, hivesDir, cacheDir, sdksDir, homeDirectory: homeDir);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero));

        try
        {
            // Act
            using var provider = new FileLoggerProvider(context, timeProvider);
            var logger = provider.CreateLogger("Test");
            
            // Log an error to trigger bundle creation
            logger.LogError(new InvalidOperationException("Test error"), "Test error occurred");
            
            // Wait for async writes to complete
            await provider.FlushAsync();
            
            var bundlePath = provider.GetDiagnosticsPath();

            // Assert
            Assert.NotNull(bundlePath);
            Assert.True(Directory.Exists(bundlePath));
            Assert.Contains("2025-01-15-10-30-00", bundlePath);

            // Verify files exist
            Assert.True(File.Exists(Path.Combine(bundlePath, "aspire.log")));
            Assert.True(File.Exists(Path.Combine(bundlePath, "environment.json")));
            Assert.True(File.Exists(Path.Combine(bundlePath, "error.txt")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FileLoggerProvider_ErrorFileContainsExceptionDetails()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var homeDir = new DirectoryInfo(tempDir);
        var workingDir = new DirectoryInfo(Path.GetTempPath());
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir, "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir, "cache"));
        var sdksDir = new DirectoryInfo(Path.Combine(tempDir, "sdks"));
        
        var context = new CliExecutionContext(workingDir, hivesDir, cacheDir, sdksDir, homeDirectory: homeDir);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero));

        try
        {
            // Act
            using var provider = new FileLoggerProvider(context, timeProvider);
            var logger = provider.CreateLogger("Test");
            
            logger.LogError(new InvalidOperationException("Test error message"), "Build failed");
            
            // Wait for async writes to complete
            await provider.FlushAsync();
            
            var bundlePath = provider.GetDiagnosticsPath();

            // Assert
            Assert.NotNull(bundlePath);
            var errorFilePath = Path.Combine(bundlePath, "error.txt");
            var errorContent = File.ReadAllText(errorFilePath);
            
            Assert.Contains("Aspire CLI Failure Report", errorContent);
            Assert.Contains("Test error message", errorContent);
            Assert.Contains("Build failed", errorContent);
            Assert.Contains("Exception Details:", errorContent);
            Assert.Contains("InvalidOperationException", errorContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FileLoggerProvider_EnvironmentFileContainsSystemInfo()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var homeDir = new DirectoryInfo(tempDir);
        var workingDir = new DirectoryInfo(Path.GetTempPath());
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir, "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir, "cache"));
        var sdksDir = new DirectoryInfo(Path.Combine(tempDir, "sdks"));
        
        var context = new CliExecutionContext(workingDir, hivesDir, cacheDir, sdksDir, homeDirectory: homeDir);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero));

        try
        {
            // Act
            using var provider = new FileLoggerProvider(context, timeProvider);
            var logger = provider.CreateLogger("Test");
            
            logger.LogError(new InvalidOperationException("Test"), "Test error");
            
            // Wait for async writes to complete
            await provider.FlushAsync();
            
            var bundlePath = provider.GetDiagnosticsPath();

            // Assert
            Assert.NotNull(bundlePath);
            var envFilePath = Path.Combine(bundlePath, "environment.json");
            var envContent = File.ReadAllText(envFilePath);
            
            // Verify JSON structure
            Assert.Contains("\"cli\"", envContent);
            Assert.Contains("\"version\"", envContent);
            Assert.Contains("\"debugMode\"", envContent);
            Assert.Contains("\"verboseMode\"", envContent);
            Assert.Contains("\"os\"", envContent);
            Assert.Contains("\"platform\"", envContent);
            Assert.Contains("\"dotnet\"", envContent);
            Assert.Contains("\"runtimeVersion\"", envContent);
            Assert.Contains("\"process\"", envContent);
            Assert.Contains("\"processId\"", envContent);
            Assert.Contains("\"docker\"", envContent);
            Assert.Contains("\"environment\"", envContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FileLoggerProvider_HandlesInnerExceptions()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-test-" + Guid.NewGuid());
        var homeDir = new DirectoryInfo(tempDir);
        var workingDir = new DirectoryInfo(Path.GetTempPath());
        var hivesDir = new DirectoryInfo(Path.Combine(tempDir, "hives"));
        var cacheDir = new DirectoryInfo(Path.Combine(tempDir, "cache"));
        var sdksDir = new DirectoryInfo(Path.Combine(tempDir, "sdks"));
        
        var context = new CliExecutionContext(workingDir, hivesDir, cacheDir, sdksDir, homeDirectory: homeDir);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero));

        try
        {
            // Act
            var innerException = new ArgumentException("Inner error");
            var outerException = new InvalidOperationException("Outer error", innerException);
            
            using var provider = new FileLoggerProvider(context, timeProvider);
            var logger = provider.CreateLogger("Test");
            
            logger.LogError(outerException, "Command failed");
            
            // Wait for async writes to complete
            await provider.FlushAsync();
            
            var bundlePath = provider.GetDiagnosticsPath();

            // Assert
            Assert.NotNull(bundlePath);
            var errorFilePath = Path.Combine(bundlePath, "error.txt");
            var errorContent = File.ReadAllText(errorFilePath);
            
            Assert.Contains("Outer error", errorContent);
            Assert.Contains("Inner error", errorContent);
            Assert.Contains("Inner Exception (1):", errorContent);
            Assert.Contains("ArgumentException", errorContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

}
