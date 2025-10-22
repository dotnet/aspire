// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Tests;

public class MauiWindowsExtensionsTests
{
    [Fact]
    public void AddWindowsDevice_CreatesResource()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var maui = appBuilder.AddMauiProject("mauiapp", "TestProject.csproj");

        // Act
        var windows = maui.AddWindowsDevice();

        // Assert
        Assert.NotNull(windows);
        Assert.Equal("mauiapp-windows", windows.Resource.Name);
        Assert.Contains(windows.Resource, maui.Resource.WindowsDevices);
    }

    [Fact]
    public void AddWindowsDevice_WithCustomName_UsesProvidedName()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var maui = appBuilder.AddMauiProject("mauiapp", "TestProject.csproj");

        // Act
        var windows = maui.AddWindowsDevice("custom-windows");

        // Assert
        Assert.Equal("custom-windows", windows.Resource.Name);
    }

    [Fact]
    public void AddWindowsDevice_DuplicateName_ThrowsException()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var maui = appBuilder.AddMauiProject("mauiapp", "TestProject.csproj");
        maui.AddWindowsDevice("device1");

        // Act & Assert
        var exception = Assert.Throws<DistributedApplicationException>(() => maui.AddWindowsDevice("device1"));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void AddWindowsDevice_MultipleDevices_AllCreated()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var maui = appBuilder.AddMauiProject("mauiapp", "TestProject.csproj");

        // Act
        var windows1 = maui.AddWindowsDevice("device1");
        var windows2 = maui.AddWindowsDevice("device2");

        // Assert
        Assert.Equal(2, maui.Resource.WindowsDevices.Count);
        Assert.Contains(windows1.Resource, maui.Resource.WindowsDevices);
        Assert.Contains(windows2.Resource, maui.Resource.WindowsDevices);
    }

    [Fact]
    public void AddWindowsDevice_HasCorrectConfiguration()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var maui = appBuilder.AddMauiProject("mauiapp", "TestProject.csproj");

        // Act
        var windows = maui.AddWindowsDevice();

        // Assert
        var resource = windows.Resource;
        Assert.Equal("dotnet", resource.Command);
        
        // Check for explicit start annotation
        var hasExplicitStart = resource.TryGetAnnotationsOfType<ExplicitStartupAnnotation>(out _);
        Assert.True(hasExplicitStart);
    }

    [Fact]
    public async Task HasWindowsTargetFramework_WithWindowsTfm_ReturnsTrue()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, projectContent);

        try
        {
            // Act
            var hasWindowsTfm = InvokeHasWindowsTargetFramework(tempFile);

            // Assert
            Assert.True(hasWindowsTfm);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HasWindowsTargetFramework_WithConditionalWindowsTfm_ReturnsTrue()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios</TargetFrameworks>
                    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, projectContent);

        try
        {
            // Act
            var hasWindowsTfm = InvokeHasWindowsTargetFramework(tempFile);

            // Assert
            Assert.True(hasWindowsTfm);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HasWindowsTargetFramework_WithoutWindowsTfm_ReturnsFalse()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, projectContent);

        try
        {
            // Act
            var hasWindowsTfm = InvokeHasWindowsTargetFramework(tempFile);

            // Assert
            Assert.False(hasWindowsTfm);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HasWindowsTargetFramework_WithSingleWindowsTfm_ReturnsTrue()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, projectContent);

        try
        {
            // Act
            var hasWindowsTfm = InvokeHasWindowsTargetFramework(tempFile);

            // Assert
            Assert.True(hasWindowsTfm);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HasWindowsTargetFramework_InvalidFile_ReturnsTrue()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");

        // Act
        var hasWindowsTfm = InvokeHasWindowsTargetFramework(nonExistentFile);

        // Assert - defaults to true to avoid false positives
        Assert.True(hasWindowsTfm);
    }

    private static bool InvokeHasWindowsTargetFramework(string projectPath)
    {
        // Use reflection to invoke the private method
        var method = typeof(MauiWindowsExtensions).GetMethod(
            "HasWindowsTargetFramework",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        return (bool)method!.Invoke(null, [projectPath])!;
    }
}
