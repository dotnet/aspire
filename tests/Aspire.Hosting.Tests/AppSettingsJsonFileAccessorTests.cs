// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Aspire.Hosting.Tests;

public class AppSettingsJsonFileAccessorTests
{
    private const string TestProjectPath = @"C:\Projects\TestProject\test.csproj";
    private const string TestEnvironmentName = "Development";
    private const string TestAppSettingsFilePath = @"C:\Projects\TestProject\wwwroot\appsettings.Development.json";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange & Act
        var fileSystemMock = new MockFileSystem();

        // Act & Assert
        var accessor = new AppSettingsJsonFileAccessor(TestProjectPath, TestEnvironmentName, fileSystemMock);
        Assert.NotNull(accessor);
    }

    [Fact]
    public void Constructor_NullProjectPath_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemMock = new MockFileSystem();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AppSettingsJsonFileAccessor(null!, TestEnvironmentName, fileSystemMock));
    }

    [Fact]
    public void Constructor_NullEnvironmentName_ThrowsArgumentNullException()
    {
        // Arrange
        var fileSystemMock = new MockFileSystem();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AppSettingsJsonFileAccessor(TestProjectPath, null!, fileSystemMock));
    }

    [Fact]
    public void ReadFileAsJson_FileExists_ReturnsJsonObject()
    {
        // Arrange
        var jsonContent = new JsonObject { ["Key"] = "Value" };
        var serializedContent = JsonSerializer.Serialize(jsonContent, _jsonSerializerOptions);

        var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { TestAppSettingsFilePath, new MockFileData(serializedContent)},
        });

        var accessor = new AppSettingsJsonFileAccessor(TestProjectPath, TestEnvironmentName, fileSystemMock);

        // Act
        var result = accessor.ReadFileAsJson();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        Assert.Equal("Value", result["Key"]?.ToString());
    }

    [Fact]
    public void ReadFileAsJson_FileDoesNotExist_CreatesAndReturnsEmptyJsonObject()
    {
        // Arrange
        var fileSystemMock = new MockFileSystem();
        var accessor = new AppSettingsJsonFileAccessor(TestProjectPath, TestEnvironmentName, fileSystemMock);

        // Act
        var result = accessor.ReadFileAsJson();

        // Assert
        Assert.NotNull(result);
        Assert.True(fileSystemMock.FileExists(TestAppSettingsFilePath));
        Assert.Equal(new JsonObject(), result);
    }

    [Fact]
    public void SaveJson_ValidJsonObject_SavesToFile()
    {
        // Arrange
        var jsonContent = new JsonObject { ["Key"] = "Value" };
        var serializedContent = jsonContent.ToJsonString(_jsonSerializerOptions);

        var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>());

        var accessor = new AppSettingsJsonFileAccessor(TestProjectPath, TestEnvironmentName, fileSystemMock);

        // Act
        accessor.SaveJson(jsonContent);
        var actual = JsonSerializer.Deserialize<JsonObject>(fileSystemMock.GetFile(TestAppSettingsFilePath).TextContents, _jsonSerializerOptions)!.ToJsonString(_jsonSerializerOptions);

        // Assert
        Assert.True(fileSystemMock.FileExists(TestAppSettingsFilePath));
        Assert.Equal(serializedContent, actual);
    }

    [Fact]
    public void EnsureAppSettingsFilePath_DirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var fileSystemMock = new MockFileSystem();
        var accessor = new AppSettingsJsonFileAccessor(TestProjectPath, TestEnvironmentName, fileSystemMock);

        // Act
        accessor.ReadFileAsJson(); // Triggers EnsureAppSettingsFilePath

        // Assert
        Assert.True(fileSystemMock.FileExists(TestAppSettingsFilePath));
    }
}
