// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Tests.Utils;

public class PathLookupHelperTests
{
    [Fact]
    public void FindFullPathFromPath_WhenCommandExistsOnPath_ReturnsFullPath()
    {
        // Arrange
        var existingFiles = new HashSet<string>
        {
            Path.Combine("/usr/bin", "mycommand")
        };

        // Act
        var result = PathLookupHelper.FindFullPathFromPath("mycommand", "/usr/bin:/usr/local/bin", ':', existingFiles.Contains);

        // Assert
        Assert.Equal(Path.Combine("/usr/bin", "mycommand"), result);
    }

    [Fact]
    public void FindFullPathFromPath_WhenCommandNotOnPath_ReturnsNull()
    {
        // Arrange
        static bool AlwaysFalse(string _) => false;

        // Act
        var result = PathLookupHelper.FindFullPathFromPath("mycommand", "/usr/bin:/usr/local/bin", ':', AlwaysFalse);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindFullPathFromPath_WhenPathVariableIsEmpty_ReturnsNull()
    {
        // Arrange - when path is empty, no files should exist on that (non-existent) path
        static bool AlwaysFalse(string _) => false;

        // Act
        var result = PathLookupHelper.FindFullPathFromPath("mycommand", "", ':', AlwaysFalse);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindFullPathFromPath_WhenPathVariableIsNull_ReturnsNull()
    {
        // Arrange
        static bool AlwaysFalse(string _) => false;

        // Act
        var result = PathLookupHelper.FindFullPathFromPath("mycommand", null, ':', AlwaysFalse);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindFullPathFromPath_ReturnsFirstMatchFromPath()
    {
        // Arrange
        var existingFiles = new HashSet<string>
        {
            Path.Combine("/first/path", "mycommand"),
            Path.Combine("/second/path", "mycommand")
        };

        // Act
        var result = PathLookupHelper.FindFullPathFromPath("mycommand", "/first/path:/second/path", ':', existingFiles.Contains);

        // Assert
        Assert.Equal(Path.Combine("/first/path", "mycommand"), result);
    }

    [Fact]
    public void FindFullPathFromPath_UsesCorrectPathSeparator()
    {
        // Arrange
        var existingFiles = new HashSet<string>
        {
            Path.Combine("C:\\Windows\\System32", "mycommand")
        };

        // Act
        var result = PathLookupHelper.FindFullPathFromPath("mycommand", "C:\\Windows\\System32;C:\\Program Files", ';', existingFiles.Contains);

        // Assert
        Assert.Equal(Path.Combine("C:\\Windows\\System32", "mycommand"), result);
    }
}
