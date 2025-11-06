// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Maui.Utilities;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class MauiMacCatalystExtensionsTests
{
    [Fact]
    public void AddMacCatalystDevice_CreatesResource()
    {
        // Arrange - Create a temporary project file with macOS Catalyst TFM
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

            // Act
            var macCatalyst = maui.AddMacCatalystDevice();

            // Assert
            Assert.NotNull(macCatalyst);
            Assert.Equal("mauiapp-maccatalyst", macCatalyst.Resource.Name);
            Assert.Equal(maui.Resource, macCatalyst.Resource.Parent);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddMacCatalystDevice_WithCustomName_UsesProvidedName()
    {
        // Arrange - Create a temporary project file with macOS Catalyst TFM
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

            // Act
            var macCatalyst = maui.AddMacCatalystDevice("custom-maccatalyst");

            // Assert
            Assert.Equal("custom-maccatalyst", macCatalyst.Resource.Name);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddMacCatalystDevice_DuplicateName_ThrowsException()
    {
        // Arrange - Create a temporary project file with macOS Catalyst TFM
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
            maui.AddMacCatalystDevice("device1");

            // Act & Assert
            var exception = Assert.Throws<DistributedApplicationException>(() => maui.AddMacCatalystDevice("device1"));
            Assert.Contains("already exists", exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddMacCatalystDevice_MultipleDevices_AllowsMultipleWithDifferentNames()
    {
        // Arrange - Create a temporary project file with macOS Catalyst TFM
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

            // Act
            var device1 = maui.AddMacCatalystDevice("device1");
            var device2 = maui.AddMacCatalystDevice("device2");

            // Assert
            Assert.Equal(2, appBuilder.Resources.OfType<Maui.MauiMacCatalystPlatformResource>().Count());
            Assert.Contains(device1.Resource, appBuilder.Resources);
            Assert.Contains(device2.Resource, appBuilder.Resources);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddMacCatalystDevice_SetsCorrectResourceProperties()
    {
        // Arrange - Create a temporary project file with macOS Catalyst TFM
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

            // Act
            var macCatalyst = maui.AddMacCatalystDevice();

            // Assert
            var executableAnnotation = macCatalyst.Resource.Annotations.OfType<ExecutableAnnotation>().Single();
            Assert.Equal("dotnet", executableAnnotation.Command);
            Assert.NotNull(executableAnnotation.WorkingDirectory);
            Assert.Equal(maui.Resource, macCatalyst.Resource.Parent);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task AddMacCatalystDevice_SetsCorrectCommandLineArguments()
    {
        // Arrange - Create a temporary project file with macOS Catalyst TFM
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

            // Act
            var macCatalyst = maui.AddMacCatalystDevice();

            using var app = appBuilder.Build();

            // Assert
            var args = await ArgumentEvaluator.GetArgumentListAsync(macCatalyst.Resource);
            Assert.Contains("run", args);
            Assert.Contains("-f", args);
            Assert.Contains("net10.0-maccatalyst", args);
            Assert.Contains("-p:OpenArguments=-W", args);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public async Task AddMacCatalystDevice_WithoutMacCatalystTfm_ThrowsOnBeforeStartEvent()
    {
        // Arrange - Create a temporary project file without macOS Catalyst TFM
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

            // Act - Adding the device should succeed (validation deferred to start)
            var macCatalyst = maui.AddMacCatalystDevice();
            
            // Assert - Resource is created
            Assert.NotNull(macCatalyst);
            Assert.Equal("mauiapp-maccatalyst", macCatalyst.Resource.Name);
            
            // Build the app to get access to eventing
            await using var app = appBuilder.Build();
            
            // Trigger the BeforeResourceStartedEvent which should throw
            var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
            {
                await app.Services.GetRequiredService<IDistributedApplicationEventing>()
                    .PublishAsync(new BeforeResourceStartedEvent(macCatalyst.Resource, app.Services), CancellationToken.None);
            });
            
            Assert.Contains("Unable to detect Mac Catalyst target framework", exception.Message);
            Assert.Contains(tempFile, exception.Message);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddMacCatalystDevice_DetectsMacCatalystTfmFromMultiTargetedProject()
    {
        // Arrange - Create a temporary project file with multiple TFMs including macOS Catalyst
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            // Act
            var tfm = ProjectFileReader.GetPlatformTargetFramework(tempFile, "maccatalyst");

            // Assert
            Assert.NotNull(tfm);
            Assert.Equal("net10.0-maccatalyst", tfm);
        }
        finally
        {
            CleanupTempFile(tempFile);
        }
    }

    [Fact]
    public void AddMacCatalystDevice_DetectsMacCatalystTfmFromSingleTargetProject()
    {
        // Arrange - Create a temporary project file with single macOS Catalyst TFM
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net10.0-maccatalyst</TargetFramework>
                </PropertyGroup>
            </Project>
            """;
        var tempFile = CreateTempProjectFile(projectContent);

        try
        {
            // Act
            var tfm = ProjectFileReader.GetPlatformTargetFramework(tempFile, "maccatalyst");

            // Assert
            Assert.NotNull(tfm);
            Assert.Equal("net10.0-maccatalyst", tfm);
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

    private static void CleanupTempFile(string tempFile)
    {
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
    }
}
