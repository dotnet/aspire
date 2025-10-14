// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class FileSystemHelperTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void CopyDirectory_WithSimpleFiles_CopiesAllFiles()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Create some test files
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file2.txt"), "content2");
        File.WriteAllText(Path.Combine(sourceDir.FullName, "file3.cs"), "using System;");

        // Act
        FileSystemHelper.CopyDirectory(sourceDir.FullName, destDir);

        // Assert
        Assert.True(Directory.Exists(destDir));
        Assert.True(File.Exists(Path.Combine(destDir, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(destDir, "file2.txt")));
        Assert.True(File.Exists(Path.Combine(destDir, "file3.cs")));
        
        Assert.Equal("content1", File.ReadAllText(Path.Combine(destDir, "file1.txt")));
        Assert.Equal("content2", File.ReadAllText(Path.Combine(destDir, "file2.txt")));
        Assert.Equal("using System;", File.ReadAllText(Path.Combine(destDir, "file3.cs")));
    }

    [Fact]
    public void CopyDirectory_WithSubdirectories_CopiesRecursively()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Create nested directory structure
        var subDir1 = sourceDir.CreateSubdirectory("subdir1");
        var subDir2 = subDir1.CreateSubdirectory("subdir2");
        
        File.WriteAllText(Path.Combine(sourceDir.FullName, "root.txt"), "root content");
        File.WriteAllText(Path.Combine(subDir1.FullName, "level1.txt"), "level 1 content");
        File.WriteAllText(Path.Combine(subDir2.FullName, "level2.txt"), "level 2 content");

        // Act
        FileSystemHelper.CopyDirectory(sourceDir.FullName, destDir);

        // Assert
        Assert.True(Directory.Exists(destDir));
        Assert.True(File.Exists(Path.Combine(destDir, "root.txt")));
        Assert.True(Directory.Exists(Path.Combine(destDir, "subdir1")));
        Assert.True(File.Exists(Path.Combine(destDir, "subdir1", "level1.txt")));
        Assert.True(Directory.Exists(Path.Combine(destDir, "subdir1", "subdir2")));
        Assert.True(File.Exists(Path.Combine(destDir, "subdir1", "subdir2", "level2.txt")));
        
        Assert.Equal("root content", File.ReadAllText(Path.Combine(destDir, "root.txt")));
        Assert.Equal("level 1 content", File.ReadAllText(Path.Combine(destDir, "subdir1", "level1.txt")));
        Assert.Equal("level 2 content", File.ReadAllText(Path.Combine(destDir, "subdir1", "subdir2", "level2.txt")));
    }

    [Fact]
    public void CopyDirectory_WithEmptyDirectory_CreatesDestination()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("empty_source");
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "empty_destination");

        // Act
        FileSystemHelper.CopyDirectory(sourceDir.FullName, destDir);

        // Assert
        Assert.True(Directory.Exists(destDir));
        Assert.Empty(Directory.GetFiles(destDir));
        Assert.Empty(Directory.GetDirectories(destDir));
    }

    [Fact]
    public void CopyDirectory_WithNonExistentSource_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var nonExistentSource = Path.Combine(workspace.WorkspaceRoot.FullName, "nonexistent");
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => 
            FileSystemHelper.CopyDirectory(nonExistentSource, destDir));
    }

    [Fact]
    public void CopyDirectory_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            FileSystemHelper.CopyDirectory(null!, destDir));
    }

    [Fact]
    public void CopyDirectory_WithNullDestination_ThrowsArgumentNullException()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            FileSystemHelper.CopyDirectory(sourceDir.FullName, null!));
    }

    [Fact]
    public void CopyDirectory_WithEmptySource_ThrowsArgumentException()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            FileSystemHelper.CopyDirectory(string.Empty, destDir));
    }

    [Fact]
    public void CopyDirectory_WithEmptyDestination_ThrowsArgumentException()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            FileSystemHelper.CopyDirectory(sourceDir.FullName, string.Empty));
    }

    [Fact]
    public void CopyDirectory_PreservesFileContent_WithBinaryFiles()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Create a binary file with random content
        var binaryFilePath = Path.Combine(sourceDir.FullName, "binary.dat");
        var randomBytes = new byte[1024];
        Random.Shared.NextBytes(randomBytes);
        File.WriteAllBytes(binaryFilePath, randomBytes);

        // Act
        FileSystemHelper.CopyDirectory(sourceDir.FullName, destDir);

        // Assert
        var copiedFilePath = Path.Combine(destDir, "binary.dat");
        Assert.True(File.Exists(copiedFilePath));
        
        var copiedBytes = File.ReadAllBytes(copiedFilePath);
        Assert.Equal(randomBytes, copiedBytes);
    }

    [Fact]
    public void CopyDirectory_WithMultipleLevelsOfSubdirectories_CopiesAll()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var sourceDir = workspace.CreateDirectory("source");
        var destDir = Path.Combine(workspace.WorkspaceRoot.FullName, "destination");

        // Create a deep directory structure
        var current = sourceDir;
        for (int i = 0; i < 5; i++)
        {
            current = current.CreateSubdirectory($"level{i}");
            File.WriteAllText(Path.Combine(current.FullName, $"file{i}.txt"), $"content at level {i}");
        }

        // Act
        FileSystemHelper.CopyDirectory(sourceDir.FullName, destDir);

        // Assert
        var currentDest = destDir;
        for (int i = 0; i < 5; i++)
        {
            currentDest = Path.Combine(currentDest, $"level{i}");
            Assert.True(Directory.Exists(currentDest));
            var filePath = Path.Combine(currentDest, $"file{i}.txt");
            Assert.True(File.Exists(filePath));
            Assert.Equal($"content at level {i}", File.ReadAllText(filePath));
        }
    }
}
