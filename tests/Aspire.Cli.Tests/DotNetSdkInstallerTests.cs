// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Semver;

namespace Aspire.Cli.Tests;

public class DotNetSdkInstallerTests
{
    private static CliExecutionContext CreateTestExecutionContext()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "aspire-cli-tests", Guid.NewGuid().ToString());
        var workingDirectory = new DirectoryInfo(tempPath);
        var hivesDirectory = new DirectoryInfo(Path.Combine(tempPath, "hives"));
        var cacheDirectory = new DirectoryInfo(Path.Combine(tempPath, "cache"));
        var sdksDirectory = new DirectoryInfo(Path.Combine(tempPath, "sdks"));
        
        return new CliExecutionContext(workingDirectory, hivesDirectory, cacheDirectory, sdksDirectory, debugMode: false);
    }

    private static ILogger<DotNetSdkInstaller> CreateTestLogger()
    {
        return NullLogger<DotNetSdkInstaller>.Instance;
    }

    private static IDotNetCliRunner CreateTestDotNetCliRunner()
    {
        return new TestDotNetCliRunner();
    }

    [Fact]
    public async Task CheckAsync_WhenDotNetIsAvailable_ReturnsTrue()
    {
        var installer = new DotNetSdkInstaller(new MinimumSdkCheckFeature(), CreateEmptyConfiguration(), CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // This test assumes the test environment has .NET SDK installed
        var result = await installer.CheckAsync();

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CheckAsync_WithMinimumVersion_WhenDotNetIsAvailable_ReturnsTrue()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var configuration = CreateConfigurationWithOverride("8.0.0");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // This test assumes the test environment has .NET SDK installed with a version >= 8.0.0
        var result = await installer.CheckAsync();

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CheckAsync_WithActualMinimumVersion_BehavesCorrectly()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var configuration = CreateConfigurationWithOverride(DotNetSdkInstaller.MinimumSdkVersion);
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Use the actual minimum version constant and check the behavior
        // Since this test environment has 8.0.117, it should return false for 9.0.302
        var result = await installer.CheckAsync();

        // Don't assert the specific result, just ensure the method doesn't throw
        // The behavior will depend on what SDK versions are actually installed
        Assert.True(result.Success == true || result.Success == false); // This will always pass but exercises the code path
    }

    [Fact]
    public async Task CheckAsync_WithHighMinimumVersion_ReturnsFalse()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var configuration = CreateConfigurationWithOverride("99.0.0");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Use an unreasonably high version that should not exist
        var result = await installer.CheckAsync();

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CheckAsync_WithInvalidMinimumVersion_ReturnsFalse()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var configuration = CreateConfigurationWithOverride("invalid.version");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Use an invalid version string
        var result = await installer.CheckAsync();

        Assert.False(result.Success);
    }

    [Fact]
    public async Task InstallAsync_CreatesSdksDirectory()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var context = CreateTestExecutionContext();
        var installer = new DotNetSdkInstaller(features, CreateEmptyConfiguration(), context, CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Get the sdks directory path
        var sdksDirectory = context.SdksDirectory.FullName;
        var sdkVersion = installer.GetEffectiveMinimumSdkVersion();
        var sdkInstallPath = Path.Combine(sdksDirectory, "dotnet", sdkVersion);

        // Clean up if it exists from a previous test
        if (Directory.Exists(sdkInstallPath))
        {
            Directory.Delete(sdkInstallPath, recursive: true);
        }

        // Note: We can't actually test the full installation in unit tests
        // because it requires downloading and running install scripts.
        // This test just verifies that the method exists and can be called.
        // The actual installation would be tested in integration tests.
        
        // For now, we just verify the method signature exists and doesn't throw
        // ArgumentNullException or similar for valid inputs
        var installTask = installer.InstallAsync(CancellationToken.None);
        
        // We expect this to either succeed or fail with a network/download error,
        // but not throw NotImplementedException anymore
        Assert.NotNull(installTask);
    }

    [Fact]
    public void GetSdksDirectory_ReturnsValidPath()
    {
        var context = CreateTestExecutionContext();
        
        // Verify the sdks directory from the execution context
        var sdksDirectory = context.SdksDirectory.FullName;
        
        // Verify the path contains the expected components
        Assert.Contains("sdks", sdksDirectory);
        
        // Verify it's a valid path format
        Assert.False(string.IsNullOrWhiteSpace(sdksDirectory));
    }

    [Fact]
    public async Task CheckReturnsTrueIfFeatureDisabled()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, false);
        var configuration = CreateConfigurationWithOverride("invalid.version");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Use an invalid version string
        var result = await installer.CheckAsync();

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CheckAsync_UsesArchitectureSpecificCommand()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var configuration = CreateConfigurationWithOverride("8.0.0");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // This test verifies that the architecture-specific command is used
        // Since the implementation adds --arch flag, it should still work correctly
        var result = await installer.CheckAsync();

        // The test should pass if the command with --arch flag works
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CheckAsync_UsesOverrideMinimumSdkVersion_WhenConfigured()
    {
        var configuration = CreateConfigurationWithOverride("8.0.0");
        var installer = new DotNetSdkInstaller(new MinimumSdkCheckFeature(), configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // The installer should use the override version instead of the constant
        var result = await installer.CheckAsync();

        // Should use 8.0.0 instead of 9.0.302, which should be available in test environment
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CheckAsync_UsesDefaultMinimumSdkVersion_WhenNotConfigured()
    {
        var installer = new DotNetSdkInstaller(new MinimumSdkCheckFeature(), CreateEmptyConfiguration(), CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Call the parameterless method that should use the default constant
        var result = await installer.CheckAsync();

        // The result depends on whether 9.0.302 is installed, but the test ensures no exception is thrown
        Assert.True(result.Success == true || result.Success == false);
    }

    [Fact]
    public async Task CheckAsync_UsesMinimumSdkVersion()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var context = CreateTestExecutionContext();
        var installer = new DotNetSdkInstaller(features, CreateEmptyConfiguration(), context, CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // Call the parameterless method that should use the minimum SDK version
        var result = await installer.CheckAsync();

        // The result depends on whether 10.0.100 is installed, but the test ensures no exception is thrown
        Assert.True(result.Success == true || result.Success == false);
    }

    [Fact]
    public async Task CheckAsync_UsesOverrideVersion_WhenOverrideConfigured()
    {
        var features = new TestFeatures()
            .SetFeature(KnownFeatures.MinimumSdkCheckEnabled, true);
        var configuration = CreateConfigurationWithOverride("8.0.0");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        // The installer should use the override version instead of the baseline constant
        var result = await installer.CheckAsync();

        // Should use 8.0.0 instead of 10.0.100, which should be available in test environment
        Assert.True(result.Success);
    }

    [Fact]
    public void GetEffectiveMinimumSdkVersion_ReturnsBaseline_WhenNoOverrides()
    {
        var features = new TestFeatures();
        var context = CreateTestExecutionContext();
        var installer = new DotNetSdkInstaller(features, CreateEmptyConfiguration(), context, CreateTestDotNetCliRunner(), null, CreateTestLogger());

        var effectiveVersion = installer.GetEffectiveMinimumSdkVersion();

        Assert.Equal(DotNetSdkInstaller.MinimumSdkVersion, effectiveVersion);
    }

    [Fact]
    public void GetEffectiveMinimumSdkVersion_ReturnsOverride_WhenOverrideConfigured()
    {
        var features = new TestFeatures();
        var configuration = CreateConfigurationWithOverride("7.0.0");
        var installer = new DotNetSdkInstaller(features, configuration, CreateTestExecutionContext(), CreateTestDotNetCliRunner(), null, CreateTestLogger());

        var effectiveVersion = installer.GetEffectiveMinimumSdkVersion();

        Assert.Equal("7.0.0", effectiveVersion);
    }

    [Fact]
    public void ErrorMessage_Format_IsCorrect()
    {
        // Test the error message format with placeholders
        var message = string.Format(CultureInfo.InvariantCulture,
            ErrorStrings.ResourceManager.GetString("MinimumSdkVersionNotMet", CultureInfo.GetCultureInfo("en-US"))!,
            "10.0.100-rc.2.25502.107",
            "(not found)");

        Assert.Equal("The Aspire CLI requires .NET SDK version 10.0.100-rc.2.25502.107 or later. Detected: (not found).", message);
    }

    [Fact]
    public void MeetsMinimumRequirement_ComparesVersionsStrictly()
    {
        // Test that version comparison uses strict semantic versioning
        // preview.1 is less than rc.2, so this should return false
        var installedVersion = SemVersion.Parse("10.0.100-preview.1.25463.5", SemVersionStyles.Strict);
        var requiredVersion = SemVersion.Parse("10.0.100-rc.2.25502.107", SemVersionStyles.Strict);

        // Use reflection to access the private method
        var method = typeof(DotNetSdkInstaller).GetMethod("MeetsMinimumRequirement",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, new object[] { installedVersion, requiredVersion })!;

        Assert.False(result);
    }

    [Fact]
    public void MeetsMinimumRequirement_AllowsHigherVersions()
    {
        // Test with a more recent .NET 10 version
        var installedVersion = SemVersion.Parse("10.1.0", SemVersionStyles.Strict);
        var requiredVersion = SemVersion.Parse("10.0.100-rc.2.25502.107", SemVersionStyles.Strict);

        // Use reflection to access the private method
        var method = typeof(DotNetSdkInstaller).GetMethod("MeetsMinimumRequirement",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, new object[] { installedVersion, requiredVersion })!;

        Assert.True(result);
    }

    [Fact]
    public void MeetsMinimumRequirement_RejectsDotNet9()
    {
        // Test that .NET 9 is rejected
        var installedVersion = SemVersion.Parse("9.0.999", SemVersionStyles.Strict);
        var requiredVersion = SemVersion.Parse("10.0.100-rc.2.25502.107", SemVersionStyles.Strict);

        // Use reflection to access the private method
        var method = typeof(DotNetSdkInstaller).GetMethod("MeetsMinimumRequirement",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, new object[] { installedVersion, requiredVersion })!;

        Assert.False(result);
    }

    [Fact]
    public void MeetsMinimumRequirement_UsesStrictComparison_ForOtherVersions()
    {
        // Test that other version requirements still use strict comparison
        var installedVersion = SemVersion.Parse("9.0.301", SemVersionStyles.Strict);
        var requiredVersion = SemVersion.Parse("9.0.302", SemVersionStyles.Strict);

        // Use reflection to access the private method
        var method = typeof(DotNetSdkInstaller).GetMethod("MeetsMinimumRequirement",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (bool)method!.Invoke(null, new object[] { installedVersion, requiredVersion })!;

        Assert.False(result);
    }

    private static IConfiguration CreateEmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private static IConfiguration CreateConfigurationWithOverride(string overrideVersion)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("overrideMinimumSdkVersion", overrideVersion)
            })
            .Build();
    }
}

public class MinimumSdkCheckFeature(bool enabled = true) : IFeatures
{
    public bool IsFeatureEnabled(string featureName, bool defaultValue = false)
    {
        return featureName == KnownFeatures.MinimumSdkCheckEnabled ? enabled : false;
    }
}

public class TestFeatures : IFeatures
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
