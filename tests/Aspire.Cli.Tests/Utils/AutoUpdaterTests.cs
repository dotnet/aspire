// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Aspire.Cli.Tests.Utils;

public class AutoUpdaterTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void IsAutoUpdateDisabled_ReturnsTrueWhenSetToTrue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configValues = new Dictionary<string, string?>
        {
            ["ASPIRE_CLI_AUTO_UPDATE_DISABLED"] = "true"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        Assert.True(autoUpdater.IsAutoUpdateDisabled());
    }

    [Fact]
    public void IsAutoUpdateDisabled_ReturnsTrueWhenSetTo1()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configValues = new Dictionary<string, string?>
        {
            ["ASPIRE_CLI_AUTO_UPDATE_DISABLED"] = "1"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        Assert.True(autoUpdater.IsAutoUpdateDisabled());
    }

    [Fact]
    public void IsAutoUpdateDisabled_ReturnsFalseWhenNotSet()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();

        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        Assert.False(autoUpdater.IsAutoUpdateDisabled());
    }

    [Fact]
    public void IsAutoUpdateDisabled_ReturnsFalseWhenSetToFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configValues = new Dictionary<string, string?>
        {
            ["ASPIRE_CLI_AUTO_UPDATE_DISABLED"] = "false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        Assert.False(autoUpdater.IsAutoUpdateDisabled());
    }

    [Theory]
    [InlineData("update", "--self")]
    [InlineData("UPDATE", "--SELF")]
    [InlineData("Update", "--Self")]
    [InlineData("--self", "update")]
    public void IsUpdateSelfCommand_ReturnsTrueForUpdateSelfArgs(string arg1, string arg2)
    {
        Assert.True(AutoUpdater.IsUpdateSelfCommand([arg1, arg2]));
    }

    [Theory]
    [InlineData("update")]
    [InlineData("--self")]
    [InlineData("run")]
    [InlineData("new", "--self")]
    public void IsUpdateSelfCommand_ReturnsFalseForOtherArgs(params string[] args)
    {
        Assert.False(AutoUpdater.IsUpdateSelfCommand(args));
    }

    [Fact]
    public void IsPrOrHiveBuild_ReturnsTrueWhenHivesExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        
        // Create a hive directory
        var hivesDir = Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "hives", "pr-12345");
        Directory.CreateDirectory(hivesDir);

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        Assert.True(autoUpdater.IsPrOrHiveBuild());
    }

    [Fact]
    public void IsPrOrHiveBuild_ReturnsFalseWhenNoHives()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        Assert.False(autoUpdater.IsPrOrHiveBuild());
    }

    [Fact]
    public async Task GetConfiguredChannelAsync_ReturnsStableWhenNotSet()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        var channel = await autoUpdater.GetConfiguredChannelAsync();

        Assert.Equal("stable", channel);
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("staging")]
    public async Task GetConfiguredChannelAsync_ReturnsConfiguredChannel(string channelName)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        testConfigService.SetValue("channel", channelName);

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService);

        var channel = await autoUpdater.GetConfiguredChannelAsync();

        Assert.Equal(channelName, channel);
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("Daily")]
    [InlineData("DAILY")]
    [InlineData("staging")]
    [InlineData("Staging")]
    public async Task ShouldCheckForUpdateAsync_ReturnsTrueForDailyAndStaging(string channelName)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        // Set a very recent check time - should still return true for daily/staging
        testConfigService.SetValue($"lastAutoUpdateCheck.{channelName}", fakeTime.GetUtcNow().ToString("O"));

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService, timeProvider: fakeTime);

        var shouldCheck = await autoUpdater.ShouldCheckForUpdateAsync(channelName);

        Assert.True(shouldCheck);
    }

    [Fact]
    public async Task ShouldCheckForUpdateAsync_ReturnsFalseForStableWithin24Hours()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        
        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        // Set check time to 1 hour ago
        testConfigService.SetValue("lastAutoUpdateCheck.stable", fakeTime.GetUtcNow().AddHours(-1).ToString("O"));

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService, timeProvider: fakeTime);

        var shouldCheck = await autoUpdater.ShouldCheckForUpdateAsync("stable");

        Assert.False(shouldCheck);
    }

    [Fact]
    public async Task ShouldCheckForUpdateAsync_ReturnsTrueForStableAfter24Hours()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        
        var testConfigService = new TestConfigurationService(workspace.WorkspaceRoot);
        // Set check time to 25 hours ago
        testConfigService.SetValue("lastAutoUpdateCheck.stable", fakeTime.GetUtcNow().AddHours(-25).ToString("O"));

        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration, configurationService: testConfigService, timeProvider: fakeTime);

        var shouldCheck = await autoUpdater.ShouldCheckForUpdateAsync("stable");

        Assert.True(shouldCheck);
    }

    [Fact]
    public async Task ShouldCheckForUpdateAsync_ReturnsTrueForStableWithNoLastCheck()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var configuration = new ConfigurationBuilder().Build();
        var autoUpdater = CreateAutoUpdater(workspace, configuration);

        var shouldCheck = await autoUpdater.ShouldCheckForUpdateAsync("stable");

        Assert.True(shouldCheck);
    }

    private static AutoUpdater CreateAutoUpdater(
        TemporaryWorkspace workspace,
        IConfiguration configuration,
        IConfigurationService? configurationService = null,
        TimeProvider? timeProvider = null)
    {
        var logger = NullLogger<AutoUpdater>.Instance;
        var nuGetPackageCache = new TestNuGetPackageCache();
        var packagingService = new TestPackagingService(nuGetPackageCache);
        
        var hivesDirectory = new DirectoryInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "hives"));
        var cacheDirectory = new DirectoryInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "cache"));
        var sdksDirectory = new DirectoryInfo(Path.Combine(workspace.WorkspaceRoot.FullName, ".aspire", "sdks"));
        
        var executionContext = new CliExecutionContext(
            workspace.WorkspaceRoot,
            hivesDirectory,
            cacheDirectory,
            sdksDirectory);

        configurationService ??= new TestConfigurationService(workspace.WorkspaceRoot);
        timeProvider ??= TimeProvider.System;
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
