// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Tests.Utils;

public class DirectoryServiceTests : IDisposable
{
    private readonly List<string> _createdDirectories = new();

    [Fact]
    public void DirectoryService_CreatesBasePath_UnderAspireTemp()
    {
        // Arrange
        var appHostName = "TestAppHost";
        var appHostSha = "1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF";

        // Act
        var directoryService = new DirectoryService(null, appHostName, appHostSha);
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Assert
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expectedRoot = Path.Combine(userProfile, ".aspire", "temp");

        Assert.StartsWith(expectedRoot, directoryService.TempDirectory.BasePath);
        Assert.Contains("testapphost-1234567890ab", directoryService.TempDirectory.BasePath);
        Assert.True(Directory.Exists(directoryService.TempDirectory.BasePath));
    }

    [Fact]
    public void DirectoryService_CreateSubdirectory_CreatesUniqueDirectory()
    {
        // Arrange
        var directoryService = new DirectoryService(null, "TestAppHost", "ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890");
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Act
        var subdir1 = directoryService.TempDirectory.CreateSubdirectory();
        var subdir2 = directoryService.TempDirectory.CreateSubdirectory();

        // Assert
        Assert.NotEqual(subdir1, subdir2);
        Assert.True(Directory.Exists(subdir1));
        Assert.True(Directory.Exists(subdir2));
        Assert.StartsWith(directoryService.TempDirectory.BasePath, subdir1);
        Assert.StartsWith(directoryService.TempDirectory.BasePath, subdir2);
    }

    [Fact]
    public void DirectoryService_CreateSubdirectory_WithPrefix_IncludesPrefix()
    {
        // Arrange
        var directoryService = new DirectoryService(null, "TestAppHost", "FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321FEDCBA0987654321");
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Act
        var subdir = directoryService.TempDirectory.CreateSubdirectory("dcp");

        // Assert
        var subdirName = Path.GetFileName(subdir);
        Assert.StartsWith("dcp-", subdirName);
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void DirectoryService_GetFilePath_ReturnsPathWithExtension()
    {
        // Arrange
        var directoryService = new DirectoryService(null, "TestAppHost", "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF");
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Act
        var filePath1 = directoryService.TempDirectory.GetFilePath(".json");
        var filePath2 = directoryService.TempDirectory.GetFilePath("json"); // without dot

        // Assert
        Assert.EndsWith(".json", filePath1);
        Assert.EndsWith(".json", filePath2);
        Assert.StartsWith(directoryService.TempDirectory.BasePath, filePath1);
        Assert.StartsWith(directoryService.TempDirectory.BasePath, filePath2);
        Assert.NotEqual(filePath1, filePath2); // Each call should return unique path
    }

    [Fact]
    public void DirectoryService_GetFilePath_WithNullExtension_ReturnsPathWithoutExtension()
    {
        // Arrange
        var directoryService = new DirectoryService(null, "TestAppHost", "AABBCCDD11223344AABBCCDD11223344AABBCCDD11223344AABBCCDD11223344");
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Act
        var filePath = directoryService.TempDirectory.GetFilePath(null);

        // Assert
        Assert.StartsWith(directoryService.TempDirectory.BasePath, filePath);
        Assert.DoesNotContain(".", Path.GetFileName(filePath));
    }

    [Fact]
    public void DirectoryService_CreateSubdirectoryPath_CreatesAndReturnsCorrectPath()
    {
        // Arrange
        var directoryService = new DirectoryService(null, "TestAppHost", "5566778899AABBCC5566778899AABBCC5566778899AABBCC5566778899AABBCC");
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Act
        var subdirPath = directoryService.TempDirectory.CreateSubdirectoryPath("azure");

        // Assert
        Assert.Equal(Path.Combine(directoryService.TempDirectory.BasePath, "azure"), subdirPath);
        // CreateSubdirectoryPath creates the directory
        Assert.True(Directory.Exists(subdirPath));
    }

    [Fact]
    public void DirectoryService_WithConfigOverride_UsesConfiguredPath()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "aspire-test-custom-" + Guid.NewGuid().ToString("N"));
        _createdDirectories.Add(tempPath);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = tempPath
            })
            .Build();

        // Act
        var directoryService = new DirectoryService(configuration, "TestAppHost", "DEADBEEF12345678DEADBEEF12345678DEADBEEF12345678DEADBEEF12345678");

        // Assert
        Assert.StartsWith(tempPath, directoryService.TempDirectory.BasePath);
    }

    [Fact]
    public void DirectoryService_WithEnvironmentVariableOverride_UsesConfiguredPath()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "aspire-test-env-" + Guid.NewGuid().ToString("N"));
        _createdDirectories.Add(tempPath);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPIRE_TEMP_FOLDER"] = tempPath
            })
            .Build();

        // Act
        var directoryService = new DirectoryService(configuration, "TestAppHost", "CAFEBABE87654321CAFEBABE87654321CAFEBABE87654321CAFEBABE87654321");

        // Assert
        Assert.StartsWith(tempPath, directoryService.TempDirectory.BasePath);
    }

    [Fact]
    public void DirectoryService_SanitizesAppHostName()
    {
        // Arrange - AppHost name with forward slash (invalid on all platforms)
        var appHostName = "Test/AppHost";
        var appHostSha = "1111222233334444555566667777888811112222333344445555666677778888";

        // Act
        var directoryService = new DirectoryService(null, appHostName, appHostSha);
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Assert - Should not throw and directory should exist
        // This verifies that the sanitization was effective on the current platform
        Assert.True(Directory.Exists(directoryService.TempDirectory.BasePath));

        // The directory name should be lowercase
        var dirName = Path.GetFileName(directoryService.TempDirectory.BasePath);
        Assert.Equal(dirName.ToLowerInvariant(), dirName);

        // The forward slash should have been replaced (it's a path separator on all platforms)
        Assert.DoesNotContain("/", dirName);
    }

    [Fact]
    public void DirectoryService_DirectoryNamesAreLowercase()
    {
        // Arrange
        var appHostName = "MyAppHost";
        var appHostSha = "AABBCCDD11223344AABBCCDD11223344AABBCCDD11223344AABBCCDD11223344";

        // Act
        var directoryService = new DirectoryService(null, appHostName, appHostSha);
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Assert
        var dirName = Path.GetFileName(directoryService.TempDirectory.BasePath);
        Assert.Equal(dirName.ToLowerInvariant(), dirName);
        Assert.Contains("myapphost-", dirName);
        Assert.Contains("aabbccdd1122", dirName);
    }

    [Fact]
    public void DirectoryService_WithTildeInPath_ExpandsHomeDirectory()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Aspire:TempDirectory"] = "~/.aspire-test-tilde"
            })
            .Build();

        // Act
        var directoryService = new DirectoryService(configuration, "TestAppHost", "EEEEFFFFAAAABBBBEEEEFFFFAAAABBBBEEEEFFFFAAAABBBBEEEEFFFFAAAABBBB");
        _createdDirectories.Add(directoryService.TempDirectory.BasePath);

        // Assert
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.StartsWith(userProfile, directoryService.TempDirectory.BasePath);
        Assert.DoesNotContain("~", directoryService.TempDirectory.BasePath);
    }

    public void Dispose()
    {
        // Clean up created directories
        foreach (var dir in _createdDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
