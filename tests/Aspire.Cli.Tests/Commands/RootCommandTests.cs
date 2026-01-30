// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class RootCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task RootCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RootCommandWithNoLogoArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--nologo --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("True", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public async Task NoLogoEnvironmentVariable_ParsedCorrectly(string? value, bool expectedNoLogo)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ConfigurationCallback = config =>
            {
                config[CliConfigNames.NoLogo] = value;
            };
        });
        var provider = services.BuildServiceProvider();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var isNoLogo = configuration.GetBool(CliConfigNames.NoLogo, defaultValue: false);

        Assert.Equal(expectedNoLogo, isNoLogo);

        // Also verify command still works with the configuration
        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("--help");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerDisplayedWhenSentinelDoesNotExist()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false);

        Assert.True(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerNotDisplayedWhenSentinelExists()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false);

        Assert.False(bannerService.WasBannerDisplayed);
        Assert.False(sentinel.WasCreated);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerNotDisplayedWithNoLogoArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: true);

        Assert.False(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public async Task FirstTimeUseNotice_BannerNotDisplayedWithNoLogoEnvironmentVariable()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
            options.ConfigurationCallback = config =>
            {
                config[CliConfigNames.NoLogo] = "1";
            };
        });
        var provider = services.BuildServiceProvider();

        var configuration = provider.GetRequiredService<IConfiguration>();
        var noLogo = configuration.GetBool(CliConfigNames.NoLogo, defaultValue: false);

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo);

        Assert.False(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
    }

    [Fact]
    public async Task BannerOption_DisplaysBannerWhenInvoked()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        var bannerOption = new BannerOption(() => bannerService);

        // Invoke the option's action directly
        Assert.NotNull(bannerOption.Action);
        var asyncAction = Assert.IsType<System.CommandLine.Invocation.AsynchronousCommandLineAction>(bannerOption.Action, exactMatch: false);
        var parseResult = new System.CommandLine.RootCommand().Parse([]);
        var exitCode = await asyncAction.InvokeAsync(parseResult, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.True(bannerService.WasBannerDisplayed);
    }

    [Fact]
    public async Task BannerOption_CanBeInvokedMultipleTimes()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var bannerService = new TestBannerService();

        var bannerOption = new BannerOption(() => bannerService);
        var asyncAction = Assert.IsType<System.CommandLine.Invocation.AsynchronousCommandLineAction>(bannerOption.Action, exactMatch: false);
        var parseResult = new System.CommandLine.RootCommand().Parse([]);

        // Invoke multiple times
        await asyncAction.InvokeAsync(parseResult, CancellationToken.None);
        await asyncAction.InvokeAsync(parseResult, CancellationToken.None);
        await asyncAction.InvokeAsync(parseResult, CancellationToken.None);

        Assert.Equal(3, bannerService.DisplayCount);
    }

    [Fact]
    public void BannerOption_HasCorrectDescription()
    {
        var bannerService = new TestBannerService();
        var bannerOption = new BannerOption(() => bannerService);

        Assert.Equal("--banner", bannerOption.Name);
        Assert.NotNull(bannerOption.Description);
        Assert.NotEmpty(bannerOption.Description);
    }

    [Fact]
    public void BannerOption_ThrowsWhenFactoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new BannerOption(null!));
    }

    [Fact]
    public async Task Banner_DisplayedOnFirstRunNotSuppressedByExplicitBannerRequest()
    {
        // When it's a first run AND user explicitly requests --banner,
        // the banner should still be shown (via first run logic)
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false);

        Assert.True(bannerService.WasBannerDisplayed);
        Assert.True(sentinel.WasCreated);
        // Telemetry notice should be shown on first run
        var errorOutput = errorWriter.ToString();
        Assert.Contains("Telemetry", errorOutput);
    }

    [Fact]
    public async Task Banner_TelemetryNoticeShownOnFirstRun()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = false };
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false);

        var errorOutput = errorWriter.ToString();
        Assert.Contains("Telemetry", errorOutput);
    }

    [Fact]
    public async Task Banner_TelemetryNoticeNotShownOnSubsequentRuns()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var errorWriter = new StringWriter();
        var sentinel = new TestFirstTimeUseNoticeSentinel { SentinelExists = true }; // Not first run
        var bannerService = new TestBannerService();

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ErrorTextWriter = errorWriter;
            options.FirstTimeUseNoticeSentinelFactory = _ => sentinel;
            options.BannerServiceFactory = _ => bannerService;
        });
        var provider = services.BuildServiceProvider();

        await Program.DisplayFirstTimeUseNoticeIfNeededAsync(provider, noLogo: false);

        var errorOutput = errorWriter.ToString();
        Assert.DoesNotContain("Telemetry", errorOutput);
    }
}
