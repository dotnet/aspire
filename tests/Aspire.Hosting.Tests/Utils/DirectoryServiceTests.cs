// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public class DirectoryServiceTests : IDisposable
{
    private readonly List<string> _createdDirectories = new();

    [Fact]
    public void DirectoryService_CreateTempSubdirectory_CreatesDirectory()
    {
        // Arrange
        var directoryService = new DirectoryService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire");
        _createdDirectories.Add(subdir);

        // Assert
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void DirectoryService_CreateTempSubdirectory_CreatesUniqueDirectories()
    {
        // Arrange
        var directoryService = new DirectoryService();

        // Act
        var subdir1 = directoryService.TempDirectory.CreateTempSubdirectory("aspire");
        var subdir2 = directoryService.TempDirectory.CreateTempSubdirectory("aspire");
        _createdDirectories.Add(subdir1);
        _createdDirectories.Add(subdir2);

        // Assert
        Assert.NotEqual(subdir1, subdir2);
        Assert.True(Directory.Exists(subdir1));
        Assert.True(Directory.Exists(subdir2));
    }

    [Fact]
    public void DirectoryService_CreateTempSubdirectory_WithPrefix_IncludesPrefix()
    {
        // Arrange
        var directoryService = new DirectoryService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-dcp");
        _createdDirectories.Add(subdir);

        // Assert
        var subdirName = Path.GetFileName(subdir);
        Assert.StartsWith("aspire-dcp", subdirName);
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void DirectoryService_CreateTempSubdirectory_WithNullPrefix_UsesDefault()
    {
        // Arrange
        var directoryService = new DirectoryService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory(null);
        _createdDirectories.Add(subdir);

        // Assert
        var subdirName = Path.GetFileName(subdir);
        Assert.StartsWith("aspire", subdirName);
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void DirectoryService_CreateTempSubdirectory_UsesSystemTempPath()
    {
        // Arrange
        var directoryService = new DirectoryService();
        var systemTempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-test");
        _createdDirectories.Add(subdir);

        // Assert - the created directory should be under the system temp path
        Assert.StartsWith(systemTempPath, subdir);
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
