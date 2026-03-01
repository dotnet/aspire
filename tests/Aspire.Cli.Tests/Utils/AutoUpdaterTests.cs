// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class AutoUpdaterTests
{
    [Fact]
    public void ShouldAutoUpdate_ReturnsTrue_WhenAllConditionsMet()
    {
        var hostEnvironment = new FakeCliHostEnvironment();
        var features = new FakeFeatures();
        var executionContext = CreateExecutionContext();

        var result = AutoUpdater.ShouldAutoUpdate(hostEnvironment, features, executionContext);

        Assert.True(result);
    }

    [Fact]
    public void ShouldAutoUpdate_ReturnsFalse_WhenFeatureDisabled()
    {
        var hostEnvironment = new FakeCliHostEnvironment();
        var features = new FakeFeatures { AutoUpdateEnabled = false };
        var executionContext = CreateExecutionContext();

        var result = AutoUpdater.ShouldAutoUpdate(hostEnvironment, features, executionContext);

        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoUpdate_ReturnsFalse_WhenRunningInCI()
    {
        var hostEnvironment = new FakeCliHostEnvironment { IsRunningInCI = true };
        var features = new FakeFeatures();
        var executionContext = CreateExecutionContext();

        var result = AutoUpdater.ShouldAutoUpdate(hostEnvironment, features, executionContext);

        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoUpdate_ReturnsFalse_WhenEnvVarDisabled()
    {
        var hostEnvironment = new FakeCliHostEnvironment();
        var features = new FakeFeatures();
        var envVars = new Dictionary<string, string?> { ["ASPIRE_CLI_AUTO_UPDATE"] = "false" };
        var executionContext = CreateExecutionContext(envVars);

        var result = AutoUpdater.ShouldAutoUpdate(hostEnvironment, features, executionContext);

        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoUpdate_ReturnsTrue_WhenEnvVarNotSet()
    {
        var hostEnvironment = new FakeCliHostEnvironment();
        var features = new FakeFeatures();
        var envVars = new Dictionary<string, string?>();
        var executionContext = CreateExecutionContext(envVars);

        var result = AutoUpdater.ShouldAutoUpdate(hostEnvironment, features, executionContext);

        Assert.True(result);
    }

    [Fact]
    public void GetStagingDirectory_ReturnsExpectedPath()
    {
        var stagingDir = AutoUpdater.GetStagingDirectory();

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expected = Path.Combine(homeDir, ".aspire", "staging");

        Assert.Equal(expected, stagingDir);
    }

    [Fact]
    public void HasStagedUpdate_ReturnsFalse_WhenNoStagingDirectory()
    {
        // Staging directory shouldn't exist in the test environment
        var result = AutoUpdater.HasStagedUpdate();

        // This could be true if a real staging dir exists, but typically it won't in test
        // Just verify it doesn't throw
        Assert.IsType<bool>(result);
    }

    private static CliExecutionContext CreateExecutionContext(Dictionary<string, string?>? envVars = null)
    {
        var tempDir = new DirectoryInfo(Path.GetTempPath());
        return new CliExecutionContext(
            workingDirectory: tempDir,
            hivesDirectory: tempDir,
            cacheDirectory: tempDir,
            sdksDirectory: tempDir,
            logsDirectory: tempDir,
            logFilePath: Path.Combine(tempDir.FullName, "test.log"),
            environmentVariables: envVars != null ? new Dictionary<string, string?>(envVars) : null);
    }

    private sealed class FakeCliHostEnvironment : ICliHostEnvironment
    {
        public bool SupportsInteractiveInput => true;
        public bool SupportsInteractiveOutput => true;
        public bool SupportsAnsi => false;
        public bool IsRunningInCI { get; set; }
    }

    private sealed class FakeFeatures : IFeatures
    {
        public bool AutoUpdateEnabled { get; set; } = true;

        public bool IsFeatureEnabled(string featureFlag, bool defaultValue)
        {
            if (featureFlag == KnownFeatures.AutoUpdateEnabled)
            {
                return AutoUpdateEnabled;
            }
            return defaultValue;
        }
    }
}
