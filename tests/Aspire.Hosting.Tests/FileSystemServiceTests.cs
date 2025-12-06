// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREFILESYSTEM001 // Type is for evaluation purposes only

namespace Aspire.Hosting.Tests;

public class FileSystemServiceTests
{
    [Fact]
    public void CreateTempSubdirectory_CreatesDirectory()
    {
        var service = new FileSystemService();

        using var tempDir = service.TempDirectory.CreateTempSubdirectory();

        Assert.NotNull(tempDir.Path);
        Assert.True(Directory.Exists(tempDir.Path));
    }

    [Fact]
    public void CreateTempSubdirectory_WithPrefix_CreatesDirectoryWithPrefix()
    {
        var service = new FileSystemService();

        using var tempDir = service.TempDirectory.CreateTempSubdirectory("test-prefix");

        Assert.NotNull(tempDir.Path);
        Assert.True(Directory.Exists(tempDir.Path));
        Assert.Contains("test-prefix", tempDir.Path);
    }

    [Fact]
    public void CreateTempSubdirectory_Dispose_DeletesDirectory()
    {
        var service = new FileSystemService();
        var tempDir = service.TempDirectory.CreateTempSubdirectory();
        var path = tempDir.Path;

        Assert.True(Directory.Exists(path));

        tempDir.Dispose();

        Assert.False(Directory.Exists(path));
    }

    [Fact]
    public void CreateTempFile_CreatesFile()
    {
        var service = new FileSystemService();

        using var tempFile = service.TempDirectory.CreateTempFile();

        Assert.NotNull(tempFile.Path);
        Assert.True(File.Exists(tempFile.Path));
    }

    [Fact]
    public void CreateTempFile_WithFileName_CreatesNamedFile()
    {
        var service = new FileSystemService();

        using var tempFile = service.TempDirectory.CreateTempFile("config.json");

        Assert.NotNull(tempFile.Path);
        Assert.True(File.Exists(tempFile.Path));
        Assert.EndsWith("config.json", tempFile.Path);
    }

    [Fact]
    public void CreateTempFile_Dispose_DeletesFile()
    {
        var service = new FileSystemService();
        var tempFile = service.TempDirectory.CreateTempFile();
        var path = tempFile.Path;

        Assert.True(File.Exists(path));

        tempFile.Dispose();

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void CreateTempFile_WithFileName_Dispose_DeletesFileAndParentDirectory()
    {
        var service = new FileSystemService();
        var tempFile = service.TempDirectory.CreateTempFile("test-file.txt");
        var filePath = tempFile.Path;
        var parentDir = Path.GetDirectoryName(filePath);

        Assert.True(File.Exists(filePath));
        Assert.True(Directory.Exists(parentDir));

        tempFile.Dispose();

        Assert.False(File.Exists(filePath));
        Assert.False(Directory.Exists(parentDir));
    }

    [Fact]
    public void PathExtractionPattern_DirectoryPersistsAfterScopeEnds()
    {
        // This test verifies the intentional pattern of extracting .Path
        // without disposing, which is common in the codebase
        var service = new FileSystemService();
        string? extractedPath;

        // Simulate the common pattern: extract path, let object go out of scope
        {
            var tempDir = service.TempDirectory.CreateTempSubdirectory("path-extraction-test");
            extractedPath = tempDir.Path;
            // Note: intentionally NOT disposing here - this is the pattern used in production
        }

        // The directory should still exist because we didn't dispose
        Assert.True(Directory.Exists(extractedPath));

        // Clean up manually for test hygiene
        Directory.Delete(extractedPath, recursive: true);
    }

    [Fact]
    public void PathExtractionPattern_FilePersistsAfterScopeEnds()
    {
        // This test verifies the intentional pattern of extracting .Path
        // without disposing, which is common in the codebase
        var service = new FileSystemService();
        string? extractedPath;

        // Simulate the common pattern: extract path, let object go out of scope
        {
            var tempFile = service.TempDirectory.CreateTempFile();
            extractedPath = tempFile.Path;
            // Note: intentionally NOT disposing here - this is the pattern used in production
        }

        // The file should still exist because we didn't dispose
        Assert.True(File.Exists(extractedPath));

        // Clean up manually for test hygiene
        File.Delete(extractedPath);
    }

    [Fact]
    public void TempDirectory_Property_ReturnsSameInstance()
    {
        var service = new FileSystemService();

        var tempDir1 = service.TempDirectory;
        var tempDir2 = service.TempDirectory;

        Assert.Same(tempDir1, tempDir2);
    }

    [Fact]
    public void CreateTempSubdirectory_MultipleCallsCreateDifferentDirectories()
    {
        var service = new FileSystemService();

        using var tempDir1 = service.TempDirectory.CreateTempSubdirectory();
        using var tempDir2 = service.TempDirectory.CreateTempSubdirectory();

        Assert.NotEqual(tempDir1.Path, tempDir2.Path);
        Assert.True(Directory.Exists(tempDir1.Path));
        Assert.True(Directory.Exists(tempDir2.Path));
    }

    [Fact]
    public void CreateTempFile_MultipleCallsCreateDifferentFiles()
    {
        var service = new FileSystemService();

        using var tempFile1 = service.TempDirectory.CreateTempFile();
        using var tempFile2 = service.TempDirectory.CreateTempFile();

        Assert.NotEqual(tempFile1.Path, tempFile2.Path);
        Assert.True(File.Exists(tempFile1.Path));
        Assert.True(File.Exists(tempFile2.Path));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var service = new FileSystemService();
        var tempDir = service.TempDirectory.CreateTempSubdirectory();
        var tempFile = service.TempDirectory.CreateTempFile();

        // First dispose
        tempDir.Dispose();
        tempFile.Dispose();

        // Second dispose should not throw
        tempDir.Dispose();
        tempFile.Dispose();
    }
}
