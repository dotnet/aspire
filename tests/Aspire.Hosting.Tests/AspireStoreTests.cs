// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AspireStoreTests
{
    [Fact]
    public void Create_ShouldInitializeStore()
    {
        var store = CreateStore();

        Assert.NotNull(store);
        Assert.True(Directory.Exists(Path.GetDirectoryName(store.BasePath)));
    }

    [Fact]
    public void BasePath_ShouldBeAbsolute()
    {
        var store = CreateStore();

        var path = store.BasePath;

        Assert.True(Path.IsPathRooted(path));
    }

    [Fact]
    public void BasePath_ShouldUseConfiguration()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration[AspireStoreExtensions.AspireStorePathKeyName] = Path.GetTempPath();
        var store = builder.CreateStore();

        var path = store.BasePath;

        Assert.DoesNotContain($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", path);
        Assert.Contains(Path.GetTempPath(), path);
    }

    [Fact]
    public void BasePath_ShouldBePrefixed_WhenUsingConfiguration()
    {
        var store = CreateStore();

        var path = store.BasePath;

        Assert.Contains(".aspire", path);
    }

    [Fact]
    public void GetFileName_ShouldNotCreateFile()
    {
        var store = CreateStore();

        var filename = "testfile1.txt";
        var filePath = store.GetFileName(filename);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public void GetOrCreateFileWithContent_ShouldCreateFile_WithStreamContent()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration[AspireStoreExtensions.AspireStorePathKeyName] = Path.GetTempPath();
        var store = builder.CreateStore();

        var filename = "testfile2.txt";
        var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test content"));
        var filePath = store.GetFileNameWithContent(filename, content);

        Assert.True(File.Exists(filePath));
        Assert.Equal("Test content", File.ReadAllText(filePath));
    }

    [Fact]
    public void GetOrCreateFileWithContent_ShouldCreateFile_WithFileContent()
    {
        var store = CreateStore();

        var filename = "testfile2.txt";
        var tempFilename = Path.GetTempFileName();
        File.WriteAllText(tempFilename, "Test content");
        var filePath = store.GetFileNameWithContent(filename, tempFilename);

        Assert.True(File.Exists(filePath));
        Assert.Equal("Test content", File.ReadAllText(filePath));

        try
        {
            File.Delete(tempFilename);
        }
        catch
        {
        }
    }

    [Fact]
    public void GetOrCreateFileWithContent_ShouldNotRecreateFile()
    {
        var store = CreateStore();

        var filename = "testfile3.txt";
        var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Test content"));
        var filePath = store.GetFileNameWithContent(filename, content);

        File.WriteAllText(filePath, "updated");

        content.Position = 0;
        var filePath2 = store.GetFileNameWithContent(filename, content);
        var content2 = File.ReadAllText(filePath2);

        Assert.Equal("updated", content2);
    }

    private static IAspireStore CreateStore()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration[AspireStoreExtensions.AspireStorePathKeyName] = Path.GetTempPath();
        var store = builder.CreateStore();
        return store;
    }
}
