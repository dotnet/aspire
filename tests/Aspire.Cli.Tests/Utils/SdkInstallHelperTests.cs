// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class SdkInstallHelperTests
{
    private sealed class TestCliHostEnvironment(bool supportsInteractiveInput) : ICliHostEnvironment
    {
        public bool SupportsInteractiveInput { get; } = supportsInteractiveInput;
        public bool SupportsInteractiveOutput => true;
        public bool SupportsAnsi => true;
    }

    private sealed class TestFeatures : IFeatures
    {
        private readonly Dictionary<string, bool> _features = new();

        public TestFeatures SetFeature(string featureName, bool value)
        {
            _features[featureName] = value;
            return this;
        }

        public bool IsFeatureEnabled(string featureName, bool defaultValue = false)
        {
            return _features.TryGetValue(featureName, out var value) ? value : defaultValue;
        }
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenSdkAlreadyInstalled_RecordsAlreadyInstalledTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (true, "9.0.302", "9.0.302", false)
        };

        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures();

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry);

        Assert.True(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("9.0.302", tags[TelemetryConstants.Tags.SdkDetectedVersion]);
        Assert.Equal("9.0.302", tags[TelemetryConstants.Tags.SdkMinimumRequiredVersion]);
        Assert.Equal("already_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("already_installed", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenSdkMissingAndFeatureDisabled_RecordsFeatureNotEnabledTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "9.0.302", false)
        };

        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, false);

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry);

        Assert.False(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("(not found)", tags[TelemetryConstants.Tags.SdkDetectedVersion]);
        Assert.Equal("9.0.302", tags[TelemetryConstants.Tags.SdkMinimumRequiredVersion]);
        Assert.Equal("not_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("feature_not_enabled", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenSdkMissingAndNotInteractive_RecordsNotInteractiveTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, "8.0.100", "9.0.302", false)
        };

        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true);
        var hostEnvironment = new TestCliHostEnvironment(supportsInteractiveInput: false);

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry,
            hostEnvironment);

        Assert.False(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("8.0.100", tags[TelemetryConstants.Tags.SdkDetectedVersion]);
        Assert.Equal("9.0.302", tags[TelemetryConstants.Tags.SdkMinimumRequiredVersion]);
        Assert.Equal("not_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("not_interactive", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenUserDeclinesInstallation_RecordsUserDeclinedTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, "8.0.100", "9.0.302", false)
        };

        var interactionService = new TestConsoleInteractionService
        {
            ConfirmCallback = (_, _) => false // User declines
        };
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true);
        var hostEnvironment = new TestCliHostEnvironment(supportsInteractiveInput: true);

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry,
            hostEnvironment);

        Assert.False(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("not_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("user_declined", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenInstallationSucceeds_RecordsInstalledTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, "8.0.100", "9.0.302", false),
            InstallAsyncCallback = _ => Task.CompletedTask
        };

        var interactionService = new TestConsoleInteractionService
        {
            ConfirmCallback = (_, _) => true // User accepts
        };
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true);
        var hostEnvironment = new TestCliHostEnvironment(supportsInteractiveInput: true);

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry,
            hostEnvironment);

        Assert.True(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("not_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("installed", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenInstallationFails_RecordsInstallErrorTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, "8.0.100", "9.0.302", false),
            InstallAsyncCallback = _ => throw new InvalidOperationException("Installation failed")
        };

        var interactionService = new TestConsoleInteractionService
        {
            ConfirmCallback = (_, _) => true // User accepts
        };
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true);
        var hostEnvironment = new TestCliHostEnvironment(supportsInteractiveInput: true);

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry,
            hostEnvironment);

        Assert.False(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("not_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("install_error", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }

    [Fact]
    public async Task EnsureSdkInstalledAsync_WhenForceInstall_RecordsForceInstalledTelemetry()
    {
        using var fixture = new TelemetryFixture();

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (true, "9.0.302", "9.0.302", true), // forceInstall = true
            InstallAsyncCallback = _ => Task.CompletedTask
        };

        var interactionService = new TestConsoleInteractionService();
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.DotNetSdkInstallationEnabled, true);

        var result = await SdkInstallHelper.EnsureSdkInstalledAsync(
            sdkInstaller,
            interactionService,
            features,
            fixture.Telemetry);

        Assert.True(result);
        Assert.NotNull(fixture.CapturedActivity);

        var tags = fixture.CapturedActivity.Tags.ToDictionary(t => t.Key, t => t.Value);
        Assert.Equal("force_installed", tags[TelemetryConstants.Tags.SdkCheckResult]);
        Assert.Equal("installed", tags[TelemetryConstants.Tags.SdkInstallResult]);
    }
}
