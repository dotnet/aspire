// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Tests.Utils;

public class AspireTempDirectoryServiceTests
{
    [Fact]
    public void BaseTempDirectory_ReturnsDefaultLocation()
    {
        var service = new AspireTempDirectoryService();
        var expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspire", "temp");
        Assert.Equal(expected, service.BaseTempDirectory);
    }

    [Fact]
    public void BaseTempDirectory_RespectsEnvironmentVariable()
    {
        var customPath = Path.Combine(Path.GetTempPath(), "custom-aspire-temp");
        try
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", customPath);
            var service = new AspireTempDirectoryService();
            Assert.Equal(Path.GetFullPath(customPath), service.BaseTempDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", null);
            if (Directory.Exists(customPath))
            {
                Directory.Delete(customPath, recursive: true);
            }
        }
    }

    [Fact]
    public void BaseTempDirectory_RespectsConfiguration()
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
            var service = new AspireTempDirectoryService(config);
            Assert.Equal(Path.GetFullPath(customPath), service.BaseTempDirectory);
        }
        finally
        {
            if (Directory.Exists(customPath))
            {
                Directory.Delete(customPath, recursive: true);
            }
        }
    }

    [Fact]
    public void BaseTempDirectory_EnvironmentVariableHasPriorityOverConfiguration()
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
            var service = new AspireTempDirectoryService(config);
            Assert.Equal(Path.GetFullPath(envPath), service.BaseTempDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPIRE_TEMP_FOLDER", null);
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
    public void CreateTempSubdirectory_CreatesDirectoryWithoutPrefix()
    {
        var service = new AspireTempDirectoryService();
        var tempDir = service.CreateTempSubdirectory();

        try
        {
            Assert.True(Directory.Exists(tempDir));
            Assert.StartsWith(service.BaseTempDirectory, tempDir);
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
    public void CreateTempSubdirectory_CreatesDirectoryWithPrefix()
    {
        var service = new AspireTempDirectoryService();
        var tempDir = service.CreateTempSubdirectory("test-prefix");

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
    public void GetTempFilePath_ReturnsUniquePathWithoutExtension()
    {
        var service = new AspireTempDirectoryService();
        var path1 = service.GetTempFilePath();
        var path2 = service.GetTempFilePath();

        Assert.NotEqual(path1, path2);
        Assert.StartsWith(service.BaseTempDirectory, path1);
        Assert.StartsWith(service.BaseTempDirectory, path2);
    }

    [Fact]
    public void GetTempFilePath_ReturnsPathWithExtension()
    {
        var service = new AspireTempDirectoryService();
        var path = service.GetTempFilePath(".json");

        Assert.EndsWith(".json", path);
        Assert.StartsWith(service.BaseTempDirectory, path);
    }

    [Fact]
    public void GetTempFilePath_AddsExtensionIfMissingDot()
    {
        var service = new AspireTempDirectoryService();
        var path = service.GetTempFilePath("txt");

        Assert.EndsWith(".txt", path);
    }

    [Fact]
    public void GetTempSubdirectoryPath_ReturnsCombinedPath()
    {
        var service = new AspireTempDirectoryService();
        var path = service.GetTempSubdirectoryPath("my-subdir");

        Assert.Equal(Path.Combine(service.BaseTempDirectory, "my-subdir"), path);
    }

    [Fact]
    public void Configuration_SupportsTildeExpansion()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = "~/custom-temp"
            })
            .Build();

        var service = new AspireTempDirectoryService(config);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.GetFullPath(Path.Combine(userProfile, "custom-temp"));

        try
        {
            Assert.Equal(expected, service.BaseTempDirectory);
        }
        finally
        {
            if (Directory.Exists(expected))
            {
                Directory.Delete(expected, recursive: true);
            }
        }
    }
}
