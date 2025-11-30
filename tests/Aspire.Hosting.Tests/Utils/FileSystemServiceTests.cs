// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests.Utils;

public class FileSystemServiceTests : IDisposable
{
    private readonly List<string> _createdDirectories = new();
    private readonly List<string> _createdFiles = new();

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_CreatesDirectory()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire");
        _createdDirectories.Add(subdir);

        // Assert
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_CreatesUniqueDirectories()
    {
        // Arrange
        var directoryService = new FileSystemService();

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
    public void FileSystemService_CreateTempSubdirectory_WithPrefix_IncludesPrefix()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-dcp");
        _createdDirectories.Add(subdir);

        // Assert
        var subdirName = Path.GetFileName(subdir);
        Assert.StartsWith("aspire-dcp", subdirName);
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_WithNullPrefix_UsesDefault()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory(null);
        _createdDirectories.Add(subdir);

        // Assert
        var subdirName = Path.GetFileName(subdir);
        Assert.StartsWith("aspire", subdirName);
        Assert.True(Directory.Exists(subdir));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_UsesSystemTempPath()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var systemTempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-test");
        _createdDirectories.Add(subdir);

        // Assert - the created directory should be under the system temp path
        Assert.StartsWith(systemTempPath, subdir);
    }

    [Fact]
    public void FileSystemService_GetTempFileName_CreatesFile()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName();
        _createdFiles.Add(tempFile);

        // Assert
        Assert.True(File.Exists(tempFile));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_WithNullExtension_CreatesTmpFile()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName(null);
        _createdFiles.Add(tempFile);

        // Assert
        Assert.True(File.Exists(tempFile));
        Assert.Equal(".tmp", Path.GetExtension(tempFile));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_WithExtension_CreatesFileWithExtension()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName(".json");
        _createdFiles.Add(tempFile);

        // Assert
        Assert.True(File.Exists(tempFile));
        Assert.Equal(".json", Path.GetExtension(tempFile));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_CreatesUniqueFiles()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile1 = directoryService.TempDirectory.GetTempFileName();
        var tempFile2 = directoryService.TempDirectory.GetTempFileName();
        _createdFiles.Add(tempFile1);
        _createdFiles.Add(tempFile2);

        // Assert
        Assert.NotEqual(tempFile1, tempFile2);
        Assert.True(File.Exists(tempFile1));
        Assert.True(File.Exists(tempFile2));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_UsesSystemTempPath()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var systemTempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName();
        _createdFiles.Add(tempFile);

        // Assert - the created file should be under the system temp path
        Assert.StartsWith(systemTempPath, tempFile);
    }

    [Fact]
    public void FileSystemService_CreateTempFile_WithName_CreatesFileWithName()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.CreateTempFile("aspire-test", "config.php");
        _createdDirectories.Add(Path.GetDirectoryName(tempFile)!); // Clean up the parent directory

        // Assert
        Assert.True(File.Exists(tempFile));
        Assert.Equal("config.php", Path.GetFileName(tempFile));
    }

    [Fact]
    public void FileSystemService_CreateTempFile_WithName_CreatesUniqueDirectories()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile1 = directoryService.TempDirectory.CreateTempFile("aspire-test", "script.php");
        var tempFile2 = directoryService.TempDirectory.CreateTempFile("aspire-test", "script.php");
        _createdDirectories.Add(Path.GetDirectoryName(tempFile1)!);
        _createdDirectories.Add(Path.GetDirectoryName(tempFile2)!);

        // Assert - files have the same name but are in different directories
        Assert.NotEqual(tempFile1, tempFile2);
        Assert.Equal("script.php", Path.GetFileName(tempFile1));
        Assert.Equal("script.php", Path.GetFileName(tempFile2));
        Assert.True(File.Exists(tempFile1));
        Assert.True(File.Exists(tempFile2));
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

        // Clean up created files
        foreach (var file in _createdFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
