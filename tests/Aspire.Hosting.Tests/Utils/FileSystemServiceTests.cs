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
        _createdDirectories.Add(subdir.Path);

        // Assert
        Assert.True(Directory.Exists(subdir.Path));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_CreatesUniqueDirectories()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var subdir1 = directoryService.TempDirectory.CreateTempSubdirectory("aspire");
        var subdir2 = directoryService.TempDirectory.CreateTempSubdirectory("aspire");
        _createdDirectories.Add(subdir1.Path);
        _createdDirectories.Add(subdir2.Path);

        // Assert
        Assert.NotEqual(subdir1.Path, subdir2.Path);
        Assert.True(Directory.Exists(subdir1.Path));
        Assert.True(Directory.Exists(subdir2.Path));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_WithPrefix_IncludesPrefix()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-dcp");
        _createdDirectories.Add(subdir.Path);

        // Assert
        var subdirName = Path.GetFileName(subdir.Path);
        Assert.StartsWith("aspire-dcp", subdirName);
        Assert.True(Directory.Exists(subdir.Path));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_WithNullPrefix_UsesDefault()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory(null);
        _createdDirectories.Add(subdir.Path);

        // Assert
        var subdirName = Path.GetFileName(subdir.Path);
        Assert.StartsWith("aspire", subdirName);
        Assert.True(Directory.Exists(subdir.Path));
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_UsesSystemTempPath()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var systemTempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Act
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-test");
        _createdDirectories.Add(subdir.Path);

        // Assert - the created directory should be under the system temp path
        Assert.StartsWith(systemTempPath, subdir.Path);
    }

    [Fact]
    public void FileSystemService_CreateTempSubdirectory_Dispose_DeletesDirectory()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var subdir = directoryService.TempDirectory.CreateTempSubdirectory("aspire-dispose-test");
        var path = subdir.Path;

        // Act
        Assert.True(Directory.Exists(path));
        subdir.Dispose();

        // Assert
        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_CreatesFile()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName();
        _createdFiles.Add(tempFile.Path);

        // Assert
        Assert.True(File.Exists(tempFile.Path));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_WithNullExtension_CreatesTmpFile()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName(null);
        _createdFiles.Add(tempFile.Path);

        // Assert
        Assert.True(File.Exists(tempFile.Path));
        Assert.Equal(".tmp", Path.GetExtension(tempFile.Path));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_WithExtension_CreatesFileWithExtension()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName(".json");
        _createdFiles.Add(tempFile.Path);

        // Assert
        Assert.True(File.Exists(tempFile.Path));
        Assert.Equal(".json", Path.GetExtension(tempFile.Path));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_CreatesUniqueFiles()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile1 = directoryService.TempDirectory.GetTempFileName();
        var tempFile2 = directoryService.TempDirectory.GetTempFileName();
        _createdFiles.Add(tempFile1.Path);
        _createdFiles.Add(tempFile2.Path);

        // Assert
        Assert.NotEqual(tempFile1.Path, tempFile2.Path);
        Assert.True(File.Exists(tempFile1.Path));
        Assert.True(File.Exists(tempFile2.Path));
    }

    [Fact]
    public void FileSystemService_GetTempFileName_UsesSystemTempPath()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var systemTempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Act
        var tempFile = directoryService.TempDirectory.GetTempFileName();
        _createdFiles.Add(tempFile.Path);

        // Assert - the created file should be under the system temp path
        Assert.StartsWith(systemTempPath, tempFile.Path);
    }

    [Fact]
    public void FileSystemService_GetTempFileName_Dispose_DeletesFile()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var tempFile = directoryService.TempDirectory.GetTempFileName();
        var path = tempFile.Path;

        // Act
        Assert.True(File.Exists(path));
        tempFile.Dispose();

        // Assert
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void FileSystemService_CreateTempFile_WithName_CreatesFileWithName()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile = directoryService.TempDirectory.CreateTempFile("aspire-test", "config.php");
        _createdDirectories.Add(Path.GetDirectoryName(tempFile.Path)!); // Clean up the parent directory

        // Assert
        Assert.True(File.Exists(tempFile.Path));
        Assert.Equal("config.php", Path.GetFileName(tempFile.Path));
    }

    [Fact]
    public void FileSystemService_CreateTempFile_WithName_CreatesUniqueDirectories()
    {
        // Arrange
        var directoryService = new FileSystemService();

        // Act
        var tempFile1 = directoryService.TempDirectory.CreateTempFile("aspire-test", "script.php");
        var tempFile2 = directoryService.TempDirectory.CreateTempFile("aspire-test", "script.php");
        _createdDirectories.Add(Path.GetDirectoryName(tempFile1.Path)!);
        _createdDirectories.Add(Path.GetDirectoryName(tempFile2.Path)!);

        // Assert - files have the same name but are in different directories
        Assert.NotEqual(tempFile1.Path, tempFile2.Path);
        Assert.Equal("script.php", Path.GetFileName(tempFile1.Path));
        Assert.Equal("script.php", Path.GetFileName(tempFile2.Path));
        Assert.True(File.Exists(tempFile1.Path));
        Assert.True(File.Exists(tempFile2.Path));
    }

    [Fact]
    public void FileSystemService_CreateTempFile_Dispose_DeletesFileAndParentDirectory()
    {
        // Arrange
        var directoryService = new FileSystemService();
        var tempFile = directoryService.TempDirectory.CreateTempFile("aspire-dispose-test", "test.txt");
        var filePath = tempFile.Path;
        var parentDir = Path.GetDirectoryName(filePath)!;

        // Act
        Assert.True(File.Exists(filePath));
        Assert.True(Directory.Exists(parentDir));
        tempFile.Dispose();

        // Assert - both file and parent directory should be deleted
        Assert.False(File.Exists(filePath));
        Assert.False(Directory.Exists(parentDir));
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
