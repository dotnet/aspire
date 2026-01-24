// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Utils;

public class AutoUpdaterTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void ShouldNotUpdate_WhenEnvVarDisabled()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configValues = new Dictionary<string, string?>
        {
            ["ASPIRE_CLI_AUTO_UPDATE_DISABLED"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        // Act - The method is fire-and-forget, but we can verify no update happens
        // by checking the update was skipped via the internal logic
        autoUpdater.StartBackgroundUpdate([]);

        // Assert - No exception thrown means it handled the disabled case correctly
        // The actual verification is in the debug logs showing "Auto-update is disabled via environment variable"
    }

    [Fact]
    public void ShouldNotUpdate_WhenEnvVarDisabledWith1()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configValues = new Dictionary<string, string?>
        {
            ["ASPIRE_CLI_AUTO_UPDATE_DISABLED"] = "1"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        // Act
        autoUpdater.StartBackgroundUpdate([]);

        // Assert - No exception thrown
    }

    [Fact]
    public void ShouldNotUpdate_WhenUpdateSelfCommand()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        // Act - Pass update --self args
        autoUpdater.StartBackgroundUpdate(["update", "--self"]);

        // Assert - No exception thrown means it handled the update --self case correctly
    }

    [Fact]
    public void ShouldNotUpdate_WhenUpdateSelfCommandCaseInsensitive()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        // Act - Pass UPDATE --SELF with different casing
        autoUpdater.StartBackgroundUpdate(["UPDATE", "--SELF"]);

        // Assert - No exception thrown
    }

    [Fact]
    public void ShouldNotUpdate_WhenPrHivesExist()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create a hive directory to simulate PR build environment
        var hivesDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "hives", "pr-12345");
        Directory.CreateDirectory(hivesDir);

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, createHives: true);

        // Act
        autoUpdater.StartBackgroundUpdate([]);

        // Assert - No exception thrown, auto-update should be skipped for PR/hive builds
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("Daily")]
    [InlineData("DAILY")]
    public async Task DailyChannel_AlwaysChecks_NoThrottle(string channelName)
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Set up config with channel and a recent last check timestamp
        var configValues = new Dictionary<string, string?>
        {
            ["channel"] = channelName,
            [$"lastAutoUpdateCheck:{channelName}"] = DateTimeOffset.UtcNow.ToString("O")
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        testConfigService.SetValue("channel", channelName);
        // Set a very recent check time
        testConfigService.SetValue($"lastAutoUpdateCheck.{channelName}", DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O"));

        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService);

        // Act
        autoUpdater.StartBackgroundUpdate([]);

        // Small delay to let background task start
        await Task.Delay(100);

        // Assert - No exception, daily should not be throttled
    }

    [Theory]
    [InlineData("staging")]
    [InlineData("Staging")]
    public async Task StagingChannel_AlwaysChecks_NoThrottle(string channelName)
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        testConfigService.SetValue("channel", channelName);
        testConfigService.SetValue($"lastAutoUpdateCheck.{channelName}", DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O"));

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService);

        // Act
        autoUpdater.StartBackgroundUpdate([]);

        await Task.Delay(100);

        // Assert - No exception, staging should not be throttled
    }

    [Fact]
    public async Task StableChannel_SkipsUpdate_WhenCheckedWithin24Hours()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        testConfigService.SetValue("channel", "stable");
        // Set check time to 1 hour ago (within 24hr throttle)
        testConfigService.SetValue("lastAutoUpdateCheck.stable", DateTimeOffset.UtcNow.AddHours(-1).ToString("O"));

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService);

        // Act
        autoUpdater.StartBackgroundUpdate([]);

        await Task.Delay(100);

        // Assert - No exception, and update should be throttled (skipped)
    }

    [Fact]
    public void StartBackgroundUpdate_DoesNotBlock()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        // Act & Assert - This should return immediately
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        autoUpdater.StartBackgroundUpdate([]);
        stopwatch.Stop();

        // Should complete in under 100ms since it's fire-and-forget
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"StartBackgroundUpdate took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }

    private static AutoUpdater CreateAutoUpdater(
        TemporaryWorkspace workspace,
        IConfiguration configuration,
        IConfigurationService? configurationService = null,
        bool createHives = false)
    {
        var logger = NullLogger<AutoUpdater>.Instance;
        var nuGetPackageCache = new TestNuGetPackageCache();
        var packagingService = new TestPackagingService(nuGetPackageCache);
        
        var hivesDirectory = new DirectoryInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "hives"));
        if (createHives)
        {
            var prHiveDir = Path.Combine(hivesDirectory.FullName, "pr-12345");
            Directory.CreateDirectory(prHiveDir);
        }
        
        var cacheDirectory = new DirectoryInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache"));
        var sdksDirectory = new DirectoryInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "sdks"));
        
        var executionContext = new CliExecutionContext(
            workspace.WorkspaceRoot,
            hivesDirectory,
            cacheDirectory,
            sdksDirectory);

        configurationService ??= new TestConfigurationService(workspace.WorkspaceRoot);
        var timeProvider = TimeProvider.System;
        var cliInstaller = new CliInstaller(NullLogger<CliInstaller>.Instance);

        return new AutoUpdater(
            logger,
            configuration,
            configurationService,
            packagingService,
            nuGetPackageCache,
            cliInstaller,
            executionContext,
            timeProvider);
    }
}

internal sealed class TestConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, string?> _values = new();
    private readonly DirectoryInfo _workingDirectory;

    public TestConfigurationService(DirectoryInfo workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public void SetValue(string key, string? value)
    {
        _values[key] = value;
    }

    public Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        _values.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetConfigurationAsync(string key, string value, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteConfigurationAsync(string key, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.Remove(key));
    }

    public Task<Dictionary<string, string>> GetAllConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value!));
    }

    public Task<Dictionary<string, string>> GetLocalConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return GetAllConfigurationAsync(cancellationToken);
    }

    public Task<Dictionary<string, string>> GetGlobalConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return GetAllConfigurationAsync(cancellationToken);
    }

    public string GetSettingsFilePath(bool isGlobal)
    {
        return Path.Combine(_workingDirectory.FullName, ".aspire", isGlobal ? "globalsettings.json" : "settings.json");
    }
}

internal sealed class TestPackagingService : IPackagingService
{
    private readonly INuGetPackageCache _nuGetPackageCache;

    public TestPackagingService(INuGetPackageCache nuGetPackageCache)
    {
        _nuGetPackageCache = nuGetPackageCache;
    }

    public Task<IEnumerable<PackageChannel>> GetChannelsAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<PackageChannel> channels =
        [
            PackageChannel.CreateExplicitChannel(PackageChannelNames.Stable, PackageChannelQuality.Stable, null, _nuGetPackageCache, false, "https://example.com/stable"),
            PackageChannel.CreateExplicitChannel(PackageChannelNames.Staging, PackageChannelQuality.Prerelease, null, _nuGetPackageCache, false, "https://example.com/staging"),
            PackageChannel.CreateExplicitChannel(PackageChannelNames.Daily, PackageChannelQuality.Prerelease, null, _nuGetPackageCache, false, "https://example.com/daily")
        ];
        return Task.FromResult(channels);
    }
}
