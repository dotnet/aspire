// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Tests.Utils;

namespace Aspire.Cli.Tests.Projects;

public class GuestAppHostProjectTests(ITestOutputHelper outputHelper) : IDisposable
{
    private readonly TemporaryWorkspace _workspace = TemporaryWorkspace.Create(outputHelper);

    public void Dispose()
    {
        _workspace.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void AspireJsonConfiguration_LoadOrCreate_SetsDefaultSdkVersion()
    {
        // Arrange
        var directory = _workspace.WorkspaceRoot.FullName;

        // Act
        var config = AspireJsonConfiguration.LoadOrCreate(directory, "13.1.0");

        // Assert
        Assert.Equal("13.1.0", config.SdkVersion);
    }

    [Fact]
    public void AspireJsonConfiguration_LoadOrCreate_PreservesExistingSdkVersion()
    {
        // Arrange - create settings.json with existing SDK version
        var settingsDir = _workspace.CreateDirectory(".aspire");
        var settingsPath = Path.Combine(settingsDir.FullName, "settings.json");
        File.WriteAllText(settingsPath, """
            {
                "sdkVersion": "12.0.0",
                "language": "typescript"
            }
            """);

        // Act
        var config = AspireJsonConfiguration.LoadOrCreate(_workspace.WorkspaceRoot.FullName, "13.1.0");

        // Assert - should preserve existing version, not override with default
        Assert.Equal("12.0.0", config.SdkVersion);
    }

    [Fact]
    public void AspireJsonConfiguration_Save_UpdatesSdkVersion()
    {
        // Arrange - create initial settings.json
        var settingsDir = _workspace.CreateDirectory(".aspire");
        var settingsPath = Path.Combine(settingsDir.FullName, "settings.json");
        File.WriteAllText(settingsPath, """
            {
                "sdkVersion": "12.0.0",
                "language": "typescript",
                "packages": {
                    "Aspire.Hosting.Redis": "12.0.0"
                }
            }
            """);

        // Act - load, update SDK version, and save
        var config = AspireJsonConfiguration.Load(_workspace.WorkspaceRoot.FullName);
        Assert.NotNull(config);
        config.SdkVersion = "13.1.0";
        config.Save(_workspace.WorkspaceRoot.FullName);

        // Assert - reload and verify
        var reloaded = AspireJsonConfiguration.Load(_workspace.WorkspaceRoot.FullName);
        Assert.NotNull(reloaded);
        Assert.Equal("13.1.0", reloaded.SdkVersion);
        Assert.Equal("typescript", reloaded.Language);
        Assert.NotNull(reloaded.Packages);
        Assert.Equal("12.0.0", reloaded.Packages["Aspire.Hosting.Redis"]);
    }

    [Fact]
    public void AspireJsonConfiguration_AddOrUpdatePackage_AddsNewPackage()
    {
        // Arrange
        var config = new AspireJsonConfiguration
        {
            SdkVersion = "13.1.0",
            Language = "typescript"
        };

        // Act
        config.AddOrUpdatePackage("Aspire.Hosting.Redis", "13.1.0");

        // Assert
        Assert.NotNull(config.Packages);
        Assert.Single(config.Packages);
        Assert.Equal("13.1.0", config.Packages["Aspire.Hosting.Redis"]);
    }

    [Fact]
    public void AspireJsonConfiguration_AddOrUpdatePackage_UpdatesExistingPackage()
    {
        // Arrange
        var config = new AspireJsonConfiguration
        {
            SdkVersion = "13.1.0",
            Language = "typescript",
            Packages = new Dictionary<string, string>
            {
                ["Aspire.Hosting.Redis"] = "12.0.0"
            }
        };

        // Act
        config.AddOrUpdatePackage("Aspire.Hosting.Redis", "13.1.0");

        // Assert
        Assert.NotNull(config.Packages);
        Assert.Single(config.Packages);
        Assert.Equal("13.1.0", config.Packages["Aspire.Hosting.Redis"]);
    }

    [Fact]
    public void AspireJsonConfiguration_GetAllPackages_IncludesBasePackages()
    {
        // Arrange
        var config = new AspireJsonConfiguration
        {
            SdkVersion = "13.1.0",
            Language = "typescript",
            Packages = new Dictionary<string, string>
            {
                ["Aspire.Hosting.Redis"] = "13.1.0"
            }
        };

        // Act
        var packages = config.GetAllPackages().ToList();

        // Assert - should include base packages plus explicit packages
        Assert.Contains(packages, p => p.Name == "Aspire.Hosting" && p.Version == "13.1.0");
        Assert.Contains(packages, p => p.Name == "Aspire.Hosting.AppHost" && p.Version == "13.1.0");
        Assert.Contains(packages, p => p.Name == "Aspire.Hosting.Redis" && p.Version == "13.1.0");
    }

    [Fact]
    public void AspireJsonConfiguration_GetAllPackages_WithNoExplicitPackages_ReturnsBasePackagesOnly()
    {
        // Arrange
        var config = new AspireJsonConfiguration
        {
            SdkVersion = "13.1.0",
            Language = "typescript"
        };

        // Act
        var packages = config.GetAllPackages().ToList();

        // Assert - should include base packages only
        Assert.Equal(2, packages.Count);
        Assert.Contains(packages, p => p.Name == "Aspire.Hosting" && p.Version == "13.1.0");
        Assert.Contains(packages, p => p.Name == "Aspire.Hosting.AppHost" && p.Version == "13.1.0");
    }

    [Fact]
    public void AspireJsonConfiguration_Save_PreservesExtensionData()
    {
        // Arrange - create settings.json with extra properties
        var settingsDir = _workspace.CreateDirectory(".aspire");
        var settingsPath = Path.Combine(settingsDir.FullName, "settings.json");
        File.WriteAllText(settingsPath, """
            {
                "sdkVersion": "13.1.0",
                "language": "typescript",
                "features": {
                    "experimental": true
                },
                "customProperty": "customValue"
            }
            """);

        // Act - load, modify, and save
        var config = AspireJsonConfiguration.Load(_workspace.WorkspaceRoot.FullName);
        Assert.NotNull(config);
        config.SdkVersion = "13.2.0";
        config.Save(_workspace.WorkspaceRoot.FullName);

        // Assert - reload and verify extension data is preserved
        var json = File.ReadAllText(settingsPath);
        Assert.Contains("features", json);
        Assert.Contains("experimental", json);
        Assert.Contains("customProperty", json);
        Assert.Contains("customValue", json);
    }

    [Fact]
    public async Task AspireJsonConfiguration_MatchesSnapshot()
    {
        // Arrange - create a full settings.json
        var config = new AspireJsonConfiguration
        {
            Schema = "https://json.schemastore.org/aspire-settings.json",
            AppHostPath = "apphost.ts",
            Language = "typescript",
            SdkVersion = "13.1.0",
            Packages = new Dictionary<string, string>
            {
                ["Aspire.Hosting.Redis"] = "13.1.0",
                ["Aspire.Hosting.PostgreSQL"] = "13.1.0"
            }
        };

        // Act
        config.Save(_workspace.WorkspaceRoot.FullName);

        // Assert
        var settingsPath = AspireJsonConfiguration.GetFilePath(_workspace.WorkspaceRoot.FullName);
        var content = await File.ReadAllTextAsync(settingsPath);

        await Verify(content, extension: "json")
            .UseFileName("AspireJsonConfiguration_SettingsJson");
    }
}
