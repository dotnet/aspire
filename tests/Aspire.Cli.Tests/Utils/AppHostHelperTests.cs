// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Telemetry;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class AppHostHelperTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("13.0.0-preview.1.25520.1", true)] // Preview version higher than minimum
    [InlineData("13.0.0-preview.1", true)] // Preview version higher than minimum
    [InlineData("13.0.0", true)] // Release version higher than minimum
    [InlineData("10.0.0-preview.1", true)] // Preview version higher than minimum
    [InlineData("9.2.0", true)] // Exact match
    [InlineData("9.2.0-preview.1", true)] // Preview with same major.minor.patch as minimum
    [InlineData("9.2.1", true)] // Patch version higher
    [InlineData("9.3.0", true)] // Minor version higher
    [InlineData("10.0.0", true)] // Major version higher
    [InlineData("9.1.9", false)] // Version lower than minimum
    [InlineData("9.1.9-preview.1", false)] // Preview version lower than minimum
    [InlineData("8.0.0", false)] // Major version lower
    [InlineData("8.9.9-preview.1", false)] // Preview with major version lower
    public async Task CheckAppHostCompatibilityAsync_VersionComparison_ReturnsExpectedResult(
        string appHostVersion,
        bool expectedCompatible)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));

        var runner = new TestDotNetCliRunner
        {
            GetAppHostInformationAsyncCallback = (projectFile, options, ct) =>
                (0, true, appHostVersion)
        };

        var interactionService = new TestConsoleInteractionService();
        var telemetry = new AspireCliTelemetry();

        // Note: We can't easily test with different minimum versions without modifying the AppHostHelper code,
        // but we can test that the current minimum version (9.2.0) works correctly with various app host versions
        var (isCompatible, supportsBackchannel, version) = await AppHostHelper.CheckAppHostCompatibilityAsync(
            runner,
            interactionService,
            projectFile,
            telemetry,
            workspace.WorkspaceRoot,
            CancellationToken.None);

        Assert.Equal(expectedCompatible, isCompatible);
        Assert.Equal(expectedCompatible, supportsBackchannel); // Both should match for versions >= 9.2.0
        Assert.Equal(appHostVersion, version);
    }

    [Fact]
    public async Task CheckAppHostCompatibilityAsync_WhenProjectAnalysisFails_ReturnsFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));

        var runner = new TestDotNetCliRunner
        {
            GetAppHostInformationAsyncCallback = (projectFile, options, ct) =>
                (1, false, null) // Non-zero exit code
        };

        var interactionService = new TestConsoleInteractionService();
        var telemetry = new AspireCliTelemetry();

        var (isCompatible, supportsBackchannel, version) = await AppHostHelper.CheckAppHostCompatibilityAsync(
            runner,
            interactionService,
            projectFile,
            telemetry,
            workspace.WorkspaceRoot,
            CancellationToken.None);

        Assert.False(isCompatible);
        Assert.False(supportsBackchannel);
        Assert.Null(version);
    }

    [Fact]
    public async Task CheckAppHostCompatibilityAsync_WhenNotAspireHost_ReturnsFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "NotAnAppHost.csproj"));

        var runner = new TestDotNetCliRunner
        {
            GetAppHostInformationAsyncCallback = (projectFile, options, ct) =>
                (0, false, null) // Not an Aspire host
        };

        var interactionService = new TestConsoleInteractionService();
        var telemetry = new AspireCliTelemetry();

        var (isCompatible, supportsBackchannel, version) = await AppHostHelper.CheckAppHostCompatibilityAsync(
            runner,
            interactionService,
            projectFile,
            telemetry,
            workspace.WorkspaceRoot,
            CancellationToken.None);

        Assert.False(isCompatible);
        Assert.False(supportsBackchannel);
        Assert.Null(version);
    }

    [Fact]
    public async Task CheckAppHostCompatibilityAsync_WhenVersionCannotBeParsed_ReturnsFalse()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));

        var runner = new TestDotNetCliRunner
        {
            GetAppHostInformationAsyncCallback = (projectFile, options, ct) =>
                (0, true, "invalid-version-string")
        };

        var interactionService = new TestConsoleInteractionService();
        var telemetry = new AspireCliTelemetry();

        var (isCompatible, supportsBackchannel, version) = await AppHostHelper.CheckAppHostCompatibilityAsync(
            runner,
            interactionService,
            projectFile,
            telemetry,
            workspace.WorkspaceRoot,
            CancellationToken.None);

        Assert.False(isCompatible);
        Assert.False(supportsBackchannel);
        Assert.Null(version);
    }

    [Theory]
    [InlineData("13.0.0-preview.1.25520.1")] // Full preview version with build metadata
    [InlineData("13.0.0-preview.1")] // Preview version without build metadata
    [InlineData("13.0.0-rc.1")] // Release candidate
    [InlineData("13.0.0-beta.2")] // Beta version
    [InlineData("13.0.0-alpha.1")] // Alpha version
    public async Task CheckAppHostCompatibilityAsync_PreviewVersions_AreAccepted(string previewVersion)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var projectFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "AppHost.csproj"));

        var runner = new TestDotNetCliRunner
        {
            GetAppHostInformationAsyncCallback = (projectFile, options, ct) =>
                (0, true, previewVersion)
        };

        var interactionService = new TestConsoleInteractionService();
        var telemetry = new AspireCliTelemetry();

        var (isCompatible, supportsBackchannel, version) = await AppHostHelper.CheckAppHostCompatibilityAsync(
            runner,
            interactionService,
            projectFile,
            telemetry,
            workspace.WorkspaceRoot,
            CancellationToken.None);

        Assert.True(isCompatible, $"Preview version {previewVersion} should be accepted");
        Assert.True(supportsBackchannel);
        Assert.Equal(previewVersion, version);
    }
}
