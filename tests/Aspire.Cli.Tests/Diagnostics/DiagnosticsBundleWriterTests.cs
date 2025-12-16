// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Aspire.Cli.Tests.Diagnostics;

public class DiagnosticsBundleWriterTests
{
    [Fact]
    public async Task WriteFailureBundleAsync_CreatesDirectoryWithTimestamp()
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
        var writer = new DiagnosticsBundleWriter(context, NullLogger<DiagnosticsBundleWriter>.Instance, timeProvider);
        
        var exception = new InvalidOperationException("Test error message");

        try
        {
            // Act
            var bundlePath = await writer.WriteFailureBundleAsync(exception, ExitCodeConstants.FailedToDotnetRunAppHost, "run", "Additional context");

            // Assert
            Assert.NotNull(bundlePath);
            Assert.True(Directory.Exists(bundlePath));
            Assert.Contains("2025-01-15-10-30-00", bundlePath);

            // Verify files exist
            Assert.True(File.Exists(Path.Combine(bundlePath, "error.txt")));
            Assert.True(File.Exists(Path.Combine(bundlePath, "environment.json")));
            Assert.True(File.Exists(Path.Combine(bundlePath, "aspire.log")));
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
    public async Task WriteFailureBundleAsync_ErrorFileContainsExceptionDetails()
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
        var writer = new DiagnosticsBundleWriter(context, NullLogger<DiagnosticsBundleWriter>.Instance, timeProvider);
        
        var exception = new InvalidOperationException("Test error message");

        try
        {
            // Act
            var bundlePath = await writer.WriteFailureBundleAsync(exception, ExitCodeConstants.FailedToBuildArtifacts, "run", "Build failed");

            // Assert
            Assert.NotNull(bundlePath);
            var errorFilePath = Path.Combine(bundlePath, "error.txt");
            var errorContent = await File.ReadAllTextAsync(errorFilePath);
            
            Assert.Contains("Aspire CLI Failure Report", errorContent);
            Assert.Contains("Command: aspire run", errorContent);
            Assert.Contains("Exit Code: 6", errorContent); // ExitCodeConstants.FailedToBuildArtifacts
            Assert.Contains("Test error message", errorContent);
            Assert.Contains("Additional Context:", errorContent);
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
    public async Task WriteFailureBundleAsync_EnvironmentFileContainsSystemInfo()
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
        var writer = new DiagnosticsBundleWriter(context, NullLogger<DiagnosticsBundleWriter>.Instance, timeProvider);
        
        var exception = new InvalidOperationException("Test");

        try
        {
            // Act
            var bundlePath = await writer.WriteFailureBundleAsync(exception, ExitCodeConstants.InvalidCommand, "test");

            // Assert
            Assert.NotNull(bundlePath);
            var envFilePath = Path.Combine(bundlePath, "environment.json");
            var envContent = await File.ReadAllTextAsync(envFilePath);
            
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
    public async Task WriteFailureBundleAsync_HandlesInnerExceptions()
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
        var writer = new DiagnosticsBundleWriter(context, NullLogger<DiagnosticsBundleWriter>.Instance, timeProvider);
        
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);

        try
        {
            // Act
            var bundlePath = await writer.WriteFailureBundleAsync(outerException, ExitCodeConstants.FailedToDotnetRunAppHost, "run");

            // Assert
            Assert.NotNull(bundlePath);
            var errorFilePath = Path.Combine(bundlePath, "error.txt");
            var errorContent = await File.ReadAllTextAsync(errorFilePath);
            
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

    [Fact]
    public async Task WriteFailureBundleAsync_DoesNotThrowOnFailure()
    {
        // Arrange - Use an invalid path to trigger an error
        var invalidDir = new DirectoryInfo("/dev/null/invalid/path");
        var context = new CliExecutionContext(invalidDir, invalidDir, invalidDir, invalidDir, homeDirectory: invalidDir);
        var timeProvider = new FakeTimeProvider();
        var writer = new DiagnosticsBundleWriter(context, NullLogger<DiagnosticsBundleWriter>.Instance, timeProvider);
        
        var exception = new InvalidOperationException("Test");

        // Act - Should not throw
        var bundlePath = await writer.WriteFailureBundleAsync(exception, ExitCodeConstants.InvalidCommand, "test");

        // Assert - Should return null on failure
        Assert.Null(bundlePath);
    }
}
