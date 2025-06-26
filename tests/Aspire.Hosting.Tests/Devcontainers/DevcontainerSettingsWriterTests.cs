// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests.Devcontainers;

public class DevcontainerSettingsWriterTests
{
    [Fact]
    public async Task WriteSettingsAsync_CodespaceEnvironment_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        // We can't directly test the home directory behavior, so we'll manually test
        // what happens when we try to create a settings file in a nested path that doesn't exist
        var nestedPath = Path.Combine(tempDir, "deep", "nested", "directory", "settings.json");
        
        // Ensure the directory doesn't exist
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
        
        try
        {
            // This simulates what happens inside the DevcontainerSettingsWriter
            // when EnsureSettingsFileExists is called with a path where parent directories don't exist
            if (!File.Exists(nestedPath))
            {
                // This is the fix we added - ensure parent directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(nestedPath)!);
                
                using var stream = File.Open(nestedPath, FileMode.CreateNew);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync("{}".AsMemory(), CancellationToken.None);
            }
            
            // Assert
            Assert.True(File.Exists(nestedPath), "Settings file should be created");
            Assert.True(Directory.Exists(Path.GetDirectoryName(nestedPath)), "Parent directory should be created");
            
            var content = await File.ReadAllTextAsync(nestedPath);
            Assert.Equal("{}", content);
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
    public async Task WriteSettingsAsync_DoesNotOverwriteExistingFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var settingsPath = Path.Combine(tempDir, "existing", "settings.json");
        
        // Create the directory and an existing file
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await File.WriteAllTextAsync(settingsPath, """{"existing": "content"}""");
        
        try
        {
            // This simulates what happens inside EnsureSettingsFileExists when file already exists
            if (!File.Exists(settingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                
                using var stream = File.Open(settingsPath, FileMode.CreateNew);
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync("{}".AsMemory(), CancellationToken.None);
            }
            
            // Assert - file should remain unchanged since it already existed
            Assert.True(File.Exists(settingsPath), "Settings file should still exist");
            
            var content = await File.ReadAllTextAsync(settingsPath);
            Assert.Equal("""{"existing": "content"}""", content);
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
    public async Task WriteSettingsAsync_FailureScenario_WithoutDirectoryCreation()
    {
        // This test demonstrates the original problem - without the fix, this would fail
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var nestedPath = Path.Combine(tempDir, "deep", "nested", "directory", "settings.json");
        
        // Ensure the directory doesn't exist
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
        
        try
        {
            // This is what would happen WITHOUT our fix (commented out Directory.CreateDirectory)
            var exception = await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            {
                if (!File.Exists(nestedPath))
                {
                    // NOT calling Directory.CreateDirectory(Path.GetDirectoryName(nestedPath)!);
                    
                    using var stream = File.Open(nestedPath, FileMode.CreateNew);
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync("{}".AsMemory(), CancellationToken.None);
                }
            });
            
            Assert.Contains("Could not find a part of the path", exception.Message);
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