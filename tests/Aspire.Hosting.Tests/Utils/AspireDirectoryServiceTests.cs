// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Tests.Utils;

public class AspireDirectoryServiceTests
{
    private const string TestAppHostName = "TestAppHost";
    private const string TestAppHostSha = "1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF";

    [Fact]
    public void TempDirectory_BasePath_ReturnsAppHostSpecificDirectory()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire", "temp",
            $"{TestAppHostName.ToLowerInvariant()}-{TestAppHostSha[..12].ToLowerInvariant()}");
        
        Assert.Equal(expected, service.TempDirectory.BasePath);
    }

    [Fact]
    public void TempDirectory_RespectsConfiguration()
    {
        var customPath = Path.Combine(Path.GetTempPath(), "custom-aspire-temp");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = customPath
            })
            .Build();

        try
        {
            var service = new AspireDirectoryService(config, TestAppHostName, TestAppHostSha);
            
            var expectedBasePath = Path.Combine(
                Path.GetFullPath(customPath),
                $"{TestAppHostName.ToLowerInvariant()}-{TestAppHostSha[..12].ToLowerInvariant()}");
            
            Assert.Equal(expectedBasePath, service.TempDirectory.BasePath);
        }
        finally
        {
            var expectedDir = Path.Combine(
                Path.GetFullPath(customPath),
                $"{TestAppHostName.ToLowerInvariant()}-{TestAppHostSha[..12].ToLowerInvariant()}");
            if (Directory.Exists(expectedDir))
            {
                Directory.Delete(expectedDir, recursive: true);
            }
            if (Directory.Exists(customPath))
            {
                Directory.Delete(customPath, recursive: true);
            }
        }
    }

    [Fact]
    public void TempDirectory_ConfigurationSourcePriorityIsRespected()
    {
        // Test that later configuration sources override earlier ones
        var firstPath = Path.Combine(Path.GetTempPath(), "first-aspire-temp");
        var secondPath = Path.Combine(Path.GetTempPath(), "second-aspire-temp");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = firstPath
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = secondPath
            })
            .Build();

        try
        {
            var service = new AspireDirectoryService(config, TestAppHostName, TestAppHostSha);
            
            // Should use the second path (later source wins)
            var expectedBasePath = Path.Combine(
                Path.GetFullPath(secondPath),
                $"{TestAppHostName.ToLowerInvariant()}-{TestAppHostSha[..12].ToLowerInvariant()}");
            
            Assert.Equal(expectedBasePath, service.TempDirectory.BasePath);
        }
        finally
        {
            var secondDir = Path.Combine(
                Path.GetFullPath(secondPath),
                $"{TestAppHostName.ToLowerInvariant()}-{TestAppHostSha[..12].ToLowerInvariant()}");
            if (Directory.Exists(secondDir))
            {
                Directory.Delete(secondDir, recursive: true);
            }
            if (Directory.Exists(secondPath))
            {
                Directory.Delete(secondPath, recursive: true);
            }
            if (Directory.Exists(firstPath))
            {
                Directory.Delete(firstPath, recursive: true);
            }
        }
    }

    [Fact]
    public void TempDirectory_CreateSubdirectory_CreatesDirectoryWithoutPrefix()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var tempDir = service.TempDirectory.CreateSubdirectory();

        try
        {
            Assert.True(Directory.Exists(tempDir));
            Assert.StartsWith(service.TempDirectory.BasePath, tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void TempDirectory_CreateSubdirectory_CreatesDirectoryWithPrefix()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var tempDir = service.TempDirectory.CreateSubdirectory("test-prefix");

        try
        {
            Assert.True(Directory.Exists(tempDir));
            Assert.Contains("test-prefix", Path.GetFileName(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void TempDirectory_GetFilePath_ReturnsUniquePathWithoutExtension()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var path1 = service.TempDirectory.GetFilePath();
        var path2 = service.TempDirectory.GetFilePath();

        Assert.NotEqual(path1, path2);
        Assert.StartsWith(service.TempDirectory.BasePath, path1);
        Assert.StartsWith(service.TempDirectory.BasePath, path2);
    }

    [Fact]
    public void TempDirectory_GetFilePath_ReturnsPathWithExtension()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var path = service.TempDirectory.GetFilePath(".json");

        Assert.EndsWith(".json", path);
        Assert.StartsWith(service.TempDirectory.BasePath, path);
    }

    [Fact]
    public void TempDirectory_GetFilePath_AddsExtensionIfMissingDot()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var path = service.TempDirectory.GetFilePath("txt");

        Assert.EndsWith(".txt", path);
    }

    [Fact]
    public void TempDirectory_GetSubdirectoryPath_ReturnsCombinedPath()
    {
        var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
        var path = service.TempDirectory.GetSubdirectoryPath("my-subdir");

        Assert.Equal(Path.Combine(service.TempDirectory.BasePath, "my-subdir"), path);
    }

    [Fact]
    public void TempDirectory_Configuration_SupportsTildeExpansion()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = "~/custom-temp"
            })
            .Build();

        var service = new AspireDirectoryService(config, TestAppHostName, TestAppHostSha);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.GetFullPath(Path.Combine(
            userProfile, "custom-temp",
            $"{TestAppHostName.ToLowerInvariant()}-{TestAppHostSha[..12].ToLowerInvariant()}"));

        try
        {
            Assert.Equal(expected, service.TempDirectory.BasePath);
        }
        finally
        {
            if (Directory.Exists(expected))
            {
                Directory.Delete(expected, recursive: true);
            }
            var parentDir = Path.Combine(userProfile, "custom-temp");
            if (Directory.Exists(parentDir) && !Directory.EnumerateFileSystemEntries(parentDir).Any())
            {
                Directory.Delete(parentDir);
            }
        }
    }

    [Fact]
    public void TempDirectory_SanitizesInvalidCharactersInAppHostName()
    {
        var invalidName = "Test<>App|Host";
        var service = new AspireDirectoryService(null, invalidName, TestAppHostSha);
        
        // Should replace invalid characters with dashes and convert to lowercase
        var sanitizedName = "test--app-host";
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire", "temp",
            $"{sanitizedName}-{TestAppHostSha[..12].ToLowerInvariant()}");
        
        Assert.Equal(expectedPath, service.TempDirectory.BasePath);
    }
}
