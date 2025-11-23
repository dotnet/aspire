// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
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
            $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
        
        Assert.Equal(expected, service.TempDirectory.BasePath);
    }

    [Fact]
    public void TempDirectory_RespectsEnvironmentVariable()
    {
        var customPath = Path.Combine(Path.GetTempPath(), "custom-aspire-temp");
        try
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", customPath);
            var service = new AspireDirectoryService(null, TestAppHostName, TestAppHostSha);
            
            var expectedBasePath = Path.Combine(
                Path.GetFullPath(customPath),
                $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
            
            Assert.Equal(expectedBasePath, service.TempDirectory.BasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", null);
            // Clean up the directory if it exists
            var expectedDir = Path.Combine(
                Path.GetFullPath(customPath),
                $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
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
    public void TempDirectory_RespectsConfiguration()
    {
        var customPath = Path.Combine(Path.GetTempPath(), "config-aspire-temp");
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
                $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
            
            Assert.Equal(expectedBasePath, service.TempDirectory.BasePath);
        }
        finally
        {
            var expectedDir = Path.Combine(
                Path.GetFullPath(customPath),
                $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
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
    public void TempDirectory_EnvironmentVariableHasPriorityOverConfiguration()
    {
        var envPath = Path.Combine(Path.GetTempPath(), "env-aspire-temp");
        var configPath = Path.Combine(Path.GetTempPath(), "config-aspire-temp");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = configPath
            })
            .Build();

        try
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", envPath);
            var service = new AspireDirectoryService(config, TestAppHostName, TestAppHostSha);
            
            var expectedBasePath = Path.Combine(
                Path.GetFullPath(envPath),
                $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
            
            Assert.Equal(expectedBasePath, service.TempDirectory.BasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", null);
            var envDir = Path.Combine(
                Path.GetFullPath(envPath),
                $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}");
            if (Directory.Exists(envDir))
            {
                Directory.Delete(envDir, recursive: true);
            }
            if (Directory.Exists(envPath))
            {
                Directory.Delete(envPath, recursive: true);
            }
            if (Directory.Exists(configPath))
            {
                Directory.Delete(configPath, recursive: true);
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
            $"{TestAppHostName}-{TestAppHostSha[..12].ToLowerInvariant()}"));

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
        
        // Should replace invalid characters with dashes
        var sanitizedName = "Test--App-Host";
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire", "temp",
            $"{sanitizedName}-{TestAppHostSha[..12].ToLowerInvariant()}");
        
        Assert.Equal(expectedPath, service.TempDirectory.BasePath);
    }
}
