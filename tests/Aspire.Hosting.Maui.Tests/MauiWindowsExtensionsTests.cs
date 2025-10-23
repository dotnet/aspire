// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Maui.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class MauiWindowsExtensionsTests
{
    [Fact]
    public void AddWindowsDevice_CreatesResource()
    {
        // Arrange - Create a temporary project file with Windows TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act
            var windows = maui.AddWindowsDevice();

            // Assert
            Assert.NotNull(windows);
            Assert.Equal("mauiapp-windows", windows.Resource.Name);
            Assert.Same(maui.Resource, windows.Resource.Parent);
            
            // Verify the resource is in the application model
            var windowsDeviceInModel = appBuilder.Resources
                .OfType<MauiWindowsPlatformResource>()
                .FirstOrDefault(r => r.Name == "mauiapp-windows");
            Assert.NotNull(windowsDeviceInModel);
            Assert.Same(windows.Resource, windowsDeviceInModel);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddWindowsDevice_WithCustomName_UsesProvidedName()
    {
        // Arrange - Create a temporary project file with Windows TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act
            var windows = maui.AddWindowsDevice("custom-windows");

            // Assert
            Assert.Equal("custom-windows", windows.Resource.Name);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddWindowsDevice_DuplicateName_ThrowsException()
    {
        // Arrange - Create a temporary project file with Windows TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);
            maui.AddWindowsDevice("device1");

            // Act & Assert
            var exception = Assert.Throws<DistributedApplicationException>(() => maui.AddWindowsDevice("device1"));
            Assert.Contains("already exists", exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddWindowsDevice_MultipleDevices_AllCreated()
    {
        // Arrange - Create a temporary project file with Windows TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act
            var windows1 = maui.AddWindowsDevice("device1");
            var windows2 = maui.AddWindowsDevice("device2");

            // Assert
            var windowsDevices = appBuilder.Resources
                .OfType<MauiWindowsPlatformResource>()
                .Where(r => r.Parent == maui.Resource)
                .ToList();
            
            Assert.Equal(2, windowsDevices.Count);
            Assert.Contains(windows1.Resource, windowsDevices);
            Assert.Contains(windows2.Resource, windowsDevices);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddWindowsDevice_HasCorrectConfiguration()
    {
        // Arrange - Create a temporary project file with Windows TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act
            var windows = maui.AddWindowsDevice();

            // Assert
            var resource = windows.Resource;
            
            // Check ExecutableAnnotation
            var execAnnotation = resource.Annotations.OfType<ExecutableAnnotation>().FirstOrDefault();
            Assert.NotNull(execAnnotation);
            Assert.Equal("dotnet", execAnnotation.Command);
            
            // Check for MauiProjectMetadata annotation
            var projectMetadata = resource.Annotations.OfType<IProjectMetadata>().FirstOrDefault();
            Assert.NotNull(projectMetadata);
            Assert.Equal(tempFile, projectMetadata.ProjectPath);
            
            // Check for explicit start annotation
            var hasExplicitStart = resource.TryGetAnnotationsOfType<ExplicitStartupAnnotation>(out _);
            Assert.True(hasExplicitStart);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task AddWindowsDevice_WithoutWindowsTfm_ThrowsOnBeforeStartEvent()
    {
        // Arrange - Create a temporary project file without Windows TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act - Adding the device should succeed (validation deferred to start)
            var windows = maui.AddWindowsDevice();
            
            // Assert - Resource is created
            Assert.NotNull(windows);
            Assert.Equal("mauiapp-windows", windows.Resource.Name);
            
            // Build the app to get access to eventing
            await using var app = appBuilder.Build();
            
            // Trigger the BeforeResourceStartedEvent which should throw
            var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
            {
                await app.Services.GetRequiredService<IDistributedApplicationEventing>()
                    .PublishAsync(new BeforeResourceStartedEvent(windows.Resource, app.Services), CancellationToken.None);
            });
            
            Assert.Contains("Unable to detect Windows target framework", exception.Message);
            Assert.Contains(tempFile, exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    private static string CreateTempProjectFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        var tempProjectFile = Path.ChangeExtension(tempFile, ".csproj");
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
        File.WriteAllText(tempProjectFile, content);
        return tempProjectFile;
    }

    private static void CleanupTempFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task GetWindowsTargetFramework_WithWindowsTfm_ReturnsCorrectTfm()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            // Act
            var tfm = ProjectFileReader.GetPlatformTargetFramework(tempFile, "windows");

            // Assert
            Assert.Equal("net10.0-windows10.0.19041.0", tfm);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task GetWindowsTargetFramework_WithConditionalWindowsTfm_ReturnsCorrectTfm()
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
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            // Act
            var tfm = ProjectFileReader.GetPlatformTargetFramework(tempFile, "windows");

            // Assert
            Assert.Equal("net10.0-windows10.0.19041.0", tfm);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task GetWindowsTargetFramework_WithoutWindowsTfm_ReturnsNull()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            // Act
            var tfm = ProjectFileReader.GetPlatformTargetFramework(tempFile, "windows");

            // Assert
            Assert.Null(tfm);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task GetWindowsTargetFramework_WithSingleWindowsTfm_ReturnsCorrectTfm()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            // Act
            var tfm = ProjectFileReader.GetPlatformTargetFramework(tempFile, "windows");

            // Assert
            Assert.Equal("net10.0-windows10.0.19041.0", tfm);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task GetWindowsTargetFramework_InvalidFile_ReturnsNull()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".csproj");

        // Act
        var tfm = ProjectFileReader.GetPlatformTargetFramework(nonExistentFile, "windows");

        // Assert - returns null when file can't be read
        Assert.Null(tfm);
    }
}
