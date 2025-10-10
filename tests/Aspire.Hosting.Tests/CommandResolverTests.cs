// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

public class CommandResolverTests
{
    [Fact]
    public void ResolveCommand_ReturnsNullForNullCommand()
    {
        // Act
        var result = CommandResolver.ResolveCommand(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveCommand_ReturnsNullForEmptyCommand()
    {
        // Act
        var result = CommandResolver.ResolveCommand(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveCommand_ReturnsNullForWhitespaceCommand()
    {
        // Act
        var result = CommandResolver.ResolveCommand("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveCommand_FindsCommandOnPath()
    {
        // Arrange - Use a command that should exist on all platforms
        var command = OperatingSystem.IsWindows() ? "cmd" : "sh";

        // Act
        var result = CommandResolver.ResolveCommand(command);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.Contains(command, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveCommand_ReturnsNullForNonexistentCommand()
    {
        // Arrange
        var command = "this-command-definitely-does-not-exist-xyz123";

        // Act
        var result = CommandResolver.ResolveCommand(command);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveCommand_ResolvesAbsolutePath()
    {
        // Arrange - Create a temporary executable file
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            var result = CommandResolver.ResolveCommand(tempFile);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tempFile, result);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ResolveCommand_ResolvesRelativePath()
    {
        // Arrange - Create a temporary executable file in a subdirectory
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test.exe");
        File.WriteAllText(tempFile, string.Empty);

        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            try
            {
                // Act
                var result = CommandResolver.ResolveCommand("./test.exe");

                // Assert
                Assert.NotNull(result);
                Assert.True(File.Exists(result));
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDir);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ResolveCommand_ReturnsNullForNonexistentAbsolutePath()
    {
        // Arrange
        var nonexistentPath = Path.Combine(Path.GetTempPath(), "nonexistent-file-xyz.exe");

        // Act
        var result = CommandResolver.ResolveCommand(nonexistentPath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ResolveCommand_ReturnsNullForNonexistentRelativePath()
    {
        // Act
        var result = CommandResolver.ResolveCommand("./nonexistent-file-xyz.exe");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("cmd")]
    [InlineData("CMD")]
    [InlineData("Cmd")]
    public void ResolveCommand_IsCaseInsensitiveOnWindows(string command)
    {
        if (!OperatingSystem.IsWindows())
        {
            // Skip on non-Windows platforms
            return;
        }

        // Act
        var result = CommandResolver.ResolveCommand(command);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void ResolveCommand_FindsCommandWithExtensionOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Skip on non-Windows platforms
            return;
        }

        // Act - "cmd.exe" should be found
        var result = CommandResolver.ResolveCommand("cmd.exe");

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.EndsWith(".exe", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveCommand_FindsCommandWithoutExtensionOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Skip on non-Windows platforms
            return;
        }

        // Act - "cmd" should be found (will add .exe from PATHEXT)
        var result = CommandResolver.ResolveCommand("cmd");

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.EndsWith(".exe", result, StringComparison.OrdinalIgnoreCase);
    }
}
