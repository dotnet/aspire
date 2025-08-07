// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Packaging;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Packaging;

public class NuGetConfigLocatorTests(ITestOutputHelper outputHelper)
{
    private static Aspire.Cli.CliExecutionContext CreateExecutionContext(DirectoryInfo workingDirectory)
    {
        return new Aspire.Cli.CliExecutionContext(workingDirectory);
    }

    [Fact]
    public void FindNuGetConfig_ThrowsArgumentNullException_WhenStartDirectoryIsNull()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var locator = new NuGetConfigLocator(executionContext);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => locator.FindNuGetConfig(null!));
    }

    [Fact]
    public void FindNuGetConfig_ReturnsNull_WhenNoNuGetConfigExists()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create a directory structure without any NuGet.config files
        var subDir = workspace.CreateDirectory("project");
        var nestedSubDir = subDir.CreateSubdirectory("nested");

        // Act
        var executionContext = CreateExecutionContext(nestedSubDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindNuGetConfig_ReturnsConfigFromStartDirectory_WhenConfigExistsInStartDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create directory with NuGet.config
        var projectDir = workspace.CreateDirectory("project");
        var configFile = new FileInfo(Path.Combine(projectDir.FullName, "nuget.config"));
        File.WriteAllText(configFile.FullName, "<configuration></configuration>");

        // Act
        var executionContext = CreateExecutionContext(projectDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile.FullName, result.FullName);
        Assert.Equal("nuget.config", result.Name);
    }

    [Fact]
    public void FindNuGetConfig_ReturnsConfigFromParentDirectory_WhenConfigExistsInParent()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create directory structure with NuGet.config in parent
        var projectDir = workspace.CreateDirectory("project");
        var subDir = projectDir.CreateSubdirectory("nested");
        
        var configFile = new FileInfo(Path.Combine(projectDir.FullName, "nuget.config"));
        File.WriteAllText(configFile.FullName, "<configuration></configuration>");

        // Act
        var executionContext = CreateExecutionContext(subDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile.FullName, result.FullName);
        Assert.Equal("nuget.config", result.Name);
    }

    [Fact]
    public void FindNuGetConfig_ReturnsClosestConfig_WhenMultipleConfigsExistInHierarchy()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create directory structure with multiple NuGet.config files
        var rootConfigFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "nuget.config"));
        File.WriteAllText(rootConfigFile.FullName, "<configuration>root</configuration>");

        var projectDir = workspace.CreateDirectory("project");
        var projectConfigFile = new FileInfo(Path.Combine(projectDir.FullName, "nuget.config"));
        File.WriteAllText(projectConfigFile.FullName, "<configuration>project</configuration>");

        var subDir = projectDir.CreateSubdirectory("nested");
        var nestedDir = subDir.CreateSubdirectory("deeper");

        // Act
        var executionContext = CreateExecutionContext(nestedDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectConfigFile.FullName, result.FullName);
        Assert.Equal("nuget.config", result.Name);
    }

    [Fact]
    public void FindNuGetConfig_FindsConfigInWorkspaceRoot_WhenSearchingFromDeepNesting()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Create deeply nested directory structure with NuGet.config only at root
        var configFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "nuget.config"));
        File.WriteAllText(configFile.FullName, "<configuration></configuration>");

        var level1 = workspace.CreateDirectory("level1");
        var level2 = level1.CreateSubdirectory("level2");
        var level3 = level2.CreateSubdirectory("level3");
        var level4 = level3.CreateSubdirectory("level4");

        // Act
        var executionContext = CreateExecutionContext(level4);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile.FullName, result.FullName);
        Assert.Equal("nuget.config", result.Name);
    }

    [Theory]
    [InlineData("nuget.config")]
    [InlineData("NuGet.config")]
    [InlineData("NUGET.CONFIG")]
    [InlineData("NuGet.Config")]
    public void FindNuGetConfig_IsCaseInsensitive_ForConfigFileName(string fileName)
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var projectDir = workspace.CreateDirectory("project");
        var configFile = new FileInfo(Path.Combine(projectDir.FullName, fileName));
        File.WriteAllText(configFile.FullName, "<configuration></configuration>");

        // Act
        var executionContext = CreateExecutionContext(projectDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile.FullName, result.FullName);
        Assert.Equal(fileName, result.Name);
    }

    [Fact]
    public void FindNuGetConfig_ReturnsFirstFound_WhenMultipleConfigsWithDifferentCasingExistInSameDirectory()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var projectDir = workspace.CreateDirectory("project");
        
        // Create multiple config files with different casing
        var lowerConfigFile = new FileInfo(Path.Combine(projectDir.FullName, "nuget.config"));
        File.WriteAllText(lowerConfigFile.FullName, "<configuration>lower</configuration>");
        
        var upperConfigFile = new FileInfo(Path.Combine(projectDir.FullName, "NUGET.CONFIG"));
        File.WriteAllText(upperConfigFile.FullName, "<configuration>upper</configuration>");

        // Act
        var executionContext = CreateExecutionContext(projectDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        // Should find one of them (the implementation uses FirstOrDefault, so order depends on file system)
        Assert.True(result.Name.Equals("nuget.config", StringComparison.OrdinalIgnoreCase) ||
                   result.Name.Equals("NUGET.CONFIG", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindNuGetConfig_IgnoresOtherFiles_WhenSearchingForConfig()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        var projectDir = workspace.CreateDirectory("project");
        
        // Create various files that are not NuGet.config
        File.WriteAllText(Path.Combine(projectDir.FullName, "nuget.exe"), "fake exe");
        File.WriteAllText(Path.Combine(projectDir.FullName, "config.xml"), "fake config");
        File.WriteAllText(Path.Combine(projectDir.FullName, "nuget.txt"), "fake text");
        File.WriteAllText(Path.Combine(projectDir.FullName, "project.csproj"), "fake project");

        var subDir = projectDir.CreateSubdirectory("nested");

        // Act
        var executionContext = CreateExecutionContext(subDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindNuGetConfig_ReturnsNull_WhenReachingFileSystemRoot()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);

        // Get a directory that should eventually reach the file system root
        // Use a temporary directory that we know doesn't have NuGet.config files
        var deepDir = workspace.CreateDirectory("deep");

        // Act
        var executionContext = CreateExecutionContext(deepDir);
        var locator = new NuGetConfigLocator(executionContext);
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindNuGetConfig_UsesWorkingDirectoryFromExecutionContext_WhenNoParameterProvided()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create config file in the workspace root
        var configFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "nuget.config"));
        File.WriteAllText(configFile.FullName, "<configuration></configuration>");

        var executionContext = CreateExecutionContext(workspace.WorkspaceRoot);
        var locator = new NuGetConfigLocator(executionContext);

        // Act
        var result = locator.FindNuGetConfig();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(configFile.FullName, result.FullName);
        Assert.Equal("nuget.config", result.Name);
    }
}
