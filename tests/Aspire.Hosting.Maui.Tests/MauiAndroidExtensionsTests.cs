// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class MauiAndroidExtensionsTests
{
    // ===== Android Device Tests =====

    [Fact]
    public void AddAndroidDevice_CreatesResource()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice();

            // Assert
            Assert.NotNull(androidDevice);
            Assert.Equal("mauiapp-android-device", androidDevice.Resource.Name);
            Assert.Same(maui.Resource, androidDevice.Resource.Parent);
            
            // Verify the resource is in the application model
            var androidDeviceInModel = appBuilder.Resources
                .OfType<MauiAndroidDeviceResource>()
                .Single(r => r.Parent == maui.Resource);
            Assert.Same(androidDevice.Resource, androidDeviceInModel);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_WithCustomName_UsesProvidedName()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice("custom-android-device");

            // Assert
            Assert.Equal("custom-android-device", androidDevice.Resource.Name);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_DuplicateName_ThrowsException()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            maui.AddAndroidDevice("android-device");

            // Act & Assert
            var exception = Assert.Throws<DistributedApplicationException>(() => 
                maui.AddAndroidDevice("android-device"));
            Assert.Contains("Android device with name 'android-device' already exists", exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_MultipleDevices_AllCreated()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var device1 = maui.AddAndroidDevice("device1");
            var device2 = maui.AddAndroidDevice("device2");

            // Assert
            var androidDevices = appBuilder.Resources
                .OfType<MauiAndroidDeviceResource>()
                .Where(r => r.Parent == maui.Resource)
                .ToList();
            Assert.Equal(2, androidDevices.Count);
            Assert.Contains(device1.Resource, androidDevices);
            Assert.Contains(device2.Resource, androidDevices);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_HasCorrectConfiguration()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice();

            // Assert
            var resource = androidDevice.Resource;
            
            // Check ExecutableAnnotation
            var execAnnotation = resource.Annotations.OfType<ExecutableAnnotation>().FirstOrDefault();
            Assert.NotNull(execAnnotation);
            Assert.Equal("dotnet", execAnnotation.Command);
            Assert.NotNull(execAnnotation.WorkingDirectory);

            // Check MauiProjectMetadata
            var metadata = resource.Annotations.OfType<MauiProjectMetadata>().FirstOrDefault();
            Assert.NotNull(metadata);
            Assert.Equal(tempFile, metadata.ProjectPath);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task AddAndroidDevice_WithoutAndroidTfm_ThrowsOnBeforeStartEvent()
    {
        // Arrange - Create a temporary project file without Android TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-ios;net10.0-windows10.0.19041.0;net10.0-maccatalyst</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act - Adding the device should succeed (validation deferred to start)
            var androidDevice = maui.AddAndroidDevice();
            
            // Assert - Resource is created
            Assert.NotNull(androidDevice);
            Assert.Equal("mauiapp-android-device", androidDevice.Resource.Name);
            
            // Build the app to get access to eventing
            await using var app = appBuilder.Build();
            
            // Trigger the BeforeResourceStartedEvent which should throw
            var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
            {
                await app.Services.GetRequiredService<IDistributedApplicationEventing>()
                    .PublishAsync(new BeforeResourceStartedEvent(androidDevice.Resource, app.Services), CancellationToken.None);
            });
            
            Assert.Contains("Unable to detect Android target framework", exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_ImplementsIMauiPlatformResource()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice();

            // Assert
            Assert.IsAssignableFrom<IMauiPlatformResource>(androidDevice.Resource);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_SetsCorrectResourceProperties()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice();

            // Assert
            var executableAnnotation = androidDevice.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
            Assert.Equal("dotnet", executableAnnotation.Command);
            Assert.NotNull(executableAnnotation.WorkingDirectory);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDevice_MultipleDevices_AllowsMultipleWithDifferentNames()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var device1 = maui.AddAndroidDevice("device1");
            var device2 = maui.AddAndroidDevice("device2");

            // Assert
            Assert.NotEqual(device1.Resource.Name, device2.Resource.Name);
            Assert.Same(device1.Resource.Parent, device2.Resource.Parent);
            Assert.Same(maui.Resource, device1.Resource.Parent);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    // ===== Android Emulator Tests =====

    [Fact]
    public void AddAndroidEmulator_CreatesResource()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidEmulator = maui.AddAndroidEmulator();

            // Assert
            Assert.NotNull(androidEmulator);
            Assert.Equal("mauiapp-android-emulator", androidEmulator.Resource.Name);
            Assert.Same(maui.Resource, androidEmulator.Resource.Parent);
            
            // Verify the resource is in the application model
            var androidEmulatorInModel = appBuilder.Resources
                .OfType<MauiAndroidEmulatorResource>()
                .Single(r => r.Parent == maui.Resource);
            Assert.Same(androidEmulator.Resource, androidEmulatorInModel);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_WithCustomName_UsesProvidedName()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidEmulator = maui.AddAndroidEmulator("custom-android-emulator");

            // Assert
            Assert.Equal("custom-android-emulator", androidEmulator.Resource.Name);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_DuplicateName_ThrowsException()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            maui.AddAndroidEmulator("android-emulator");

            // Act & Assert
            var exception = Assert.Throws<DistributedApplicationException>(() => 
                maui.AddAndroidEmulator("android-emulator"));
            Assert.Contains("Android emulator with name 'android-emulator' already exists", exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_MultipleEmulators_AllCreated()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var emulator1 = maui.AddAndroidEmulator("emulator1");
            var emulator2 = maui.AddAndroidEmulator("emulator2");

            // Assert
            var androidEmulators = appBuilder.Resources
                .OfType<MauiAndroidEmulatorResource>()
                .Where(r => r.Parent == maui.Resource)
                .ToList();
            Assert.Equal(2, androidEmulators.Count);
            Assert.Contains(emulator1.Resource, androidEmulators);
            Assert.Contains(emulator2.Resource, androidEmulators);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_HasCorrectConfiguration()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidEmulator = maui.AddAndroidEmulator();

            // Assert
            var resource = androidEmulator.Resource;
            
            // Check ExecutableAnnotation
            var execAnnotation = resource.Annotations.OfType<ExecutableAnnotation>().FirstOrDefault();
            Assert.NotNull(execAnnotation);
            Assert.Equal("dotnet", execAnnotation.Command);
            Assert.NotNull(execAnnotation.WorkingDirectory);

            // Check MauiProjectMetadata
            var metadata = resource.Annotations.OfType<MauiProjectMetadata>().FirstOrDefault();
            Assert.NotNull(metadata);
            Assert.Equal(tempFile, metadata.ProjectPath);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task AddAndroidEmulator_WithoutAndroidTfm_ThrowsOnBeforeStartEvent()
    {
        // Arrange - Create a temporary project file without Android TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-ios;net10.0-windows10.0.19041.0;net10.0-maccatalyst</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            var appBuilder = DistributedApplication.CreateBuilder();
            var maui = appBuilder.AddMauiProject("mauiapp", tempFile);

            // Act - Adding the emulator should succeed (validation deferred to start)
            var androidEmulator = maui.AddAndroidEmulator();
            
            // Assert - Resource is created
            Assert.NotNull(androidEmulator);
            Assert.Equal("mauiapp-android-emulator", androidEmulator.Resource.Name);
            
            // Build the app to get access to eventing
            await using var app = appBuilder.Build();
            
            // Trigger the BeforeResourceStartedEvent which should throw
            var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
            {
                await app.Services.GetRequiredService<IDistributedApplicationEventing>()
                    .PublishAsync(new BeforeResourceStartedEvent(androidEmulator.Resource, app.Services), CancellationToken.None);
            });
            
            Assert.Contains("Unable to detect Android target framework", exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_ImplementsIMauiPlatformResource()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidEmulator = maui.AddAndroidEmulator();

            // Assert
            Assert.IsAssignableFrom<IMauiPlatformResource>(androidEmulator.Resource);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_SetsCorrectResourceProperties()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidEmulator = maui.AddAndroidEmulator();

            // Assert
            var executableAnnotation = androidEmulator.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
            Assert.Equal("dotnet", executableAnnotation.Command);
            Assert.NotNull(executableAnnotation.WorkingDirectory);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task AddAndroidEmulator_WithEnvironment_EnvironmentVariablesAreSet()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidEmulator = maui.AddAndroidEmulator()
                .WithEnvironment("DEBUG_MODE", "true")
                .WithEnvironment("API_TIMEOUT", "30");

            // Assert - Verify environment variables are set on the resource
            var envVars = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
                androidEmulator.Resource,
                DistributedApplicationOperation.Run,
                TestServiceProvider.Instance);

            Assert.Contains(envVars, kvp => kvp.Key == "DEBUG_MODE" && kvp.Value == "true");
            Assert.Contains(envVars, kvp => kvp.Key == "API_TIMEOUT" && kvp.Value == "30");
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidEmulator_MultipleEmulators_AllowsMultipleWithDifferentNames()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var emulator1 = maui.AddAndroidEmulator("emulator1");
            var emulator2 = maui.AddAndroidEmulator("emulator2");

            // Assert
            Assert.NotEqual(emulator1.Resource.Name, emulator2.Resource.Name);
            Assert.Same(emulator1.Resource.Parent, emulator2.Resource.Parent);
            Assert.Same(maui.Resource, emulator1.Resource.Parent);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    // ===== Mixed Device and Emulator Tests =====

    [Fact]
    public void AddAndroidDeviceAndEmulator_CanCoexist()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice();
            var androidEmulator = maui.AddAndroidEmulator();

            // Assert
            Assert.NotNull(androidDevice);
            Assert.NotNull(androidEmulator);
            Assert.NotEqual(androidDevice.Resource.Name, androidEmulator.Resource.Name);
            Assert.IsType<MauiAndroidDeviceResource>(androidDevice.Resource);
            Assert.IsType<MauiAndroidEmulatorResource>(androidEmulator.Resource);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddAndroidDeviceAndEmulator_DifferentResourceTypes()
    {
        // Arrange - Create a temporary project file with Android TFM
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
            var androidDevice = maui.AddAndroidDevice("android-device");
            var androidEmulator = maui.AddAndroidEmulator("android-emulator"); // Different name since resource names must be unique

            // Assert - Should allow different names for different resource types
            Assert.NotNull(androidDevice);
            Assert.NotNull(androidEmulator);
            Assert.NotEqual(androidDevice.Resource.Name, androidEmulator.Resource.Name);
            Assert.Equal("android-device", androidDevice.Resource.Name);
            Assert.Equal("android-emulator", androidEmulator.Resource.Name);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    private static string CreateTempProjectFile(string content)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csproj");
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    private static void CleanupTempFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
