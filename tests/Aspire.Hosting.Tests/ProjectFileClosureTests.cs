// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Tests;

public class ProjectFileClosureTests
{
    [Fact]
    public void GetChangedFiles_NoChanges_ReturnsEmptyList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var filePath = Path.Combine(tempDir, "Program.cs");
            File.WriteAllText(filePath, "// test");

            var closure = new ProjectFileClosure
            {
                ProjectPath = Path.Combine(tempDir, "test.csproj"),
                CapturedAt = DateTime.UtcNow,
                FileTimestamps = new Dictionary<string, DateTime>
                {
                    [filePath] = File.GetLastWriteTimeUtc(filePath)
                }
            };

            var changed = closure.GetChangedFiles();

            Assert.Empty(changed);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetChangedFiles_FileModified_ReturnsChangedFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var filePath = Path.Combine(tempDir, "Program.cs");
            File.WriteAllText(filePath, "// original");

            var originalTime = File.GetLastWriteTimeUtc(filePath);

            var closure = new ProjectFileClosure
            {
                ProjectPath = Path.Combine(tempDir, "test.csproj"),
                CapturedAt = DateTime.UtcNow,
                FileTimestamps = new Dictionary<string, DateTime>
                {
                    [filePath] = originalTime
                }
            };

            // Modify the file with a newer timestamp.
            File.SetLastWriteTimeUtc(filePath, originalTime.AddSeconds(5));

            var changed = closure.GetChangedFiles();

            Assert.Single(changed);
            Assert.Equal(filePath, changed[0]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetChangedFiles_FileDeleted_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var filePath = Path.Combine(tempDir, "Program.cs");
            File.WriteAllText(filePath, "// will be deleted");

            var closure = new ProjectFileClosure
            {
                ProjectPath = Path.Combine(tempDir, "test.csproj"),
                CapturedAt = DateTime.UtcNow,
                FileTimestamps = new Dictionary<string, DateTime>
                {
                    [filePath] = File.GetLastWriteTimeUtc(filePath)
                }
            };

            File.Delete(filePath);

            // Should not throw and should not report as changed (file no longer exists).
            var changed = closure.GetChangedFiles();
            Assert.Empty(changed);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetChangedFiles_MultipleFiles_ReturnsOnlyChanged()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var file1 = Path.Combine(tempDir, "File1.cs");
            var file2 = Path.Combine(tempDir, "File2.cs");
            var file3 = Path.Combine(tempDir, "File3.cs");

            File.WriteAllText(file1, "// file1");
            File.WriteAllText(file2, "// file2");
            File.WriteAllText(file3, "// file3");

            var closure = new ProjectFileClosure
            {
                ProjectPath = Path.Combine(tempDir, "test.csproj"),
                CapturedAt = DateTime.UtcNow,
                FileTimestamps = new Dictionary<string, DateTime>
                {
                    [file1] = File.GetLastWriteTimeUtc(file1),
                    [file2] = File.GetLastWriteTimeUtc(file2),
                    [file3] = File.GetLastWriteTimeUtc(file3)
                }
            };

            // Modify only file2.
            File.SetLastWriteTimeUtc(file2, DateTime.UtcNow.AddMinutes(1));

            var changed = closure.GetChangedFiles();

            Assert.Single(changed);
            Assert.Equal(file2, changed[0]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetChangedFiles_EmptyClosure_ReturnsEmptyList()
    {
        var closure = new ProjectFileClosure
        {
            ProjectPath = "/nonexistent/test.csproj",
            CapturedAt = DateTime.UtcNow,
            FileTimestamps = new Dictionary<string, DateTime>()
        };

        var changed = closure.GetChangedFiles();

        Assert.Empty(changed);
    }

    [Fact]
    public void GetChangedFiles_NonExistentFile_ReturnsEmpty()
    {
        var closure = new ProjectFileClosure
        {
            ProjectPath = "/nonexistent/test.csproj",
            CapturedAt = DateTime.UtcNow,
            FileTimestamps = new Dictionary<string, DateTime>
            {
                ["/nonexistent/path/Program.cs"] = DateTime.UtcNow
            }
        };

        // Non-existent file should not be reported as changed — it's just not present.
        var changed = closure.GetChangedFiles();
        Assert.Empty(changed);
    }
}
