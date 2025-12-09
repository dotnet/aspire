// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Configuration;

public class ChannelResolverTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly FileInfo _globalSettingsFile;
    private readonly CliExecutionContext _executionContext;
    private readonly ChannelResolver _resolver;

    public ChannelResolverTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"aspire-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        var globalSettingsPath = Path.Combine(_tempDirectory, "globalsettings.json");
        _globalSettingsFile = new FileInfo(globalSettingsPath);

        var workingDirectory = new DirectoryInfo(Path.Combine(_tempDirectory, "workspace"));
        Directory.CreateDirectory(workingDirectory.FullName);

        _executionContext = new CliExecutionContext(
            workingDirectory,
            new DirectoryInfo(Path.Combine(_tempDirectory, "hives")),
            new DirectoryInfo(Path.Combine(_tempDirectory, "cache")),
            new DirectoryInfo(Path.Combine(_tempDirectory, "sdks")),
            environmentVariables: new Dictionary<string, string?>()
        );

        _resolver = new ChannelResolver(_executionContext, _globalSettingsFile, NullLogger<ChannelResolver>.Instance);
    }

    [Fact]
    public async Task ResolveChannel_WithNoSettings_ReturnsFallbackStable()
    {
        // Act
        var channel = await _resolver.ResolveChannelAsync();

        // Assert
        Assert.Equal("stable", channel);
    }

    [Fact]
    public async Task ResolveChannel_WithCliFlag_ReturnsCliFlag()
    {
        // Arrange
        await _resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "daily" });

        // Act
        var channel = await _resolver.ResolveChannelAsync(cliChannelOption: "staging");

        // Assert
        Assert.Equal("staging", channel);
    }

    [Fact]
    public async Task ResolveChannel_WithEnvironmentVariable_ReturnsEnvironmentValue()
    {
        // Arrange
        var envVars = new Dictionary<string, string?> { ["ASPIRE_CHANNEL"] = "daily" };
        var context = new CliExecutionContext(
            _executionContext.WorkingDirectory,
            _executionContext.HivesDirectory,
            _executionContext.CacheDirectory,
            _executionContext.SdksDirectory,
            environmentVariables: envVars
        );
        var resolver = new ChannelResolver(context, _globalSettingsFile, NullLogger<ChannelResolver>.Instance);

        await resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "stable" });

        // Act
        var channel = await resolver.ResolveChannelAsync();

        // Assert
        Assert.Equal("daily", channel);
    }

    [Fact]
    public async Task ResolveChannel_WithWorkspaceSettings_ReturnsWorkspaceChannel()
    {
        // Arrange
        await _resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "stable" });
        await _resolver.SetWorkspaceSettingsAsync(new WorkspaceSettings { Channel = "daily" });

        // Act
        var channel = await _resolver.ResolveChannelAsync(includeWorkspaceContext: true);

        // Assert
        Assert.Equal("daily", channel);
    }

    [Fact]
    public async Task ResolveChannel_WithWorkspaceSettingsButContextDisabled_ReturnsGlobalChannel()
    {
        // Arrange
        await _resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "stable" });
        await _resolver.SetWorkspaceSettingsAsync(new WorkspaceSettings { Channel = "daily" });

        // Act
        var channel = await _resolver.ResolveChannelAsync(includeWorkspaceContext: false);

        // Assert
        Assert.Equal("stable", channel);
    }

    [Fact]
    public async Task ResolveChannel_WithGlobalSettings_ReturnsGlobalChannel()
    {
        // Arrange
        await _resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "daily" });

        // Act
        var channel = await _resolver.ResolveChannelAsync();

        // Assert
        Assert.Equal("daily", channel);
    }

    [Fact]
    public async Task ResolveChannel_Precedence_CliOverridesAll()
    {
        // Arrange
        var envVars = new Dictionary<string, string?> { ["ASPIRE_CHANNEL"] = "daily" };
        var context = new CliExecutionContext(
            _executionContext.WorkingDirectory,
            _executionContext.HivesDirectory,
            _executionContext.CacheDirectory,
            _executionContext.SdksDirectory,
            environmentVariables: envVars
        );
        var resolver = new ChannelResolver(context, _globalSettingsFile, NullLogger<ChannelResolver>.Instance);

        await resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "stable" });
        await resolver.SetWorkspaceSettingsAsync(new WorkspaceSettings { Channel = "staging" });

        // Act
        var channel = await resolver.ResolveChannelAsync(cliChannelOption: "custom");

        // Assert
        Assert.Equal("custom", channel);
    }

    [Fact]
    public async Task ResolveChannel_Precedence_EnvOverridesWorkspaceAndGlobal()
    {
        // Arrange
        var envVars = new Dictionary<string, string?> { ["ASPIRE_CHANNEL"] = "daily" };
        var context = new CliExecutionContext(
            _executionContext.WorkingDirectory,
            _executionContext.HivesDirectory,
            _executionContext.CacheDirectory,
            _executionContext.SdksDirectory,
            environmentVariables: envVars
        );
        var resolver = new ChannelResolver(context, _globalSettingsFile, NullLogger<ChannelResolver>.Instance);

        await resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "stable" });
        await resolver.SetWorkspaceSettingsAsync(new WorkspaceSettings { Channel = "staging" });

        // Act
        var channel = await resolver.ResolveChannelAsync();

        // Assert
        Assert.Equal("daily", channel);
    }

    [Fact]
    public async Task ResolveChannel_Precedence_WorkspaceOverridesGlobal()
    {
        // Arrange
        await _resolver.SetGlobalSettingsAsync(new GlobalSettings { DefaultChannel = "stable" });
        await _resolver.SetWorkspaceSettingsAsync(new WorkspaceSettings { Channel = "daily" });

        // Act
        var channel = await _resolver.ResolveChannelAsync(includeWorkspaceContext: true);

        // Assert
        Assert.Equal("daily", channel);
    }

    [Fact]
    public async Task SetGlobalSettings_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var settings = new GlobalSettings
        {
            DefaultChannel = "daily",
            CliChannel = "daily"
        };

        // Act
        await _resolver.SetGlobalSettingsAsync(settings);

        // Assert
        Assert.True(File.Exists(_globalSettingsFile.FullName));
        var loaded = await _resolver.GetGlobalSettingsAsync();
        Assert.Equal("daily", loaded.DefaultChannel);
        Assert.Equal("daily", loaded.CliChannel);
    }

    [Fact]
    public async Task SetWorkspaceSettings_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var settings = new WorkspaceSettings
        {
            Channel = "daily"
        };

        // Act
        await _resolver.SetWorkspaceSettingsAsync(settings);

        // Assert
        var loaded = await _resolver.GetWorkspaceSettingsAsync();
        Assert.NotNull(loaded);
        Assert.Equal("daily", loaded.Channel);
    }

    [Fact]
    public async Task GetGlobalSettings_WithNonExistentFile_ReturnsEmptySettings()
    {
        // Act
        var settings = await _resolver.GetGlobalSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Null(settings.DefaultChannel);
        Assert.Null(settings.CliChannel);
    }

    [Fact]
    public async Task GetWorkspaceSettings_WithNonExistentFile_ReturnsNull()
    {
        // Act
        var settings = await _resolver.GetWorkspaceSettingsAsync();

        // Assert
        Assert.Null(settings);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
